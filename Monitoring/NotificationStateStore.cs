using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// Remembers the last notified sensation level of each meter across restarts,
/// so that a restart does not repeat a notification.
/// </summary>
public class NotificationStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly object _gate = new();
    private readonly Dictionary<string, DiscomfortLevel> _lastNotified;

    public NotificationStateStore(string filePath)
    {
        _filePath = filePath;
        _lastNotified = Load(filePath);
    }

    public DiscomfortLevel? GetLastNotified(string deviceId)
    {
        lock (_gate)
        {
            return _lastNotified.TryGetValue(deviceId, out var level) ? level : null;
        }
    }

    public void SetLastNotified(IEnumerable<(string DeviceId, DiscomfortLevel Level)> levels)
    {
        lock (_gate)
        {
            foreach (var (deviceId, level) in levels)
            {
                _lastNotified[deviceId] = level;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_lastNotified, JsonOptions));
        }
    }

    private static Dictionary<string, DiscomfortLevel> Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, DiscomfortLevel>>(File.ReadAllText(filePath), JsonOptions) ?? new();
        }
        catch (JsonException)
        {
            // Losing the state only risks one duplicate notification; do not block startup
            return new();
        }
    }
}
