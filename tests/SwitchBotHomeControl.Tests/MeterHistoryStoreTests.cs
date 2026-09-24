using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tests;

public sealed class MeterHistoryStoreTests : IDisposable
{
    private static readonly TimeSpan Jst = TimeSpan.FromHours(9);
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "meter-history-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Read_ReturnsAppendedRecordsWithinRangeInTimeOrder()
    {
        var store = new MeterHistoryStore(_directory);
        var early = new MeterRecord(new DateTimeOffset(2026, 9, 23, 23, 55, 0, Jst), "desk", "温湿度計 デスク", 25, 50);
        var late = new MeterRecord(new DateTimeOffset(2026, 9, 24, 0, 5, 0, Jst), "desk", "温湿度計 デスク", 24.5, 55);
        var outside = new MeterRecord(new DateTimeOffset(2026, 9, 24, 1, 0, 0, Jst), "desk", "温湿度計 デスク", 24, 60);

        store.Append(new[] { late, outside });
        store.Append(new[] { early });

        var records = store.Read(new DateTimeOffset(2026, 9, 23, 23, 0, 0, Jst), new DateTimeOffset(2026, 9, 24, 0, 30, 0, Jst));

        Assert.Equal(new[] { early, late }, records);
    }

    [Fact]
    public void Append_KeepsDeviceNamesWithSymbolsIntact()
    {
        var store = new MeterHistoryStore(_directory);
        var record = new MeterRecord(new DateTimeOffset(2026, 9, 24, 10, 0, 0, Jst), "id", "寝室, \"北\"\n側", 20, 50);

        store.Append(new[] { record });

        Assert.Equal(new[] { record }, store.Read(record.Time.AddMinutes(-1), record.Time.AddMinutes(1)));
    }

    [Fact]
    public void Read_SkipsMalformedLines()
    {
        var store = new MeterHistoryStore(_directory);
        var record = new MeterRecord(new DateTimeOffset(2026, 9, 24, 10, 0, 0, Jst), "desk", "温湿度計 デスク", 20, 50);
        store.Append(new[] { record });
        // 書き込み途中で落ちた行を模す
        File.AppendAllText(Directory.GetFiles(_directory).Single(), "{\"time\":\"2026-09-24T10:1\n");

        Assert.Equal(new[] { record }, store.Read(record.Time.AddHours(-1), record.Time.AddHours(1)));
    }

    [Fact]
    public void Read_ReturnsEmptyWhenNothingRecorded()
    {
        var store = new MeterHistoryStore(_directory);

        Assert.Empty(store.Read(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now));
    }
}
