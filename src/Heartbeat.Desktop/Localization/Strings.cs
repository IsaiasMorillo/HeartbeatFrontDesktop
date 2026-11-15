namespace Heartbeat.Desktop.Localization;

/// <summary>
/// Tabla de textos de la interfaz: cada clave tiene su versión en inglés y en español.
/// Los textos de las vistas XAML están en <c>Strings.Views.cs</c>; los de código (ViewModels, servicios) aquí.
/// Los textos con huecos usan <c>{0}</c>, <c>{1}</c>… y se piden con <c>Loc.T(clave, valores)</c>.
/// </summary>
public static partial class Strings
{
    private static readonly Dictionary<string, (string En, string Es)> Table = Build();

    /// <summary>Todas las claves con sus dos versiones (lo usan las pruebas para comprobar que no falta ninguna).</summary>
    public static IReadOnlyDictionary<string, (string En, string Es)> All => Table;

    public static string Get(string key, AppLanguage language) =>
        Table.TryGetValue(key, out (string En, string Es) text)
            ? (language == AppLanguage.Spanish ? text.Es : text.En)
            : key;

    private static Dictionary<string, (string En, string Es)> Build()
    {
        Dictionary<string, (string En, string Es)> table = new(StringComparer.Ordinal);
        AddCode(table);
        AddViews(table);
        AddWatch(table);
        AddSimple(table);
        AddVision(table);
        return table;
    }

    private static void AddCode(Dictionary<string, (string En, string Es)> t)
    {
        // Aviso médico y estados
        t["app.disclaimer"] = ("Self-assessment tool, not a medical diagnosis", "Herramienta de autoevaluación, no diagnóstico médico");
        t["status.normal"] = ("Normal", "Normal");
        t["status.watch"] = ("Watch", "Vigilancia");
        t["status.critical"] = ("Critical", "Crítico");
        t["status.noData"] = ("No data", "Sin datos");
        t["status.overall"] = ("Overall status: {0}", "Estado general: {0}");
        t["sync.queued"] = ("Queued (no link)", "En cola (sin enlace)");

        // Idioma
        t["lang.toggleLabel"] = ("ES", "EN");
        t["lang.toggleTip"] = ("Switch to Spanish", "Cambiar a inglés");

        // Menú
        t["nav.dashboard"] = ("Dashboard", "Dashboard");
        t["nav.checkUp"] = ("Check-up", "Check-up");
        t["nav.history"] = ("History", "Historial");
        t["nav.messages"] = ("Messages", "Mensajes");
        t["nav.radiation"] = ("Radiation", "Radiación");
        t["nav.profile"] = ("Profile", "Perfil");
        t["main.sessionOffline"] = ("Offline session", "Sesión sin conexión");
        t["main.restore"] = ("Restore", "Restaurar");

        // Asistente Claude
        t["assistant.welcome"] = ("I can explain your current Heartbeat status and active alerts in plain language. I cannot diagnose or replace emergency procedures.", "Puedo explicar tu estado actual de Heartbeat y las alertas activas con lenguaje sencillo. No puedo diagnosticar ni sustituir los procedimientos de emergencia.");
        t["assistant.you"] = ("You", "Tú");
        t["assistant.error.demo"] = ("The assistant needs a live connection to Heartbeat.Api. Change CrewApi:Mode to Live.", "El asistente necesita conexión real con Heartbeat.Api. Cambia CrewApi:Mode a Live.");
        t["assistant.error.session"] = ("Your online session expired. Sign in again while connected.", "Tu sesión en línea venció. Inicia sesión de nuevo con conexión.");
        t["assistant.error.disabled"] = ("The Claude assistant is not enabled or configured on the server.", "El asistente Claude no está habilitado o configurado en el servidor.");
        t["assistant.error.network"] = ("Heartbeat.Api could not be reached. Alerts and offline evaluation still work.", "No se pudo contactar Heartbeat.Api. Las alertas y la evaluación sin conexión siguen funcionando.");
        t["assistant.error.unavailable"] = ("Claude is temporarily unavailable. Try again later; the displayed Heartbeat status remains valid.", "Claude no está disponible temporalmente. Inténtalo más tarde; el estado mostrado por Heartbeat sigue siendo válido.");

        // Barra de estado
        t["bar.syncing"] = ("Syncing…", "Sincronizando…");
        t["bar.rejected.one"] = ("1 rejected", "1 rechazado");
        t["bar.rejected.many"] = ("{0} rejected", "{0} rechazados");
        t["bar.online"] = ("Online", "En línea");
        t["bar.offline"] = ("Offline", "Sin conexión");
        t["bar.lastSync"] = ("Last sync: {0}", "Último sync: {0}");
        t["bar.neverSynced"] = ("Last sync: never", "Último sync: nunca");
        t["bar.pending"] = ("Pending: {0}", "Pendientes: {0}");

        // Login y acceso
        t["login.title"] = ("Sign in", "Iniciar sesión");
        t["login.desc"] = ("Sign in to your astronaut profile. It also works without a link to Earth.", "Entra a tu perfil de astronauta. También funciona sin enlace con la Tierra.");
        t["login.demoHint"] = ("Demo mode · users: {0} · password: {1}", "Modo demostración · usuarios: {0} · contraseña: {1}");
        t["login.offlineNotice"] = ("Offline: you can only sign in with an account you have already used on this computer. The first sign-in must be online.", "Sin conexión: solo puedes entrar con una cuenta que ya hayas usado en este equipo. El primer acceso debe ser en línea.");
        t["login.signIn"] = ("Sign in", "Entrar");
        t["login.signingIn"] = ("Signing in…", "Entrando…");
        t["login.err.unexpected"] = ("Could not sign in because of an unexpected problem. Try again.", "No se pudo iniciar sesión por un problema inesperado. Inténtalo de nuevo.");
        t["auth.missing"] = ("Enter your username and password.", "Escribe tu usuario y tu contraseña.");
        t["auth.invalid"] = ("Incorrect username or password.", "Usuario o contraseña incorrectos.");
        t["auth.firstOnline"] = ("Offline. The first sign-in on this computer must be done online; afterward you can sign in without a link.", "Sin conexión. El primer acceso en este equipo debe hacerse en línea; después podrás entrar sin enlace.");
        t["auth.server"] = ("Could not reach the server. The first sign-in on this computer requires a connection: try again in a few minutes.", "No se pudo contactar con el servidor. El primer acceso en este equipo requiere conexión: inténtalo de nuevo en unos minutos.");

        // Dashboard
        t["dash.greeting"] = ("Hello", "Hola");
        t["dash.greetingName"] = ("Hello, {0}", "Hola, {0}");
        t["dash.dateFormat"] = ("dddd, MMMM d", "dddd, d 'de' MMMM");
        t["dash.noMeasurements"] = ("No measurements yet", "Sin mediciones todavía");
        t["dash.firstCheckUp"] = ("Log your first check-up to see the status of your indicators here.", "Registra tu primer check-up para ver aquí el estado de tus indicadores.");
        t["dash.critical.one"] = ("1 critical", "1 crítico");
        t["dash.critical.many"] = ("{0} critical", "{0} críticos");
        t["dash.watch.one"] = ("1 on watch", "1 en vigilancia");
        t["dash.watch.many"] = ("{0} on watch", "{0} en vigilancia");
        t["dash.allInRange"] = ("All indicators in range", "Todos los indicadores en rango");
        t["dash.someInRange"] = ("{0} of 6 indicators measured, all in range", "{0} de 6 indicadores medidos, todos en rango");
        t["dash.lastCheckUp"] = ("{0} · Last check-up: {1}", "{0} · Último check-up: {1}");
        t["dash.card.none"] = ("No measurements", "Sin mediciones");
        t["dash.card.hint"] = ("Log a check-up to see the status of this indicator.", "Registra un check-up para ver el estado de este indicador.");
        t["region.head"] = ("Head", "Cabeza");
        t["region.heart"] = ("Heart", "Corazón");
        t["region.abdomen"] = ("Abdomen", "Abdomen");
        t["region.noData"] = ("{0}: no measurements", "{0}: sin mediciones");

        // Indicadores y unidades
        t["ind.bloodPressure"] = ("Blood pressure", "Presión arterial");
        t["ind.heartRate"] = ("Heart rate", "Frecuencia cardíaca");
        t["ind.bloodSugar"] = ("Blood sugar", "Glucosa");
        t["ind.bodyTemperature"] = ("Body temperature", "Temperatura corporal");
        t["ind.sleep"] = ("Sleep", "Sueño");
        t["ind.mood"] = ("Mood", "Estado de ánimo");
        t["unit.heartRate"] = ("bpm", "lpm");
        t["hazard.spaceRadiation"] = ("Space radiation", "Radiación espacial");
        t["hazard.isolationAndConfinement"] = ("Isolation and confinement", "Aislamiento y confinamiento");
        t["hazard.distanceFromEarth"] = ("Distance from Earth", "Distancia de la Tierra");
        t["hazard.gravityFields"] = ("Gravity fields", "Campos de gravedad");
        t["hazard.hostileClosedEnvironment"] = ("Hostile and closed environments", "Entorno cerrado y hostil");
        t["info.rangesVersion"] = ("Reference ranges v{0}", "Rangos de referencia v{0}");
        t["info.linkFailed"] = ("Could not open the link. Copy this address into your browser: {0}", "No se pudo abrir el enlace. Copia esta dirección en tu navegador: {0}");
        t["when.today"] = ("Today, {0}", "Hoy, {0}");
        t["when.yesterday"] = ("Yesterday, {0}", "Ayer, {0}");
        t["when.daysAgo"] = ("{0} days ago, {1}", "Hace {0} días, {1}");

        // Check-up
        t["checkup.title"] = ("Check-up", "Check-up");
        t["mood.1"] = ("Very bad", "Muy mal");
        t["mood.2"] = ("Bad", "Mal");
        t["mood.3"] = ("Okay", "Normal");
        t["mood.4"] = ("Good", "Bien");
        t["mood.5"] = ("Very good", "Muy bien");
        t["checkup.err.signIn"] = ("Sign in to save a check-up.", "Inicia sesión para guardar un check-up.");
        t["checkup.err.none"] = ("Log at least one indicator. You can skip the ones you do not measure today.", "Registra al menos un indicador. Puedes omitir los que no midas hoy.");
        t["checkup.err.review"] = ("Review: {0}.", "Revisa: {0}.");
        t["checkup.prob.range"] = ("{0} (outside the measurable range)", "{0} (fuera de lo que se puede medir)");
        t["checkup.prob.bpBoth"] = ("{0} (enter systolic and diastolic)", "{0} (escribe la sistólica y la diastólica)");
        t["checkup.prob.bpNumbers"] = ("{0} (enter numbers)", "{0} (escribe números)");
        t["checkup.prob.number"] = ("{0} (enter a number)", "{0} (escribe un número)");

        // Resultado del check-up
        t["result.count.one"] = ("1 indicator", "1 indicador");
        t["result.count.many"] = ("{0} indicators", "{0} indicadores");
        t["result.saved.sent"] = ("Saved at {0} · {1} · Sent to mission control", "Guardado a las {0} · {1} · Enviado a control de misión");
        t["result.saved.queued"] = ("Saved at {0} · {1} · Will sync when the link is available", "Guardado a las {0} · {1} · Se sincronizará cuando haya enlace");
        t["result.critical.title"] = ("Critical result", "Resultado crítico");
        t["result.critical.contact"] = ("Your crew medical officer", "Tu oficial médico de la tripulación");
        t["result.critical.message"] = ("Tell them now. This result is flagged for the flight surgeons on the ground and will arrive at the next sync.", "Avísale ahora. Este resultado queda marcado para los cirujanos de vuelo en tierra y les llegará en la próxima sincronización.");
        t["result.watch.message"] = ("Some readings are on watch: measure them again at your next check-up. Mission control will see the result after the next sync.", "Hay lecturas en vigilancia: vuelve a medirlas en tu próximo check-up. Control de misión verá el resultado tras la próxima sincronización.");
        t["result.reportAlready"] = ("You already sent a report of this check-up to mission control.", "Ya enviaste un reporte de este check-up a control de misión.");
        t["result.reportSent"] = ("Report sent to mission control.", "Reporte enviado a control de misión.");
        t["result.reportQueued"] = ("Report queued: it will go out to mission control when the link is available.", "Reporte en cola: saldrá hacia control de misión cuando haya enlace.");

        // Medidas corporales
        t["body.noChange"] = ("= no change", "= sin cambios");
        t["body.height"] = ("Height", "Altura");
        t["body.neck"] = ("Neck", "Cuello");
        t["body.chest"] = ("Chest", "Pecho");
        t["body.waist"] = ("Waist", "Cintura");
        t["body.thigh"] = ("Thigh", "Muslo");
        t["body.calf"] = ("Calf", "Pantorrilla");
        t["body.err.signIn"] = ("Sign in to save measurements.", "Inicia sesión para guardar medidas.");
        t["body.err.number"] = ("{0}: enter a number in centimeters.", "{0}: escribe un número en centímetros.");
        t["body.err.range"] = ("{0}: the value must be between {1:0} and {2:0} cm.", "{0}: el valor debe estar entre {1:0} y {2:0} cm.");
        t["body.err.empty"] = ("Enter at least one measurement.", "Escribe al menos una medida.");
        t["body.saved.one"] = ("Measurement saved.", "Medida guardada.");
        t["body.saved.many"] = ("{0} measurements saved.", "{0} medidas guardadas.");

        // Historial
        t["hist.range.week"] = ("last 7 days", "últimos 7 días");
        t["hist.range.month"] = ("last 30 days", "últimos 30 días");
        t["hist.range.all"] = ("entire mission", "toda la misión");
        t["hist.empty.none"] = ("No check-ups yet. Log the first one from the Check-up section.", "Aún no hay check-ups. Registra el primero desde la sección Check-up.");
        t["hist.empty.period"] = ("No check-ups in this period.", "No hay check-ups en este período.");
        t["hist.reading.one"] = ("1 reading", "1 lectura");
        t["hist.reading.many"] = ("{0} readings", "{0} lecturas");
        t["hist.diastolic"] = (" · diastolic", " · diastólica");
        t["hist.systolic"] = (" · systolic", " · sistólica");
        t["hist.rejected"] = ("Rejected: {0}", "Rechazado: {0}");
        t["hist.synced"] = ("Synced", "Sincronizado");
        t["hist.pending"] = ("Pending sync", "Pendiente de sincronizar");
        t["chart.empty"] = ("No readings in this period", "Sin lecturas en este período");

        // Mensajes
        t["msg.inbox"] = ("Inbox ({0})", "Bandeja ({0})");
        t["msg.inboxPlain"] = ("Inbox", "Bandeja");
        t["msg.feedback.report"] = ("Report saved: it goes out to mission control when the link is available.", "Reporte guardado: sale hacia control de misión cuando haya enlace.");
        t["msg.feedback.message"] = ("Message saved: it goes out to mission control when the link is available.", "Mensaje guardado: sale hacia control de misión cuando haya enlace.");
        t["msg.attach.line"] = ("{0} · {1} · {2} rd.", "{0} · {1} · {2} lect.");
        t["msg.attach.reported"] = (" · already reported", " · ya reportado");
        t["msg.fromMc"] = ("Message from mission control", "Mensaje de control de misión");
        t["msg.alertAck"] = ("{0} acknowledged the alert for your check-up of {1}.", "{0} reconoció la alerta de tu check-up del {1}.");
        t["msg.alertAckNote"] = ("{0} acknowledged the alert for your check-up of {1}: {2}", "{0} reconoció la alerta de tu check-up del {1}: {2}");
        t["msg.alertTitle"] = ("Alert acknowledged", "Alerta reconocida");
        t["msg.status.rejected"] = ("Rejected: {0}", "Rechazado: {0}");
        t["msg.status.read"] = ("Read {0}", "Leído {0}");
        t["msg.status.sent"] = ("Sent {0}", "Enviado {0}");
        t["msg.subject.report"] = ("Check-up report: {0}", "Reporte de check-up: {0}");
        t["msg.subject.plain"] = ("Message", "Mensaje");
        t["report.subject"] = ("Check-up {0} · {1}", "Check-up {0} · {1}");
        t["report.overall"] = ("Overall status: {0}.", "Estado general: {0}.");
        t["report.note"] = ("Note: ", "Nota: ");

        // Perfil
        t["profile.err.nameRequired"] = ("Name is required.", "El nombre es obligatorio.");
        t["profile.err.max"] = ("Maximum {0} characters.", "Máximo {0} caracteres.");
        t["profile.err.signIn"] = ("Sign in to edit your profile.", "Inicia sesión para editar tu perfil.");
        t["profile.err.notFound"] = ("Your profile was not found on this computer. Sign in again while online.", "No se encontró tu perfil en este equipo. Vuelve a iniciar sesión con conexión.");
        t["profile.saved.local"] = ("Changes saved on this computer.", "Cambios guardados en este equipo.");
        t["profile.saved.pending"] = ("Changes saved on this computer. They will be sent at the next sync.", "Cambios guardados en este equipo. Se enviarán en la próxima sincronización.");
        t["profile.synced"] = ("Profile synced.", "Perfil sincronizado.");
        t["profile.updated"] = ("Last edited: {0}", "Última edición: {0}");
        t["profile.env.station"] = ("space station", "estación espacial");
        t["profile.env.moon"] = ("the Moon", "Luna");
        t["profile.env.mars"] = ("Mars", "Marte");
        t["profile.missionDay"] = ("Mission day {0}", "Día {0} de la misión");
        t["profile.missionStarts"] = ("Mission starts on {0}", "La misión comienza el {0}");

        // Radiación
        t["rad.err.dose"] = ("Enter the dose in mSv, for example 0.42.", "Escribe la dosis en mSv, por ejemplo 0.42.");
        t["rad.err.positive"] = ("The dose must be greater than 0 mSv.", "La dosis debe ser mayor que 0 mSv.");
        t["rad.err.max"] = ("The dose cannot exceed {0:0} mSv per entry.", "La dosis no puede superar {0:0} mSv por registro.");
        t["rad.err.link"] = ("Could not open the link in the browser.", "No se pudo abrir el enlace en el navegador.");
        t["rad.sentAt"] = ("Sent {0}", "Enviada {0}");

        // Sincronización
        t["syncmsg.failed"] = ("Could not sync: {0} It will retry.", "No se pudo sincronizar: {0} Se reintentará.");
        t["syncmsg.signInAgain"] = ("Sign in again while online to sync.", "Vuelve a iniciar sesión con conexión para sincronizar.");
        t["syncmsg.expired"] = ("Your session with the server expired. Sign in again while online.", "La sesión con el servidor caducó. Vuelve a iniciar sesión con conexión.");
        t["syncmsg.noResponse"] = ("No response from the server. It will retry.", "Sin respuesta del servidor. Se reintentará.");
        t["syncmsg.unconfirmed"] = ("The server did not confirm the items sent.", "El servidor no confirmó los elementos enviados.");
        t["syncmsg.kind.checkUp"] = ("check-up", "check-up");
        t["syncmsg.kind.message"] = ("message", "mensaje");
        t["syncmsg.kind.dose"] = ("radiation dose", "dosis de radiación");

        // Servidor de demostración
        t["demo.badCredentials"] = ("Invalid demo credentials.", "Credenciales de demostración inválidas.");
        t["demo.rejectedCheckUp"] = ("Rejected by the demo server (the note contains {0}).", "Rechazado por el servidor de demostración (la nota contiene {0}).");
        t["demo.rejectedMessage"] = ("Rejected by the demo server (the message contains {0}).", "Rechazado por el servidor de demostración (el mensaje contiene {0}).");
        t["demo.replyBody"] = ("We received your report. We are following your indicators; let us know if anything gets worse.", "Recibimos tu reporte. Seguimos tus indicadores; avísanos si algo empeora.");
        t["demo.mcName"] = ("Mission control (demo)", "Control de misión (demo)");
        t["demo.alertNote"] = ("Alert received by the flight surgeons.", "Alerta recibida por los cirujanos de vuelo.");

    }
}
