using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heartbeat.Desktop.Localization;

namespace Heartbeat.Desktop.Assistant;

public sealed record AssistantMessage(string Text, bool IsUser, string Author);

public sealed partial class AssistantViewModel : ObservableObject
{
    private readonly IAssistantApiClient _client;

    public AssistantViewModel(IAssistantApiClient client)
    {
        _client = client;
        Messages.Add(new AssistantMessage(Loc.T("assistant.welcome"), false, "Claude"));
    }

    public ObservableCollection<AssistantMessage> Messages { get; } = [];

    public ObservableCollection<string> SuggestedQuestions { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _question = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isSending;

    [ObservableProperty]
    private string _errorText = string.Empty;

    private bool CanSend() => !IsSending && !string.IsNullOrWhiteSpace(Question) && Question.Length <= 1_000;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        string question = Question.Trim();
        Question = string.Empty;
        ErrorText = string.Empty;
        Messages.Add(new AssistantMessage(question, true, Loc.T("assistant.you")));
        IsSending = true;

        try
        {
            AssistantResponse response = await _client.AskAsync(question, Loc.Culture.TwoLetterISOLanguageName);
            Messages.Add(new AssistantMessage(response.Answer, false, "Claude"));
            SuggestedQuestions.Clear();
            foreach (string suggestion in response.SuggestedQuestions)
            {
                SuggestedQuestions.Add(suggestion);
            }
        }
        catch (AssistantApiException ex)
        {
            ErrorText = Loc.T(ex.Error switch
            {
                AssistantError.DemoMode => "assistant.error.demo",
                AssistantError.Unauthorized => "assistant.error.session",
                AssistantError.Disabled => "assistant.error.disabled",
                AssistantError.Network => "assistant.error.network",
                _ => "assistant.error.unavailable",
            });
        }
        catch (Exception)
        {
            ErrorText = Loc.T("assistant.error.unavailable");
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void UseSuggestion(string suggestion) => Question = suggestion;
}
