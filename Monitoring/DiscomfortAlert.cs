using System.Globalization;

namespace SwitchBotHomeControl.Monitoring;

public record DiscomfortAlert(
    string DeviceName,
    double Temperature,
    double Humidity,
    double Index,
    DiscomfortLevel Level)
{
    /// <summary>
    /// Keeps only the meters whose sensation is neither "何も感じない" nor "快い".
    /// </summary>
    public static IReadOnlyList<DiscomfortAlert> FromReadings(IEnumerable<MeterReading> readings)
    {
        return readings
            .Select(r =>
            {
                var index = DiscomfortIndex.Calculate(r.Temperature, r.Humidity);
                return new DiscomfortAlert(r.DeviceName, r.Temperature, r.Humidity, index, DiscomfortIndex.Classify(index));
            })
            .Where(a => !a.Level.IsComfortable())
            .ToList();
    }

    public static string FormatMessage(IReadOnlyList<DiscomfortAlert> alerts, DateTime checkedAt)
    {
        var inv = CultureInfo.InvariantCulture;
        var lines = new List<string>
        {
            $"**不快指数のお知らせ**（{checkedAt.ToString("yyyy/MM/dd HH:mm", inv)}）"
        };
        lines.AddRange(alerts.Select(a =>
            $"- **{a.DeviceName}**: 不快指数 {a.Index.ToString("F1", inv)}「{a.Level.ToLabel()}」"
            + $"（気温 {a.Temperature.ToString("F1", inv)}℃ / 湿度 {a.Humidity.ToString("0.#", inv)}%）"));
        return string.Join("\n", lines);
    }
}
