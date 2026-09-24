using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SwitchBotHomeControl.Api;
using SwitchBotHomeControl.Notifications;

namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// Every 10 minutes, reads all thermo-hygrometers, records the readings,
/// updates the tray icon and posts sensation level changes to Discord (when configured).
/// </summary>
public class DiscomfortMonitorService : BackgroundService
{
    private const int SwitchBotSuccessCode = 100;

    private readonly SwitchBotClient _switchBot;
    private readonly MeterHistoryStore _history;
    private readonly NotificationStateStore _notificationState;
    private readonly LatestMeterReadings _latest;
    private readonly ILogger<DiscomfortMonitorService> _logger;
    private readonly DiscordWebhookClient? _discord;

    public DiscomfortMonitorService(
        SwitchBotClient switchBot,
        MeterHistoryStore history,
        NotificationStateStore notificationState,
        LatestMeterReadings latest,
        ILogger<DiscomfortMonitorService> logger,
        DiscordWebhookClient? discord = null)
    {
        _switchBot = switchBot;
        _history = history;
        _notificationState = notificationState;
        _latest = latest;
        _logger = logger;
        _discord = discord;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(DiscomfortChangeDetector.Interval);
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
        var now = DateTimeOffset.Now;
        var records = await ReadMetersAsync(now);
        _latest.Update(new MeterSnapshot(now, records));
        if (records.Count == 0)
        {
            return;
        }

        _history.Append(records);
        if (_discord == null)
        {
            return;
        }

        var history = _history.Read(now - DiscomfortChangeDetector.LookBack, now);
        var changes = DiscomfortChangeDetector.DetectAll(records, history, _notificationState.GetLastNotified);
        if (changes.Count == 0)
        {
            return;
        }

        await _discord.SendAsync(DiscomfortChange.FormatMessage(changes, now.LocalDateTime), cancellationToken);
        // Saved only after a successful post, so that a failed post is retried in a later cycle
        _notificationState.SetLastNotified(changes.Select(c => (c.Current.DeviceId, c.Current.Level)));
    }

    /// <summary>
    /// Readings of every meter that returned valid values; empty when the device list is unavailable.
    /// </summary>
    private async Task<IReadOnlyList<MeterRecord>> ReadMetersAsync(DateTimeOffset now)
    {
        try
        {
            var devices = await _switchBot.GetDevicesAsync();
            if (devices.StatusCode != SwitchBotSuccessCode)
            {
                _logger.LogError("Failed to get device list: {Message}", devices.Message);
                return Array.Empty<MeterRecord>();
            }

            var records = new List<MeterRecord>();
            foreach (var meter in devices.Body.DeviceList.Where(d => MeterStatus.IsThermoHygrometer(d.DeviceType)))
            {
                var record = await ReadMeterAsync(now, meter.DeviceId, meter.DeviceName);
                if (record != null)
                {
                    records.Add(record);
                }
            }

            return records;
        }
        catch (Exception ex)
        {
            // Report the failure as an empty snapshot so the tray icon does not keep showing stale values
            _logger.LogError(ex, "Failed to get device list");
            return Array.Empty<MeterRecord>();
        }
    }

    private async Task<MeterRecord?> ReadMeterAsync(DateTimeOffset now, string deviceId, string deviceName)
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

            return new MeterRecord(now, deviceId, deviceName, temperature, humidity);
        }
        catch (Exception ex)
        {
            // One failing meter (HTTP error, timeout, malformed JSON) should not hide the others.
            // SwitchBotClient takes no cancellation token, so a cancellation here is an HTTP timeout.
            _logger.LogWarning(ex, "Skipped {DeviceName}: status request failed", deviceName);
            return null;
        }
    }
}
