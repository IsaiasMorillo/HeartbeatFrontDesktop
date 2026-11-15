using System.IO;

using Heartbeat.Desktop.Localization;
using Heartbeat.Desktop.Services;
using Heartbeat.Desktop.ViewModels;
using Heartbeat.Shared;

namespace Heartbeat.Desktop.Tests;

/// <summary>HB-FE-01: modo simple con semáforo por peligro, introducción de 3 pasos y glosario.</summary>
public sealed class SimpleStatusTests
{
    private static DashboardViewModel Create(TestWorld world, IOnboardingStore? onboarding = null) =>
        new(world.Store, world.Session, world.Info, world.Presenter, world.Navigation, world.Clock, world.Reminder, world.ProfileStore, world.EnvironmentStore, onboarding);

    private static TestWorld Fresh()
    {
        TestWorld world = new(signedInUser: null);
        world.Session.SignIn(new SessionUser(Guid.NewGuid(), "nueva", "Nueva Persona"));
        return world;
    }

    // ---- Perfil de entorno incompleto ----

    [Fact]
    public void A_mission_profile_with_no_crew_or_volume_does_not_break_the_screen()
    {
        // El servidor manda ceros cuando la misión no tiene el perfil definido: antes el login terminaba en «problema inesperado».
        EnvironmentProfile empty = new("station", 0m, 0m, 0, 0m);

        SimpleStatusViewModel simple = new([], [], [], empty);

        HazardLightViewModel isolation = simple.Hazards.First(h => h.Hazard == Hazard.IsolationAndConfinement);
        Assert.Null(isolation.Status);
    }

    // ---- Semáforos ----

    [Fact]
    public void Shows_one_light_for_each_of_the_five_hazards_plus_a_global_one()
    {
        SimpleStatusViewModel simple = Create(new TestWorld("ana")).Simple;

        Assert.Equal(
            [Hazard.SpaceRadiation, Hazard.IsolationAndConfinement, Hazard.DistanceFromEarth, Hazard.GravityFields, Hazard.HostileClosedEnvironment],
            simple.Hazards.Select(h => h.Hazard));
        Assert.True(simple.HasData);
        Assert.NotNull(simple.GlobalStatus);
    }

    [Fact]
    public void Every_light_with_data_names_its_status_in_words_and_says_why()
    {
        SimpleStatusViewModel simple = Create(new TestWorld("ana")).Simple;

        Assert.All(simple.Hazards.Where(h => h.Status is not null), h =>
        {
            Assert.Contains(h.StatusText, new[] { "Normal", "Vigilancia", "Crítico" });
            Assert.False(string.IsNullOrWhiteSpace(h.Why));
            Assert.False(string.IsNullOrWhiteSpace(h.Name));
        });
    }

    [Fact]
    public void The_global_light_is_the_worst_of_the_hazards_with_data()
    {
        SimpleStatusViewModel simple = Create(new TestWorld("luis")).Simple;

        Assert.Equal(simple.Hazards.Where(h => h.Status is not null).Max(h => h.Status), simple.GlobalStatus);
        Assert.Equal(HealthStatus.Critical, simple.GlobalStatus);
        Assert.Equal("Crítico", simple.GlobalStatusText);
        Assert.Contains("(", simple.GlobalWhy);
    }

    [Fact]
    public void A_dose_over_the_career_limit_turns_radiation_red_and_shows_its_numbers()
    {
        TestWorld world = Fresh();
        world.Store.AddRadiationDose(world.Session.CurrentUser!.Id, 450m);
        world.Store.AddRadiationDose(world.Session.CurrentUser!.Id, 200m);

        HazardLightViewModel radiation = Create(world).Simple.Hazards.Single(h => h.Hazard == Hazard.SpaceRadiation);

        Assert.Equal(HealthStatus.Critical, radiation.Status);
        Assert.Equal("Crítico", radiation.StatusText);
        Assert.True(radiation.HasEvidence);
        Assert.Contains("mSv", radiation.Evidence);
        Assert.True(radiation.HasSource);
    }

    [Fact]
    public void A_new_astronaut_sees_no_data_instead_of_a_false_green()
    {
        SimpleStatusViewModel simple = Create(Fresh()).Simple;

        Assert.False(simple.HasData);
        Assert.Null(simple.GlobalStatus);
        Assert.Equal("Sin datos", simple.GlobalStatusText);
        Assert.All(simple.Hazards, h =>
        {
            Assert.Null(h.Status);
            Assert.Equal("Sin datos", h.StatusText);
        });
    }

    [Fact]
    public void Planned_conditions_of_the_mission_never_turn_the_light_red_on_their_own()
    {
        // Gravedad y distancia salen del perfil de la misión: el motor las limita a «Vigilancia».
        SimpleStatusViewModel simple = Create(new TestWorld("ana")).Simple;

        Assert.NotEqual(HealthStatus.Critical, simple.Hazards.Single(h => h.Hazard == Hazard.DistanceFromEarth).Status);
    }

    // ---- Datos simulados ----

    [Fact]
    public void The_simulated_data_label_exists_in_both_languages()
    {
        Assert.Equal("Datos simulados", Strings.Get("simple.simulated", AppLanguage.Spanish));
        Assert.Equal("Simulated data", Strings.Get("simple.simulated", AppLanguage.English));
    }

    [Fact]
    public void Every_hazard_and_state_has_a_plain_language_reason_in_both_languages()
    {
        foreach (Hazard hazard in Enum.GetValues<Hazard>())
        {
            foreach (string state in new[] { "none", "normal", "watch", "critical" })
            {
                Assert.True(Strings.All.ContainsKey($"simple.why.{hazard}.{state}"), $"falta simple.why.{hazard}.{state}");
            }
        }
    }

    // ---- Introducción de 3 pasos ----

    [Fact]
    public void The_first_time_the_introduction_opens_at_step_one_of_three()
    {
        OnboardingViewModel onboarding = Create(new TestWorld("ana"), new FakeOnboardingStore()).Onboarding;

        Assert.True(onboarding.IsOpen);
        Assert.Equal(0, onboarding.StepIndex);
        Assert.False(onboarding.CanGoBack);
        Assert.Equal(3, OnboardingViewModel.StepCount);
        Assert.Equal("Paso 1 de 3", onboarding.ProgressText);
    }

    [Fact]
    public void Next_walks_the_three_steps_and_the_last_one_closes_and_remembers_it()
    {
        TestWorld world = new("ana");
        FakeOnboardingStore store = new();
        OnboardingViewModel onboarding = Create(world, store).Onboarding;

        onboarding.NextCommand.Execute(null);
        Assert.Equal(1, onboarding.StepIndex);
        Assert.True(onboarding.CanGoBack);
        onboarding.NextCommand.Execute(null);
        Assert.True(onboarding.IsLastStep);
        Assert.Equal("Listo", onboarding.NextText);
        Assert.True(onboarding.IsOpen);

        onboarding.NextCommand.Execute(null);

        Assert.False(onboarding.IsOpen);
        Assert.True(store.IsCompleted(world.Session.CurrentUser!.Id));
        Assert.False(Create(world, store).Onboarding.IsOpen);
    }

    [Fact]
    public void Skip_also_remembers_it_and_back_returns_to_the_previous_step()
    {
        TestWorld world = new("ana");
        FakeOnboardingStore store = new();
        OnboardingViewModel onboarding = Create(world, store).Onboarding;
        onboarding.NextCommand.Execute(null);
        onboarding.BackCommand.Execute(null);
        Assert.Equal(0, onboarding.StepIndex);

        onboarding.FinishCommand.Execute(null);

        Assert.False(onboarding.IsOpen);
        Assert.True(store.IsCompleted(world.Session.CurrentUser!.Id));
    }

    [Fact]
    public void The_introduction_can_be_shown_again_from_the_first_step()
    {
        TestWorld world = new("ana");
        FakeOnboardingStore store = new();
        store.MarkCompleted(world.Session.CurrentUser!.Id);
        OnboardingViewModel onboarding = Create(world, store).Onboarding;
        Assert.False(onboarding.IsOpen);

        onboarding.ShowCommand.Execute(null);

        Assert.True(onboarding.IsOpen);
        Assert.Equal(0, onboarding.StepIndex);
    }

    [Fact]
    public void Without_an_onboarding_store_the_introduction_stays_closed()
    {
        Assert.False(Create(new TestWorld("ana")).Onboarding.IsOpen);
    }

    [Fact]
    public void The_file_store_remembers_who_finished_and_survives_a_damaged_file()
    {
        string path = Path.Combine(Path.GetTempPath(), $"onboarding-{Guid.NewGuid():N}.json");
        try
        {
            Guid ana = Guid.NewGuid();
            FileOnboardingStore store = new(path);
            Assert.False(store.IsCompleted(ana));

            store.MarkCompleted(ana);

            Assert.True(new FileOnboardingStore(path).IsCompleted(ana));
            File.WriteAllText(path, "{ not json");
            Assert.False(new FileOnboardingStore(path).IsCompleted(ana));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- Glosario ----

    [Fact]
    public void The_glossary_explains_msv_co2_sans_and_pvt()
    {
        DashboardViewModel dashboard = Create(new TestWorld("ana"));

        string[] terms = dashboard.GlossaryTerms.Select(t => t.Term).ToArray();
        Assert.Contains(terms, t => t.StartsWith("mSv", StringComparison.Ordinal));
        Assert.Contains("CO₂", terms);
        Assert.Contains("SANS", terms);
        Assert.Contains("PVT", terms);
        Assert.All(dashboard.GlossaryTerms, t => Assert.False(string.IsNullOrWhiteSpace(t.Meaning)));
    }

    [Fact]
    public void The_glossary_starts_closed_and_toggles()
    {
        DashboardViewModel dashboard = Create(new TestWorld("ana"));
        Assert.False(dashboard.IsGlossaryOpen);

        dashboard.ToggleGlossaryCommand.Execute(null);
        Assert.True(dashboard.IsGlossaryOpen);

        dashboard.ToggleGlossaryCommand.Execute(null);
        Assert.False(dashboard.IsGlossaryOpen);
    }
}
