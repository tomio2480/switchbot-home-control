using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SwitchBotHomeControl.Api;
using SwitchBotHomeControl.Notifications;

namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// Every 10 minutes, reads all thermo-hygrometers and posts to Discord
/// when any of them is neither "何も感じない" nor "快い".
/// </summary>
public class DiscomfortMonitorService : BackgroundService
{
    private const int SwitchBotSuccessCode = 100;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);

    private readonly SwitchBotClient _switchBot;
    private readonly DiscordWebhookClient _discord;
    private readonly ILogger<DiscomfortMonitorService> _logger;

    public DiscomfortMonitorService(
        SwitchBotClient switchBot,
        DiscordWebhookClient discord,
        ILogger<DiscomfortMonitorService> logger)
    {
        _switchBot = switchBot;
        _discord = discord;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // A failed cycle (network outage, API error) must not stop monitoring;
                // the next tick retries from scratch.
                _logger.LogError(ex, "Discomfort index check failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        var devices = await _switchBot.GetDevicesAsync();
        if (devices.StatusCode != SwitchBotSuccessCode)
        {
            throw new InvalidOperationException($"Failed to get device list: {devices.Message}");
        }

        var readings = new List<MeterReading>();
        foreach (var meter in devices.Body.DeviceList.Where(d => MeterStatus.IsThermoHygrometer(d.DeviceType)))
        {
            var reading = await ReadMeterAsync(meter.DeviceId, meter.DeviceName);
            if (reading != null)
            {
                readings.Add(reading);
            }
        }

        var alerts = DiscomfortAlert.FromReadings(readings);
        if (alerts.Count == 0)
        {
            return;
        }

        await _discord.SendAsync(DiscomfortAlert.FormatMessage(alerts, DateTime.Now), cancellationToken);
    }

    private async Task<MeterReading?> ReadMeterAsync(string deviceId, string deviceName)
    {
        try
        {
            var status = await _switchBot.GetDeviceStatusAsync(deviceId);
            if (status.StatusCode != SwitchBotSuccessCode)
            {
                _logger.LogWarning("Skipped {DeviceName}: status request failed ({Message})", deviceName, status.Message);
                return null;
            }

            if (!MeterStatus.TryRead(status.Body, out var temperature, out var humidity))
            {
                _logger.LogWarning("Skipped {DeviceName}: no valid temperature/humidity (battery may be empty)", deviceName);
                return null;
            }

            return new MeterReading(deviceName, temperature, humidity);
        }
        catch (HttpRequestException ex)
        {
            // One unreachable meter should not hide alerts from the others
            _logger.LogWarning(ex, "Skipped {DeviceName}: status request failed", deviceName);
            return null;
        }
    }
}
