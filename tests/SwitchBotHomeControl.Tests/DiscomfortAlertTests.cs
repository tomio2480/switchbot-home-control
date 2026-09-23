using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tests;

public class DiscomfortAlertTests
{
    [Fact]
    public void FromReadings_KeepsOnlyUncomfortableMeters()
    {
        var readings = new[]
        {
            new MeterReading("温湿度計 デスク", 25, 50),   // 71.8 暑くない
            new MeterReading("温湿度計 リビング", 20, 50), // 65.3 快い
            new MeterReading("温湿度計 寝室", 10, 50),     // 52.2 寒い
        };

        var alerts = DiscomfortAlert.FromReadings(readings);

        Assert.Equal(
            new[]
            {
                new DiscomfortAlert("温湿度計 デスク", 25, 50, 71.8, DiscomfortLevel.NotHot),
                new DiscomfortAlert("温湿度計 寝室", 10, 50, 52.2, DiscomfortLevel.Cold),
            },
            alerts);
    }

    [Fact]
    public void FromReadings_ReturnsEmptyWhenAllComfortable()
    {
        var readings = new[] { new MeterReading("温湿度計 デスク", 20, 50) };

        Assert.Empty(DiscomfortAlert.FromReadings(readings));
    }

    [Fact]
    public void FormatMessage_ListsEveryAlertWithDeviceName()
    {
        var alerts = new[]
        {
            new DiscomfortAlert("温湿度計 デスク", 28, 75, 79.0, DiscomfortLevel.SlightlyHot),
            new DiscomfortAlert("温湿度計 寝室", 10, 50, 52.2, DiscomfortLevel.Cold),
        };

        var message = DiscomfortAlert.FormatMessage(alerts, new DateTime(2026, 9, 23, 14, 30, 0));

        Assert.Equal(
            "**不快指数のお知らせ**（2026/09/23 14:30）\n"
            + "- **温湿度計 デスク**: 不快指数 79.0「やや暑い」（気温 28.0℃ / 湿度 75%）\n"
            + "- **温湿度計 寝室**: 不快指数 52.2「寒い」（気温 10.0℃ / 湿度 50%）",
            message);
    }
}
