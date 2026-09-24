using System.Globalization;

namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// A sensation level change to notify: <paramref name="From"/> is the level the user last knew about.
/// </summary>
public record DiscomfortChange(MeterRecord Current, DiscomfortLevel From)
{
    public static string FormatMessage(IReadOnlyList<DiscomfortChange> changes, DateTime checkedAt)
    {
        var inv = CultureInfo.InvariantCulture;
        var lines = new List<string>
        {
            $"**不快指数のお知らせ**（{checkedAt.ToString("yyyy/MM/dd HH:mm", inv)}）"
        };
        lines.AddRange(changes.Select(c =>
            $"- **{c.Current.DeviceName}**: 「{c.From.ToLabel()}」→「{c.Current.Level.ToLabel()}」"
            + $"（不快指数 {c.Current.Index.ToString("F1", inv)}"
            + $" / 気温 {c.Current.Temperature.ToString("F1", inv)}℃"
            + $" / 湿度 {c.Current.Humidity.ToString("0.#", inv)}%）"));
        return string.Join("\n", lines);
    }
}
