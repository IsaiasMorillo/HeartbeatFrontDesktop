namespace Heartbeat.Desktop.Assistant;

public interface IAssistantApiClient
{
    Task<AssistantResponse> AskAsync(string question, string language, CancellationToken cancellationToken = default);
}

public enum AssistantError
{
    DemoMode,
    Unauthorized,
    Disabled,
    Unavailable,
    Network,
}

public sealed class AssistantApiException(AssistantError error, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public AssistantError Error { get; } = error;
}
