using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tests;

public sealed class NotificationStateStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "notification-state-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_directory, "notification-state.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void LastNotified_SurvivesReload()
    {
        new NotificationStateStore(FilePath).SetLastNotified(new[] { ("desk", DiscomfortLevel.Pleasant) });

        var reloaded = new NotificationStateStore(FilePath);

        Assert.Equal(DiscomfortLevel.Pleasant, reloaded.GetLastNotified("desk"));
        Assert.Null(reloaded.GetLastNotified("bedroom"));
    }

    [Fact]
    public void GetLastNotified_ReturnsNullWhenFileIsCorrupt()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");

        Assert.Null(new NotificationStateStore(FilePath).GetLastNotified("desk"));
    }
}
