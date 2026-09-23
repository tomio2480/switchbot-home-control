using System.Text;
using System.Text.Json;

namespace SwitchBotHomeControl.Notifications;

public class DiscordWebhookClient
{
    private const string WebhookPathPrefix = "/api/webhooks/";
    private readonly Uri _webhookUrl;
    private readonly HttpClient _httpClient;

    public DiscordWebhookClient(string webhookUrl)
    {
        if (!IsValidWebhookUrl(webhookUrl))
        {
            throw new ArgumentException("Discord webhook URL must be an https URL under /api/webhooks/", nameof(webhookUrl));
        }

        _webhookUrl = new Uri(webhookUrl);
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public static bool IsValidWebhookUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.AbsolutePath.StartsWith(WebhookPathPrefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Builds the webhook body. Mentions are disabled so that device names such as "@everyone" never ping.
    /// </summary>
    public static string BuildPayload(string content)
    {
        return JsonSerializer.Serialize(new
        {
            content,
            allowed_mentions = new { parse = Array.Empty<string>() }
        });
    }

    public async Task SendAsync(string content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _webhookUrl)
        {
            Content = new StringContent(BuildPayload(content), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
