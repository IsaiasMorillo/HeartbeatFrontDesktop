# Heartbeat — App de escritorio

NASA Space Apps 2026. App WPF de autoevaluación de salud para astronautas (funciona sin conexión y sincroniza con la API cuando hay enlace).

> **Datos simulados.** El modo Demo usa datos ficticios para probar los flujos de la aplicación.
>
> **Herramienta de apoyo a la decisión; no diagnóstico médico.** Los resultados no sustituyen el criterio de profesionales cualificados.
>
> **No es un dispositivo médico ni diagnostica, previene o trata enfermedades.**
> No uses sus estados, alertas ni umbrales como consejo médico.

Los umbrales y fórmulas son configurables y se mantienen en el catálogo compartido de indicadores, no en la UI. Cada entrada documenta su `source` y el estado de la evidencia (`verified` o `heuristic`). Consulta el catálogo vigente en [HeartbeatBackend](https://github.com/AliaJimenez/HeartbeatBackend/blob/dev/contracts/indicators.catalog.json).

El backend, los contratos y **todos los tickets** viven en [AliaJimenez/HeartbeatBackend](https://github.com/AliaJimenez/HeartbeatBackend). Este repo contiene solo el cliente de escritorio.

## Requisitos

- Windows 10/11
- .NET SDK 10.0.x
- Git (el código compartido llega como submódulo)

## Clonar

```bash
git clone --recurse-submodules https://github.com/IsaiasMorillo/HeartbeatFrontDesktop.git
```

Si ya clonaste sin submódulos:

```bash
git submodule update --init
```

Para traer cambios nuevos de `Heartbeat.Shared` / `Heartbeat.Client` (y luego commitear el puntero actualizado):

```bash
git submodule update --remote external/HeartbeatBackend
```

> No edites nada dentro de `external/HeartbeatBackend`: los cambios a Shared van como PR en `HeartbeatBackend`.

## Compilar, probar y ejecutar

```bash
dotnet build HeartbeatDesktop.sln
dotnet test HeartbeatDesktop.sln
dotnet run --project src/Heartbeat.Desktop/Heartbeat.Desktop.csproj
```

## Estructura

| Ruta | Contenido |
| --- | --- |
| `src/Heartbeat.Desktop` | App WPF (`net10.0-windows`) |
| `tests/Heartbeat.Desktop.Tests` | Pruebas xUnit de navegación, barra de estado y DI |
| `external/HeartbeatBackend` | Submódulo: `Heartbeat.Shared` y `Heartbeat.Client` |

### Arquitectura de `Heartbeat.Desktop`

- **MVVM** con [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- **DI** con `Microsoft.Extensions.Hosting`; todo el registro está en `ServiceCollectionExtensions.AddHeartbeatDesktop()`.
- **Navegación ViewModel primero**: `INavigationService.NavigateTo<TViewModel>()`. Cada página se crea en su propio scope de DI y se libera al salir. La vista de cada ViewModel se declara en `Views/PageTemplates.xaml`.
- **Tema** en `Themes/` (claro, paleta de marca: blanco ~70 %, azul `#110559` ~20 %, rojo `#990E0E` ~10 %). El semáforo verde / ámbar / rojo (NR-01) vive en `Converters/StatusPalette.cs`: verde y ámbar significan solo normal / watch, y el estado nunca se comunica solo con color (cada chip lleva su texto). Chrome propio de ventana, rail azul de navegación, tarjetas con elevación y animación de entrada por página.
- **Check-up** (`CheckUpViewModel`, #28): de 1 a 6 indicadores en una sola sesión (los campos vacíos se omiten). Presión (sistólica y diastólica juntas), pulso, glucosa y temperatura por teclado; sueño por teclado o deslizador (paso de 0,5 h) y ánimo con 5 botones. Cada valor se valida con `IHealthEvaluator.Validate` antes de guardar y el resultado se evalúa al instante en el equipo. Se guarda en SQLite (`LocalHealthRecordStore`) con `SyncState = pending`, la versión de rangos y la nota opcional.
- **Acceso** (`AuthService`, #27): el primer acceso de cada astronauta es **en línea** (`POST /auth/login`); la app guarda su perfil, un hash PBKDF2-SHA256 propio (210 000 iteraciones, sal por cuenta) en `LocalAccount` y los tokens protegidos con **DPAPI** (usuario actual de Windows) en `TokenCache`. Sin conexión se valida la contraseña contra ese hash y se abre la sesión con el perfil guardado, devolviendo los tokens al cliente para que la sincronización retome al volver el enlace. Una respuesta 401/400/403 del servidor es definitiva (no se cae al hash local); si el servidor no responde, se usa la cuenta local si existe. Los datos se filtran por el `AstronautId` del servidor, así que varios astronautas pueden compartir el equipo sin ver datos ajenos. Cerrar sesión borra los tokens y conserva la cuenta local. Es un mecanismo de nivel demostración, como declara el contrato.
- **Sincronización** (`SyncEngine`, #32): servicio en segundo plano que sube lo pendiente sin bloquear la interfaz. Sincroniza al abrir la sesión, tras guardar (check-up, mensaje o perfil), al volver el enlace y cada 60 s con conexión, siempre para el astronauta con la sesión abierta. Sube los check-ups y las dosis de radiación en lotes de hasta 500, del más viejo al más nuevo (con sus mensajes y reportes, sin adelantarse al check-up que adjuntan, y la edición de perfil); marca `synced` solo los Id que el servidor devuelve en `accepted`, y `rejected` —con el motivo— los que rechaza (no se reintentan; se ven en el historial y en Mensajes y la barra de estado los cuenta); guarda `serverTime` como `lastPulledAt` (`SyncCursor`); baja los mensajes de control de misión; y si `rangesUpdateAvailable` descarga los rangos, los valida y los guarda (`RangesCache`) para usarlos en el próximo arranque. Si el servidor no responde, el token caducó o no hay red, lo pendiente se conserva y se reintenta; la barra de estado lo dice y tiene un botón **Sincronizar ahora**. En modo `Demo` el servidor de mentira acepta todo, y rechaza lo que contenga `[rechazar]` para poder ver un rechazo.
- **Figura humana** (`DashboardViewModel.Regions`, #48): el dashboard incluye un mapa del cuerpo con tres regiones (cabeza, corazón y abdomen) que salen del campo `bodyRegion` de cada indicador en `reference-ranges.json` (`IndicatorInfo.BodyRegion`). Cada región se colorea con el peor estado de las últimas lecturas de sus indicadores (gris si aún no hay datos) y al lado se lista el estado en texto con cada indicador, para no depender solo del color. Un clic en la región (o Enter con el teclado) abre el panel de información de su indicador más grave —o el primero de la región si no hay mediciones—; cada indicador de la lista también abre su ficha. Se quitó del dashboard el recuadro «Tendencias: próximamente», ya cubierto por Historial.
- **Reloj (Galaxy Watch 5)** (`WatchViewModel`, `Services/Watch/`, WR-DESKTOP #56): una sección «Reloj» del menú con dos pestañas, FC en vivo y Samsung Health, en vez de ventanas sueltas. La FC llega por el hub `/hubs/heartrate` con el **token del tripulante**, así que solo se ve la de su reloj; el resumen sale de `/api/salud/*`. La última lectura se guarda en `%LOCALAPPDATA%\Heartbeat\watch-last.json`: sin enlace (o si el reloj deja de enviar 10 s) se muestra con su hora y el aviso «Sin datos del reloj». La pantalla lleva siempre «Datos del reloj, no validados clínicamente». Las lecturas con `source = wearable` llevan un icono de reloj en el historial y un marcador en rombo en las gráficas (el origen se lee de la base local; que lleguen lecturas del reloj depende del import del backend, #55).
- **Idioma inglés / español** (`Localization/`): la interfaz arranca en inglés y el botón con el globo de la barra superior (muestra «ES» o «EN», el idioma al que cambia) alterna a español. Todos los textos viven en `Localization/Strings.cs` (código) y `Strings.Views.cs` (vistas), con su versión en los dos idiomas; el XAML los usa con `{loc:Loc clave}` y el código con `Loc.T("clave", valores)`. Al cambiar de idioma los textos se actualizan al momento: la pantalla actual se vuelve a crear (el login conserva lo escrito) y la elección se recuerda en `%LOCALAPPDATA%\Heartbeat\language.txt`. Para añadir un texto: una clave nueva con `en` y `es` (una prueba falla si falta alguna clave o si los huecos `{0}` no coinciden). Los nombres de indicadores y de los peligros NASA se traducen aquí; el resto de cada ficha de información (por qué importa, cómo medirlo, riesgos, fuentes) y la guía de cada lectura salen tal cual del contrato `reference-ranges.json`, que está en inglés.
- **Historial y gráficas** (`HistoryViewModel`, #35): lista de check-ups con su estado y, encima, una gráfica por indicador (la presión son dos: sistólica y diastólica, cada una con sus tramos) con los tramos sombreados en los colores de NR-01 y el nombre del estado al margen, de modo que no dependa solo del color. Selector de período: 7 días, 30 días o toda la misión; filtra la gráfica y la lista. Los tramos salen de `reference-ranges.json` (`IndicatorBands`, misma versión que la evaluación); `TrendChartBuilder` calcula el modelo (ejes, marcas, puntos y su estado) y el control `TrendChart` lo dibuja. Se dibuja con WPF nativo en lugar de LiveCharts2: su paquete WPF arrastra dependencias solo de .NET Framework (OpenTK, SkiaSharp.Views.WPF) y solo hay una versión de desarrollo para net10, con `TreatWarningsAsErrors` no compila.
- **Mensajes y reportes** (`MessagesViewModel`, #33): se redacta un mensaje libre o un reporte que adjunta un check-up (el asunto y el texto se rellenan con `ReportBuilder`, el mismo resumen que usa «Enviar reporte» del resultado, y se pueden editar; un check-up solo tiene un reporte). Todo se guarda en local y sale al sincronizar. **Enviados** muestra el estado de cada uno: en cola, enviado, leído o rechazado (con el motivo). **Bandeja** reúne las respuestas de control de misión y las alertas reconocidas que bajan por `/sync` (`Messages` con `Direction = ToAstronaut` más la tabla `AlertAcknowledgements`, idempotentes por Id y por check-up; una alerta de un check-up ajeno se ignora), con contador de no leídas y «Marcar como leído» (la lectura es local: el contrato no manda marcas del astronauta). Los esquemas nuevos se añaden a bases ya creadas (`SenderName`, `AlertAcknowledgements`). El estado «leído» de un saliente se muestra cuando llega `readAt`; hoy `/sync` no lo devuelve (ver el issue del backend). El servidor de demostración contesta a cada reporte y reconoce las alertas críticas para poder ver la bandeja.
- **Perfil** (`ProfileViewModel`, #34): el astronauta ve su cuenta y misión y edita solo su `displayName`, `callSign` y `notes` (límites 100 / 30 / 500 del contrato), también sin conexión. La edición queda pendiente con su `updatedAt` dentro de `LocalAccount.ProfileJson` (`ProfileCache`; no cambia el esquema de la base) y cuenta como pendiente en la barra de estado. `IProfileStore.GetPendingEdit` la entrega como `UpdateProfileRequest` para `SyncRequest.Profile`; al sincronizar, `MarkSynced` la limpia. Gana la edición más reciente (el servidor aplica `updatedAt >=`): un perfil del servidor posterior descarta la edición local al volver a entrar en línea.
- **Dashboard** (`DashboardViewModel`, #30): el estado actual de los 6 indicadores en una pantalla. Arriba el estado general (el peor de las últimas lecturas) y qué requiere atención; una tarjeta por indicador con su último valor, unidad, estado en color y texto, y hora («Hoy, 19:45», «Ayer…»); aviso destacado si hay críticos; y el botón **Nuevo check-up**. Como un check-up puede omitir indicadores, cada tarjeta usa la lectura más reciente de su indicador.
- **Resultado** (`CheckUpResultViewModel`, #29): al guardar un check-up se abre su pantalla de resultado, con el estado y la etiqueta de cada lectura (color + texto), la guía de las que no están en rango, el aviso destacado de escalamiento si hay críticos (a quién contactar y qué indicadores) y el botón **Enviar reporte a control de misión**, que encola un mensaje `report` con el check-up adjunto (uno por check-up; sin enlace sale al reconectar).
- **Modo demostración** (`Demo/`): mientras no estén los tickets reales hay un acceso de demostración (`ana`, `luis`, `mara` con contraseña `demo123`, sustituye #27), un historial de ejemplo sembrado en la base local la primera vez y una sincronización simulada (sustituye #32). Las pantallas solo conocen `IAuthenticator`, `IHealthRecordStore` e `ISyncStatusProvider`; el check-up se evalúa con el `HealthEvaluator` real de `Heartbeat.Shared`. El interruptor **Enlace con la Tierra** de la barra superior simula perder el enlace: apagado, los check-ups y mensajes quedan *Pendientes* y salen al volver a encenderlo.
- **Barra de estado**: en línea / sin conexión, último sync y pendientes. Se alimenta de `IConnectivityService` e `ISyncStatusProvider`; el motor de sync (#32) sustituirá la implementación provisional.

- **Asistente Claude** (`Assistant/`): el botón de chat del rail abre una ventana independiente que consulta
  `POST /api/crew/v1/assistant/chat` con la sesión del astronauta. La API key de Anthropic vive exclusivamente en
  `Heartbeat.Api`; nunca se almacena en el escritorio. El asistente explica el estado y las alertas que el backend ya
  calculó, pero no evalúa mediciones ni sustituye los procedimientos de emergencia. Requiere `CrewApi:Mode = Live`;
  si Claude o la red fallan, la evaluación local, las alertas y la sincronización siguen funcionando.

- **Panel de información y fuentes** (`Info/`): por indicador explica por qué importa, cómo medir y riesgos, y lista las fuentes NASA / agencias con enlaces que abren en el navegador. El contenido sale de `reference-ranges.json` (copia embebida en `Heartbeat.Shared`) a través de `IIndicatorInfoProvider`, con lectura estricta: una fuente no definida o con URL que no sea http(s) hace fallar las pruebas en vez de mostrar una ficha incompleta. Se abre en una ventana aparte, no modal y única, con `IIndicatorInfoPresenter.Show(indicatorId)`; el dashboard ya lo usa, y **el formulario de check-up (#30) solo tiene que inyectar `IIndicatorInfoPresenter` y llamar a `Show("heartRate")` desde su botón de ayuda** (al ser otra ventana, no se pierde lo escrito en el formulario).

### Modo simple (HB-FE-01)

El inicio abre con «Tu estado de un vistazo»: un semáforo global y uno por cada uno de los 5 peligros, cada uno con su «por qué», la etiqueta **Datos simulados**, una introducción de 3 pasos la primera vez (se recuerda por astronauta en `onboarding.json`, junto a la base local) y un glosario corto (mSv, CO₂, SANS, PVT…). El cálculo usa `RiskEngine` de `Heartbeat.Shared` (HB-BE-07) con los datos locales, así que funciona sin conexión y coincide con el servidor; los umbrales viven en el catálogo, no en la UI.

### Presión ocular y visión (HB-FE-04)

La pantalla **Visión** registra IOP (mmHg) y agudeza visual (logMAR), dibuja la tendencia de los últimos 30 días con las bandas del catálogo y muestra una nota corta sobre SANS y su relación con el CO₂, con su fuente y el estado de la evidencia (heurístico). Tendencia y correlación salen de `VisionAnalysis` (`Heartbeat.Shared`, HB-BE-05), así que coinciden con el servidor sin conexión. Las lecturas se guardan en la tabla local de ambiente (`IVisionStore`); todo es simulado y no es un diagnóstico.

### Añadir una pantalla

1. Crear `XxxViewModel : PageViewModel` y registrarlo con `AddTransient` en `ServiceCollectionExtensions`.
2. Crear su vista y asociarla en `Views/PageTemplates.xaml`.
3. Si va en el menú lateral, añadirla a `MainWindowViewModel.NavigationItems`.

## Servidor real o modo demostración

`src/Heartbeat.Desktop/appsettings.json` (sección `CrewApi`) decide con quién habla la app. Por defecto es **`Demo`**: tres astronautas ficticios (`ana`, `luis`, `mara`, contraseña `demo123`), sin servidor y con un historial de ejemplo. Para usar la Crew API real:

```bash
# PowerShell, con la API corriendo (por defecto en http://localhost:5027)
$env:CrewApi__Mode = "Live"
$env:CrewApi__BaseUrl = "http://localhost:5027/"
dotnet run --project src/Heartbeat.Desktop/Heartbeat.Desktop.csproj
```

En modo `Live` no se siembran datos de ejemplo y se entra con las cuentas de astronauta del servidor. El interruptor **Enlace con la Tierra** (también en el login) simula perder la conexión para probar el acceso sin conexión.

## Flujo de trabajo

- `dev` es la rama por defecto y **solo cambia por pull request**. `main` no se usa.
- Cada ticket va en su propia rama, `<n>-descripcion-corta` (por ejemplo `26-estructura-wpf`), que sale de `dev` y vuelve a `dev` por PR.
- El PR se cierra con `Closes AliaJimenez/HeartbeatBackend#<n>` en la descripción.
- CI: `dotnet build` y `dotnet test` en cada PR y push hacia `dev`.

Más detalle en [`docs/FLUJO-DE-TICKETS.md`](https://github.com/AliaJimenez/HeartbeatBackend/blob/dev/docs/FLUJO-DE-TICKETS.md).
