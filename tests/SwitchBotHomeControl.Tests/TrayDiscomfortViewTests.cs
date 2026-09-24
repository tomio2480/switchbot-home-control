using SwitchBotHomeControl.Monitoring;
using SwitchBotHomeControl.Tray;

namespace SwitchBotHomeControl.Tests;

public class TrayDiscomfortViewTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 24, 10, 5, 0, TimeSpan.FromHours(9));
    private static readonly MeterRecord Desk = new(At, "desk", "温湿度計 デスク", 23.2, 50);
    private static readonly MeterRecord Bedroom = new(At, "bedroom", "温湿度計 寝室", 23, 48);

    [Fact]
    public void SelectIconMeter_PrefersConfiguredName()
    {
        Assert.Equal(Bedroom, TrayDiscomfortView.SelectIconMeter(new[] { Desk, Bedroom }, "温湿度計 寝室"));
    }

    [Fact]
    public void SelectIconMeter_FallsBackToFirstMeter()
    {
        Assert.Equal(Desk, TrayDiscomfortView.SelectIconMeter(new[] { Desk, Bedroom }, null));
        Assert.Equal(Desk, TrayDiscomfortView.SelectIconMeter(new[] { Desk, Bedroom }, "存在しない温湿度計"));
    }

    [Fact]
    public void SelectIconMeter_ReturnsNullWithoutReadings()
    {
        Assert.Null(TrayDiscomfortView.SelectIconMeter(Array.Empty<MeterRecord>(), "温湿度計 デスク"));
    }

    [Fact]
    public void FormatMenuLine_ShowsIndexLevelAndReadings()
    {
        Assert.Equal("温湿度計 デスク: 69.4「快い」（23.2℃ / 50%）", TrayDiscomfortView.FormatMenuLine(Desk));
    }

    [Fact]
    public void FormatHeader_ShowsMeasuredTime()
    {
        Assert.Equal("🌡️ 不快指数（10:05 時点）", TrayDiscomfortView.FormatHeader(new MeterSnapshot(At, new[] { Desk })));
        Assert.Equal("🌡️ 不快指数: 取得待ち", TrayDiscomfortView.FormatHeader(null));
    }

    [Fact]
    public void FormatTooltip_FitsNotifyIconLimit()
    {
        Assert.Equal("SwitchBot Home Control\n温湿度計 デスク 69.4「快い」", TrayDiscomfortView.FormatTooltip(Desk));
        Assert.Equal("SwitchBot Home Control", TrayDiscomfortView.FormatTooltip(null));

        var longName = Desk with { DeviceName = new string('長', 200) };
        Assert.True(TrayDiscomfortView.FormatTooltip(longName).Length <= TrayDiscomfortView.MaxTooltipLength);
    }
}
