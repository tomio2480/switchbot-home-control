using System.Text.Json;
using SwitchBotHomeControl.Notifications;

namespace SwitchBotHomeControl.Tests;

public class DiscordWebhookClientTests
{
    [Theory]
    [InlineData("https://discord.com/api/webhooks/123/abc")]
    [InlineData("https://discordapp.com/api/webhooks/123/abc")]
    public void IsValidWebhookUrl_AcceptsHttpsWebhookUrl(string url)
    {
        Assert.True(DiscordWebhookClient.IsValidWebhookUrl(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("your_webhook_url_here")]
    [InlineData("http://discord.com/api/webhooks/123/abc")]
    [InlineData("https://discord.com/channels/123/456")]
    public void IsValidWebhookUrl_RejectsOtherValues(string? url)
    {
        Assert.False(DiscordWebhookClient.IsValidWebhookUrl(url));
    }

    [Fact]
    public void BuildPayload_SuppressesMentions()
    {
        using var payload = JsonDocument.Parse(DiscordWebhookClient.BuildPayload("@everyone 暑い"));
        var root = payload.RootElement;

        Assert.Equal("@everyone 暑い", root.GetProperty("content").GetString());
        Assert.Equal(0, root.GetProperty("allowed_mentions").GetProperty("parse").GetArrayLength());
    }
}
