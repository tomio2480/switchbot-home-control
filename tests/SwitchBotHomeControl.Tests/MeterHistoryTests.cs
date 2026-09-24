using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tests;

public class MeterHistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 10, 0, 0, TimeSpan.FromHours(9));

    [Fact]
    public void BuildSeries_GroupsByDeviceInFirstSeenOrderAndComputesIndex()
    {
        var records = new[]
        {
            new MeterRecord(Now.AddMinutes(-10), "desk", "温湿度計 デスク", 25, 50),
            new MeterRecord(Now.AddMinutes(-10), "bedroom", "温湿度計 寝室", 20, 50),
            new MeterRecord(Now, "desk", "温湿度計 デスク（改名）", 28, 75),
        };

        var series = MeterHistory.BuildSeries(records);

        Assert.Collection(
            series,
            desk =>
            {
                Assert.Equal("desk", desk.DeviceId);
                Assert.Equal("温湿度計 デスク（改名）", desk.DeviceName); // 最新の名前を使う
                Assert.Equal(
                    new[] { new MeterPoint(Now.AddMinutes(-10), 25, 50, 71.8), new MeterPoint(Now, 28, 75, 79.0) },
                    desk.Points);
            },
            bedroom =>
            {
                Assert.Equal("bedroom", bedroom.DeviceId);
                Assert.Equal(new[] { new MeterPoint(Now.AddMinutes(-10), 20, 50, 65.3) }, bedroom.Points);
            });
    }
}
