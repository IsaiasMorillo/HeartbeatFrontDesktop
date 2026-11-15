using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Heartbeat.Client;
using Heartbeat.Desktop.Services.Auth;

namespace Heartbeat.Desktop.Assistant;

/// <summary>Llama al endpoint propio de Heartbeat. La API key de Anthropic nunca llega al escritorio.</summary>
public sealed class AssistantApiClient(HttpClient http, ICrewApiClient crewApi, CrewApiOptions options) : IAssistantApiClient, IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AssistantResponse> AskAsync(string question, string language, CancellationToken cancellationToken = default)
    {
        if (options.IsDemo)
        {
            throw new AssistantApiException(AssistantError.DemoMode, "The assistant requires the live API.");
        }

        if (string.IsNullOrWhiteSpace(crewApi.AccessToken))
        {
            throw new AssistantApiException(AssistantError.Unauthorized, "No online session is available.");
        }

        using HttpResponseMessage first = await SendAsync(question, language, cancellationToken);
        if (first.StatusCode != HttpStatusCode.Unauthorized)
        {
            return await ReadAsync(first, cancellationToken);
        }

        try
        {
            await crewApi.RefreshAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            throw new AssistantApiException(AssistantError.Unauthorized, "The online session expired.", ex);
        }

        using HttpResponseMessage retry = await SendAsync(question, language, cancellationToken);
        return await ReadAsync(retry, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(string question, string language, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/crew/v1/assistant/chat")
            {
                Content = JsonContent.Create(new AssistantRequest(question, language), options: Json),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", crewApi.AccessToken);
            return await http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new AssistantApiException(AssistantError.Network, "The Heartbeat API could not be reached.", ex);
        }
    }

    private static async Task<AssistantResponse> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            AssistantResponse? result = await response.Content.ReadFromJsonAsync<AssistantResponse>(Json, cancellationToken);
            return result ?? throw new AssistantApiException(AssistantError.Unavailable, "The assistant returned an empty response.");
        }

        AssistantError error = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AssistantError.Unauthorized,
            HttpStatusCode.ServiceUnavailable => AssistantError.Disabled,
            _ => AssistantError.Unavailable,
        };
        throw new AssistantApiException(error, $"Assistant request failed with HTTP {(int)response.StatusCode}.");
    }

    public void Dispose() => http.Dispose();
}
