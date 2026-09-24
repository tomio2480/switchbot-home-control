using System.Globalization;
using System.Text.Json;

namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// Keeps meter readings as JSON Lines, one file per day (yyyy-MM-dd.jsonl).
/// The SwitchBot API has no history endpoint, so this is the only source of past readings.
/// </summary>
public class MeterHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _directory;
    private readonly object _gate = new();

    public MeterHistoryStore(string directory)
    {
        _directory = directory;
    }

    public void Append(IEnumerable<MeterRecord> records)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(_directory);
            foreach (var day in records.GroupBy(r => r.Time.Date))
            {
                File.AppendAllLines(FilePath(day.Key), day.Select(r => JsonSerializer.Serialize(r, JsonOptions)));
            }
        }
    }

    /// <summary>
    /// Records whose time is within [from, to], oldest first.
    /// </summary>
    public IReadOnlyList<MeterRecord> Read(DateTimeOffset from, DateTimeOffset to)
    {
        var records = new List<MeterRecord>();
        lock (_gate)
        {
            // Files are keyed by each record's local date; widen by a day so any UTC offset is covered
            for (var day = from.UtcDateTime.Date.AddDays(-1); day <= to.UtcDateTime.Date.AddDays(1); day = day.AddDays(1))
            {
                var path = FilePath(day);
                if (!File.Exists(path))
                {
                    continue;
                }

                records.AddRange(File.ReadLines(path)
                    .Select(TryParse)
                    .OfType<MeterRecord>()
                    .Where(r => r.Time >= from && r.Time <= to));
            }
        }

        return records.OrderBy(r => r.Time).ToList();
    }

    private string FilePath(DateTime day) =>
        Path.Combine(_directory, day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl");

    private static MeterRecord? TryParse(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<MeterRecord>(line, JsonOptions);
        }
        catch (JsonException)
        {
            // A line cut off by a crash or power loss must not hide the rest of the history
            return null;
        }
    }
}
