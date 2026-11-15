using System.Net.Http;
using Heartbeat.Client;
using Heartbeat.Desktop.Assistant;
using Heartbeat.Desktop.Data;
using Heartbeat.Desktop.Demo;
using Heartbeat.Desktop.Info;
using Heartbeat.Desktop.Navigation;
using Heartbeat.Desktop.Services;
using Heartbeat.Desktop.Services.Auth;
using Heartbeat.Desktop.Services.Profile;
using Heartbeat.Desktop.Services.Sync;
using Heartbeat.Desktop.Services.Watch;
using Heartbeat.Desktop.ViewModels;
using Heartbeat.Desktop.Views;
using Heartbeat.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heartbeat.Desktop;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra la ventana principal, la navegación, los servicios y todas las pantallas.</summary>
    public static IServiceCollection AddHeartbeatDesktop(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Base de datos local (SQLite) para funcionamiento offline.
        services.AddDbContext<LocalDbContext>(options =>
            options.UseSqlite(LocalDatabase.ConnectionString(LocalDatabase.DefaultPath)));

        // Infraestructura de la interfaz.
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISessionContext, SessionContext>();

        // Recordatorio diario de check-up (P2-REMIND): horario guardado en el equipo y aviso de Windows.
        services.AddSingleton<ICheckUpReminderSettingsStore, FileCheckUpReminderSettingsStore>();
        services.AddSingleton<ICheckUpNotification, WindowsCheckUpNotification>();

        // Introducción de 3 pasos de la primera vez (HB-FE-01).
        services.AddSingleton<IOnboardingStore, FileOnboardingStore>();
        services.AddSingleton<CheckUpReminder>();
        services.AddHostedService<CheckUpReminderService>();

        // Conectividad: la red real más el interruptor "Enlace con la Tierra" para demostrar el modo offline.
        services.AddSingleton<NetworkConnectivityService>();
        services.AddSingleton<SimulatedLinkService>();
        services.AddSingleton<IConnectivityService>(sp => sp.GetRequiredService<SimulatedLinkService>());
        services.AddSingleton<ISimulatedLink>(sp => sp.GetRequiredService<SimulatedLinkService>());

        // Acceso (#27): Crew API real o de demostración según CrewApi:Mode, hash local PBKDF2 y tokens con DPAPI.
        services.AddSingleton(sp => CrewApiOptions.From(sp.GetService<IConfiguration>()));
        services.AddSingleton<ICrewApiClient>(sp =>
        {
            CrewApiOptions options = sp.GetRequiredService<CrewApiOptions>();
            return options.IsDemo
                ? new DemoCrewApiClient(sp.GetRequiredService<TimeProvider>())
                : new CrewApiClient(new HttpClient { BaseAddress = new Uri(options.BaseUrl), Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds) });
        });
        services.AddSingleton<ITokenProtector, DpapiTokenProtector>();
        services.AddSingleton<IDeviceIdProvider, FileDeviceIdProvider>();
        services.AddSingleton<IAuthService, AuthService>();

        // Claude se consume exclusivamente a través de Heartbeat.Api; ninguna API key vive en el escritorio.
        services.AddSingleton<IAssistantApiClient>(sp =>
        {
            CrewApiOptions options = sp.GetRequiredService<CrewApiOptions>();
            return new AssistantApiClient(
                new HttpClient { BaseAddress = new Uri(options.BaseUrl), Timeout = TimeSpan.FromSeconds(Math.Max(10, options.TimeoutSeconds)) },
                sp.GetRequiredService<ICrewApiClient>(),
                options);
        });
        services.AddTransient<AssistantViewModel>();
        services.AddTransient<AssistantWindow>();
        services.AddSingleton<IAssistantWindowPresenter, AssistantWindowPresenter>();

        // Rangos de referencia: los descargados en una sincronización anterior si son más nuevos, o los embebidos.
        services.AddSingleton(sp => RangesSource.Load(sp));

        // Reglas de evaluación compartidas con el backend (Heartbeat.Shared).
        services.AddSingleton<IHealthEvaluator>(sp => new HealthEvaluator(sp.GetRequiredService<RangesSource>().Json));

        // Check-ups y mensajes se guardan en la base local (#8) y el motor de sincronización (#32) los sube.
        services.AddSingleton<IHealthRecordStore, LocalHealthRecordStore>();
        services.AddSingleton<IEnvironmentReadingStore, LocalEnvironmentReadingStore>();
        services.AddSingleton<IProfileStore, LocalProfileStore>();
        services.AddSingleton<IVisionStore, LocalVisionStore>();

        // Sincronización en segundo plano: servicio hospedado, estado para la barra de estado y petición manual.
        services.AddSingleton<SyncEngine>();
        services.AddSingleton<ISyncEngine>(sp => sp.GetRequiredService<SyncEngine>());
        services.AddSingleton<ISyncStatusProvider>(sp => sp.GetRequiredService<SyncEngine>());
        services.AddHostedService(sp => sp.GetRequiredService<SyncEngine>());
        services.AddSingleton<IBodyMeasurementStore, DemoBodyMeasurementStore>();
        services.AddTransient<BodyMeasurementsViewModel>();

        // Panel de información y fuentes por indicador (DK-INFO).
        services.AddSingleton<IIndicatorInfoProvider>(sp => new ReferenceRangesInfoProvider(sp.GetRequiredService<RangesSource>().Json));
        services.AddSingleton(sp => IndicatorBands.Parse(sp.GetRequiredService<RangesSource>().Json));
        services.AddSingleton<IUrlLauncher, ShellUrlLauncher>();
        services.AddSingleton<IIndicatorInfoPresenter, IndicatorInfoWindowPresenter>();
        services.AddTransient<IndicatorInfoViewModel>();

        // Ventana principal.
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        // Pantallas: transitorias, se crean (y liberan) en cada navegación; ver NavigationService.
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CheckUpViewModel>();
        services.AddTransient<CheckUpResultViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<MessagesViewModel>();
        services.AddTransient<RadiationViewModel>();
        services.AddTransient<VisionViewModel>();

        // Reloj (WR-DESKTOP): FC en vivo con el token del tripulante, datos de Samsung Health y la última lectura guardada.
        services.AddSingleton<ILastWatchReadingStore, FileLastWatchReadingStore>();
        services.AddSingleton<ISamsungHealthClient, HttpSamsungHealthClient>();
        services.AddTransient<IHeartRateFeed, SignalRHeartRateFeed>();
        services.AddTransient<WatchViewModel>();
        services.AddTransient<ProfileViewModel>();

        return services;
    }
}
