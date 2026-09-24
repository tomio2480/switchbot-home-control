using SwitchBotHomeControl.Monitoring;
using static SwitchBotHomeControl.Monitoring.DiscomfortLevel;

namespace SwitchBotHomeControl.Tests;

public class DiscomfortChangeDetectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 10, 0, 0, TimeSpan.FromHours(9));

    // ShouldNotify(current, ago10, ago20, ago30, lastNotified)

    [Fact]
    public void ShouldNotify_WhenLevelChangedAndHeldFor20Minutes()
    {
        Assert.True(DiscomfortChangeDetector.ShouldNotify(Pleasant, Pleasant, Pleasant, NotHot, lastNotified: null));
    }

    [Fact]
    public void ShouldNotNotify_WhenLevelIsStillOscillating()
    {
        // 30 分前と違っても，20 分前か 10 分前が現在と違えば境界付近の振動とみなす
        Assert.False(DiscomfortChangeDetector.ShouldNotify(Pleasant, NotHot, Pleasant, NotHot, lastNotified: null));
        Assert.False(DiscomfortChangeDetector.ShouldNotify(Pleasant, Pleasant, NotHot, NotHot, lastNotified: null));
    }

    [Fact]
    public void ShouldNotNotify_WhenLevelIsUnchanged()
    {
        Assert.False(DiscomfortChangeDetector.ShouldNotify(Pleasant, Pleasant, Pleasant, Pleasant, lastNotified: null));
    }

    [Theory]
    [InlineData(SlightlyHot, null, null, Pleasant)]      // 30 分前から 2 段階
    [InlineData(SlightlyHot, null, Pleasant, null)]      // 20 分前から 2 段階
    [InlineData(SlightlyHot, Pleasant, null, null)]      // 10 分前から 2 段階
    [InlineData(Cold, Pleasant, Pleasant, Pleasant)]     // 3 段階以上も含む
    public void ShouldNotify_WhenLevelJumpedTwoOrMoreSteps(
        DiscomfortLevel current, DiscomfortLevel? ago10, DiscomfortLevel? ago20, DiscomfortLevel? ago30)
    {
        Assert.True(DiscomfortChangeDetector.ShouldNotify(current, ago10, ago20, ago30, lastNotified: null));
    }

    [Fact]
    public void ShouldNotNotify_WhenCurrentLevelWasAlreadyNotified()
    {
        // 2 段階の変化を通知した後，同じ区分のまま 30 分間は 2 段階差が残るため繰り返さない
        Assert.False(DiscomfortChangeDetector.ShouldNotify(SlightlyHot, SlightlyHot, SlightlyHot, Pleasant, lastNotified: SlightlyHot));
        Assert.False(DiscomfortChangeDetector.ShouldNotify(Pleasant, Pleasant, Pleasant, NotHot, lastNotified: Pleasant));
    }

    [Fact]
    public void ShouldNotify_WhenStableLevelDiffersFromLastNotified()
    {
        // 再起動や投稿失敗で変化の瞬間を逃しても，30 分間落ち着いた区分が前回の通知と違えば知らせる
        Assert.True(DiscomfortChangeDetector.ShouldNotify(Pleasant, Pleasant, Pleasant, Pleasant, lastNotified: NotHot));
    }

    [Fact]
    public void ShouldNotNotify_WhenHistoryIsMissing()
    {
        Assert.False(DiscomfortChangeDetector.ShouldNotify(Pleasant, null, null, null, lastNotified: NotHot));
        Assert.False(DiscomfortChangeDetector.ShouldNotify(Pleasant, Pleasant, Pleasant, null, lastNotified: NotHot));
    }

    [Fact]
    public void LevelAt_PicksNearestRecordWithinHalfInterval()
    {
        var history = new[]
        {
            Record(Now.AddMinutes(-16), 25, 50), // 71.8 暑くない（目標から 6 分ずれ）
            Record(Now.AddMinutes(-9), 20, 50),  // 65.3 快い（目標から 1 分ずれ）
        };

        Assert.Equal(Pleasant, DiscomfortChangeDetector.LevelAt(history, Now.AddMinutes(-10)));
    }

    [Fact]
    public void LevelAt_ReturnsNullWhenNoRecordIsCloseEnough()
    {
        var history = new[] { Record(Now.AddMinutes(-16), 20, 50) };

        Assert.Null(DiscomfortChangeDetector.LevelAt(history, Now.AddMinutes(-10)));
    }

    [Fact]
    public void DetectAll_ReportsOnlyChangedDevicesPerDevice()
    {
        var current = new[]
        {
            Record(Now, 20, 50, "desk"),     // 快い
            Record(Now, 20, 50, "bedroom"),  // 快い
        };
        var history = new[]
        {
            Record(Now.AddMinutes(-30), 25, 50, "desk"),    // 暑くない
            Record(Now.AddMinutes(-20), 20, 50, "desk"),
            Record(Now.AddMinutes(-10), 20, 50, "desk"),
            Record(Now.AddMinutes(-30), 20, 50, "bedroom"), // ずっと快い
            Record(Now.AddMinutes(-20), 20, 50, "bedroom"),
            Record(Now.AddMinutes(-10), 20, 50, "bedroom"),
        };

        var changes = DiscomfortChangeDetector.DetectAll(current, history, _ => null);

        var change = Assert.Single(changes);
        Assert.Equal("desk", change.Current.DeviceId);
        Assert.Equal(NotHot, change.From);
    }

    [Fact]
    public void DetectAll_UsesLastNotifiedLevelAsFrom()
    {
        var current = new[] { Record(Now, 20, 50, "desk") };
        var history = new[]
        {
            Record(Now.AddMinutes(-30), 20, 50, "desk"),
            Record(Now.AddMinutes(-20), 20, 50, "desk"),
            Record(Now.AddMinutes(-10), 20, 50, "desk"),
        };

        var change = Assert.Single(DiscomfortChangeDetector.DetectAll(current, history, _ => SlightlyHot));

        Assert.Equal(SlightlyHot, change.From);
    }

    private static MeterRecord Record(DateTimeOffset time, double temperature, double humidity, string deviceId = "desk")
        => new(time, deviceId, $"温湿度計 {deviceId}", temperature, humidity);
}
