namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// Sensation bands of the discomfort index.
/// Ranges follow https://ac.fj-tec.co.jp/お役立ち情報/不快指数を知って快適な空間を/
/// </summary>
public enum DiscomfortLevel
{
    Cold,          // < 55
    Chilly,        // 55 - 60
    Neutral,       // 60 - 65
    Pleasant,      // 65 - 70
    NotHot,        // 70 - 75
    SlightlyHot,   // 75 - 80
    HotAndSweaty,  // 80 - 85
    UnbearablyHot  // >= 85
}

public static class DiscomfortIndex
{
    /// <summary>
    /// DI = 0.81T + 0.01H × (0.99T − 14.3) + 46.3, rounded to one decimal like the reference table.
    /// Computed in decimal so that values such as 65.25 round the same way as the table.
    /// </summary>
    public static double Calculate(double temperatureCelsius, double relativeHumidityPercent)
    {
        var t = (decimal)temperatureCelsius;
        var h = (decimal)relativeHumidityPercent;
        var index = 0.81m * t + 0.01m * h * (0.99m * t - 14.3m) + 46.3m;
        return (double)Math.Round(index, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Each band includes its lower bound (e.g. 65.0 is "Pleasant", 70.0 is "NotHot").
    /// </summary>
    public static DiscomfortLevel Classify(double index) => index switch
    {
        < 55 => DiscomfortLevel.Cold,
        < 60 => DiscomfortLevel.Chilly,
        < 65 => DiscomfortLevel.Neutral,
        < 70 => DiscomfortLevel.Pleasant,
        < 75 => DiscomfortLevel.NotHot,
        < 80 => DiscomfortLevel.SlightlyHot,
        < 85 => DiscomfortLevel.HotAndSweaty,
        _ => DiscomfortLevel.UnbearablyHot
    };

    public static string ToLabel(this DiscomfortLevel level) => level switch
    {
        DiscomfortLevel.Cold => "寒い",
        DiscomfortLevel.Chilly => "肌寒い",
        DiscomfortLevel.Neutral => "何も感じない",
        DiscomfortLevel.Pleasant => "快い",
        DiscomfortLevel.NotHot => "暑くない",
        DiscomfortLevel.SlightlyHot => "やや暑い",
        DiscomfortLevel.HotAndSweaty => "暑くて汗が出る",
        DiscomfortLevel.UnbearablyHot => "暑くてたまらない",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown discomfort level")
    };

    /// <summary>
    /// The inclusive lower bound used by <see cref="Classify"/>; null for the lowest level.
    /// </summary>
    public static double? LowerBound(this DiscomfortLevel level) => level switch
    {
        DiscomfortLevel.Cold => null,
        DiscomfortLevel.Chilly => 55,
        DiscomfortLevel.Neutral => 60,
        DiscomfortLevel.Pleasant => 65,
        DiscomfortLevel.NotHot => 70,
        DiscomfortLevel.SlightlyHot => 75,
        DiscomfortLevel.HotAndSweaty => 80,
        DiscomfortLevel.UnbearablyHot => 85,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown discomfort level")
    };

    /// <summary>
    /// Text colors of the sensation labels in the reference article, from blue (cold) to red (hot).
    /// </summary>
    public static string ToColorHex(this DiscomfortLevel level) => level switch
    {
        DiscomfortLevel.Cold => "#0673B6",
        DiscomfortLevel.Chilly => "#298B97",
        DiscomfortLevel.Neutral => "#5CA869",
        DiscomfortLevel.Pleasant => "#8BC43F",
        DiscomfortLevel.NotHot => "#9EA53B",
        DiscomfortLevel.SlightlyHot => "#B37F34",
        DiscomfortLevel.HotAndSweaty => "#CA562D",
        DiscomfortLevel.UnbearablyHot => "#EA1F25",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown discomfort level")
    };
}
