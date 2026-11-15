using System.Globalization;

using Heartbeat.Desktop.Converters;
using Heartbeat.Desktop.Localization;
using Heartbeat.Desktop.Services;
using Heartbeat.Desktop.Services.Profile;
using Heartbeat.Shared;

namespace Heartbeat.Desktop.ViewModels;

/// <summary>Un semáforo de un peligro: color + palabra (nunca solo color), el «por qué» en lenguaje simple y su fuente.</summary>
public sealed record HazardLightViewModel(
    Hazard Hazard,
    string Name,
    HealthStatus? Status,
    string StatusText,
    string Why,
    string Evidence,
    string Detail,
    string SourceText)
{
    public bool HasEvidence => Evidence.Length > 0;

    public bool HasDetail => Detail.Length > 0;

    public bool HasSource => SourceText.Length > 0;
}

/// <summary>
/// Modo simple (HB-FE-01): un semáforo global y uno por cada uno de los cinco peligros de la NASA, cada uno con su
/// «por qué», para entender el estado en 5 segundos. El cálculo es el motor de reglas compartido
/// (<see cref="RiskEngine"/>, HB-BE-07) con los datos guardados en el equipo, así que funciona sin conexión y da lo mismo
/// que el servidor. Los umbrales viven en el catálogo, no aquí. Todo es simulado y no es un diagnóstico.
/// </summary>
public sealed class SimpleStatusViewModel
{
    private static readonly Hazard[] Order =
    [
        Hazard.SpaceRadiation,
        Hazard.IsolationAndConfinement,
        Hazard.DistanceFromEarth,
        Hazard.GravityFields,
        Hazard.HostileClosedEnvironment,
    ];

    public SimpleStatusViewModel(
        IReadOnlyList<CheckUpRecord> checkUps,
        IReadOnlyList<RadiationDoseRecord> doses,
        IReadOnlyList<EnvironmentReadingRecord> environment,
        EnvironmentProfile? profile)
    {
        RiskInputs inputs = BuildInputs(checkUps, doses, environment, profile, out HashSet<Hazard> withData);
        RiskStatus risk = RiskEngine.Evaluate(inputs);

        Hazards = Order
            .Select(h => BuildLight(risk.Hazards.First(x => x.Hazard == h), withData.Contains(h)))
            .ToList();

        List<HazardLightViewModel> measured = Hazards.Where(h => h.Status is not null).ToList();
        HasData = measured.Count > 0;
        GlobalStatus = HasData ? measured.Max(h => h.Status!.Value) : null;
        GlobalStatusText = StatusPalette.Name(GlobalStatus);
        Headline = GlobalStatus switch
        {
            HealthStatus.Normal => Loc.T("simple.headline.normal"),
            HealthStatus.Watch => Loc.T("simple.headline.watch"),
            HealthStatus.Critical => Loc.T("simple.headline.critical"),
            _ => Loc.T("simple.headline.none"),
        };
        GlobalWhy = BuildGlobalWhy(measured);
    }

    public IReadOnlyList<HazardLightViewModel> Hazards { get; }

    public bool HasData { get; }

    /// <summary>El peor estado entre los peligros con datos; <c>null</c> si aún no hay ninguno.</summary>
    public HealthStatus? GlobalStatus { get; }

    public string GlobalStatusText { get; }

    /// <summary>Una frase para el estado global, por ejemplo «Hay algo que vigilar».</summary>
    public string Headline { get; }

    /// <summary>Qué peligros hacen que el global no sea verde, o por qué aún no hay estado.</summary>
    public string GlobalWhy { get; }

    private static RiskInputs BuildInputs(
        IReadOnlyList<CheckUpRecord> checkUps,
        IReadOnlyList<RadiationDoseRecord> doses,
        IReadOnlyList<EnvironmentReadingRecord> environment,
        EnvironmentProfile? profile,
        out HashSet<Hazard> withData)
    {
        withData = [];

        // Un check-up puede omitir indicadores: se toma la lectura más reciente de cada uno.
        List<ReadingEvaluation> latest = [];
        foreach (IndicatorType type in Enum.GetValues<IndicatorType>())
        {
            ReadingRecord? reading = checkUps
                .Select(c => c.Readings.FirstOrDefault(r => r.Input.Indicator == type))
                .FirstOrDefault(r => r is not null);
            if (reading is not null)
            {
                latest.Add(reading.Evaluation);
                withData.Add(RiskEngine.HazardOf(type));
            }
        }

        CheckUpEvaluation? vitals = latest.Count == 0
            ? null
            : new CheckUpEvaluation(latest.Max(r => r.Status), latest, "local");

        RadiationSummaryDto? radiation = null;
        if (doses.Count > 0)
        {
            decimal total = RadiationDoses.Total(doses.Select(d => d.DoseMsv));
            radiation = new RadiationSummaryDto(
                total,
                doses.Count,
                doses.Select(d => new RadiationDoseDto(d.Id, d.RecordedAt, d.SyncedAt, d.DoseMsv, d.Note)).ToList(),
                IndicatorsCatalog.SourceFor(IndicatorsCatalog.Find("radiation")!),
                PercentOfLimit: RadiationDoses.PercentOfCareerLimit(total));
            withData.Add(Hazard.SpaceRadiation);
        }

        List<EnvironmentReadingStatus> habitat = [];
        List<EnvironmentReadingStatus> vision = [];
        foreach (EnvironmentReadingRecord record in environment)
        {
            EnvironmentReadingStatus? status = EnvironmentStatusFactory.From(record);
            if (status is null)
            {
                continue;
            }

            if (IndicatorsCatalog.Find(status.IndicatorId)?.Hazard == Hazard.HostileClosedEnvironment)
            {
                habitat.Add(status);
                withData.Add(Hazard.HostileClosedEnvironment);
            }
            else if (VisionAnalysis.IsVisionIndicator(status.IndicatorId))
            {
                vision.Add(status);
                withData.Add(Hazard.GravityFields);
            }
        }

        // Un perfil de entorno incompleto (tripulación o volumen en 0, como lo manda el servidor si la misión no lo tiene
        // definido) no permite calcular los índices: se deja sin datos en vez de romper la pantalla.
        IsolationConfinementIndices? isolation = null;
        if (profile is { CrewSize: > 0, HabitableVolumeM3: > 0 })
        {
            isolation = IsolationConfinement.Evaluate(profile);
            withData.Add(Hazard.IsolationAndConfinement);
            withData.Add(Hazard.DistanceFromEarth);
            withData.Add(Hazard.GravityFields);
        }

        return new RiskInputs(vitals, radiation, habitat, profile, isolation, null, vision);
    }

    private static HazardLightViewModel BuildLight(HazardStatus hazard, bool hasData)
    {
        string name = Loc.T($"hazard.{char.ToLowerInvariant(hazard.Hazard.ToString()[0])}{hazard.Hazard.ToString()[1..]}");
        if (!hasData)
        {
            return new HazardLightViewModel(
                hazard.Hazard, name, null, StatusPalette.Name(null), Loc.T($"simple.why.{hazard.Hazard}.none"), string.Empty, string.Empty, string.Empty);
        }

        string state = hazard.Status switch
        {
            HealthStatus.Normal => "normal",
            HealthStatus.Watch => "watch",
            _ => "critical",
        };

        string evidence = string.Empty;
        if (hazard.Status != HealthStatus.Normal && hazard.Value is { } value)
        {
            string unit = hazard.Unit ?? string.Empty;
            evidence = hazard.Limit is { } limit
                ? Loc.T("simple.evidence.limit", Format(value), unit, Format(limit))
                : Loc.T("simple.evidence", Format(value), unit);
        }

        // La explicación del motor es la de Shared (en inglés): se ofrece como detalle, el «por qué» principal está traducido.
        string detail = hazard.Status == HealthStatus.Normal ? string.Empty : Loc.T("simple.detail", hazard.Explanation);
        string source = hazard.Source is { } link ? Loc.T("simple.source", link.Title) : string.Empty;

        return new HazardLightViewModel(
            hazard.Hazard,
            name,
            hazard.Status,
            StatusPalette.Name(hazard.Status),
            Loc.T($"simple.why.{hazard.Hazard}.{state}"),
            evidence,
            detail,
            source);
    }

    private static string BuildGlobalWhy(List<HazardLightViewModel> measured)
    {
        if (measured.Count == 0)
        {
            return Loc.T("simple.global.none");
        }

        List<HazardLightViewModel> flagged = measured
            .Where(h => h.Status != HealthStatus.Normal)
            .OrderByDescending(h => h.Status)
            .ToList();
        return flagged.Count == 0
            ? Loc.T("simple.global.allNormal", measured.Count)
            : Loc.T("simple.global.flagged", string.Join(", ", flagged.Select(h => $"{h.Name} ({h.StatusText.ToLower(Loc.Culture)})")));
    }

    private static string Format(decimal value) => value.ToString("0.##", CultureInfo.CurrentCulture);
}
