using Heartbeat.Desktop.Assistant;
using Heartbeat.Desktop.Localization;

namespace Heartbeat.Desktop.Tests;

[Collection(LanguageCollection.Name)]
public sealed class AssistantViewModelTests : IDisposable
{
    private sealed class FakeClient : IAssistantApiClient
    {
        public string? Question { get; private set; }
        public string? Language { get; private set; }
        public AssistantApiException? Error { get; init; }

        public Task<AssistantResponse> AskAsync(string question, string language, CancellationToken cancellationToken = default)
        {
            Question = question;
            Language = language;
            if (Error is not null)
            {
                throw Error;
            }

            return Task.FromResult(new AssistantResponse(
                "El estado actual es normal.", DateTimeOffset.UtcNow, "Anthropic", "claude-test",
                ["¿Qué datos son simulados?"], "No diagnóstico"));
        }
    }

    public AssistantViewModelTests() => LocalizationManager.Instance.SetLanguage(AppLanguage.Spanish);

    public void Dispose() => LocalizationManager.Instance.SetLanguage(AppLanguage.Spanish);

    [Fact]
    public async Task Sending_a_question_adds_both_messages_and_suggestions()
    {
        var client = new FakeClient();
        var viewModel = new AssistantViewModel(client) { Question = "  ¿Cuál es mi estado?  " };

        await viewModel.SendCommand.ExecuteAsync(null);

        Assert.Equal("¿Cuál es mi estado?", client.Question);
        Assert.Equal("es", client.Language);
        Assert.Equal(3, viewModel.Messages.Count); // bienvenida, pregunta y respuesta
        Assert.True(viewModel.Messages[1].IsUser);
        Assert.Equal("El estado actual es normal.", viewModel.Messages[2].Text);
        Assert.Equal("¿Qué datos son simulados?", Assert.Single(viewModel.SuggestedQuestions));
        Assert.Empty(viewModel.ErrorText);
        Assert.False(viewModel.IsSending);
    }

    [Fact]
    public async Task Provider_failures_are_explained_without_removing_the_users_question()
    {
        var client = new FakeClient
        {
            Error = new AssistantApiException(AssistantError.Disabled, "disabled"),
        };
        var viewModel = new AssistantViewModel(client) { Question = "¿Hay alertas?" };

        await viewModel.SendCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Messages.Count);
        Assert.Contains("no está habilitado", viewModel.ErrorText);
        Assert.False(viewModel.IsSending);
    }
}
