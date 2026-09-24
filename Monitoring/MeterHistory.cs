namespace SwitchBotHomeControl.Monitoring;

public record MeterPoint(DateTimeOffset Time, double Temperature, double Humidity, double Index);

public record MeterSeries(string DeviceId, string DeviceName, IReadOnlyList<MeterPoint> Points);

public static class MeterHistory
{
    /// <summary>
    /// Groups time-ordered records into one series per meter, in the order the meters first appear.
    /// A renamed meter keeps one series under its latest name.
    /// </summary>
    public static IReadOnlyList<MeterSeries> BuildSeries(IEnumerable<MeterRecord> records)
    {
        return records
            .GroupBy(r => r.DeviceId)
            .Select(g => new MeterSeries(
                g.Key,
                g.Last().DeviceName,
                g.Select(r => new MeterPoint(r.Time, r.Temperature, r.Humidity, r.Index)).ToList()))
            .ToList();
    }
}
