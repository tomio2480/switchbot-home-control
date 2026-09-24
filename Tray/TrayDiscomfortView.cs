using System.Globalization;
using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tray;

/// <summary>
/// Texts shown by the tray icon and its menu for the latest discomfort readings.
/// </summary>
public static class TrayDiscomfortView
{
    public const string AppName = "SwitchBot Home Control";

    /// <summary>NotifyIcon.Text throws when longer than 127 characters.</summary>
    public const int MaxTooltipLength = 127;

    /// <summary>
    /// The meter that colors the tray icon: the one named <paramref name="preferredName"/>, otherwise the first one.
    /// </summary>
    public static MeterRecord? SelectIconMeter(IReadOnlyList<MeterRecord> records, string? preferredName)
    {
        return records.FirstOrDefault(r => r.DeviceName == preferredName) ?? records.FirstOrDefault();
    }

    public static string FormatHeader(MeterSnapshot? snapshot)
    {
        return snapshot == null
            ? "🌡️ 不快指数: 取得待ち"
            : $"🌡️ 不快指数（{snapshot.Time.ToString("HH:mm", CultureInfo.InvariantCulture)} 時点）";
    }

    public static string FormatMenuLine(MeterRecord record)
    {
        var inv = CultureInfo.InvariantCulture;
        return $"{record.DeviceName}: {record.Index.ToString("F1", inv)}「{record.Level.ToLabel()}」"
            + $"（{record.Temperature.ToString("F1", inv)}℃ / {record.Humidity.ToString("0.#", inv)}%）";
    }

    public static string FormatTooltip(MeterRecord? record)
    {
        if (record == null)
        {
            return AppName;
        }

        var text = $"{AppName}\n{record.DeviceName} {record.Index.ToString("F1", CultureInfo.InvariantCulture)}「{record.Level.ToLabel()}」";
        return text.Length <= MaxTooltipLength ? text : text[..(MaxTooltipLength - 1)] + "…";
    }
}
