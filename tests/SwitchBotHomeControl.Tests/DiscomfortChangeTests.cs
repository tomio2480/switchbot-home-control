using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tests;

public class DiscomfortChangeTests
{
    [Fact]
    public void FormatMessage_ListsEveryChangeWithDeviceNameAndLevels()
    {
        var at = new DateTimeOffset(2026, 9, 24, 10, 10, 0, TimeSpan.FromHours(9));
        var changes = new[]
        {
            new DiscomfortChange(new MeterRecord(at, "desk", "温湿度計 デスク", 23.2, 50), DiscomfortLevel.NotHot),
            new DiscomfortChange(new MeterRecord(at, "bedroom", "温湿度計 寝室", 28, 75), DiscomfortLevel.Pleasant),
        };

        var message = DiscomfortChange.FormatMessage(changes, new DateTime(2026, 9, 24, 10, 10, 0));

        Assert.Equal(
            "**不快指数のお知らせ**（2026/09/24 10:10）\n"
            + "- **温湿度計 デスク**: 「暑くない」→「快い」（不快指数 69.4 / 気温 23.2℃ / 湿度 50%）\n"
            + "- **温湿度計 寝室**: 「快い」→「やや暑い」（不快指数 79.0 / 気温 28.0℃ / 湿度 75%）",
            message);
    }
}
