namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// Decides when a change of the sensation level is worth a notification,
/// ignoring oscillation around a band boundary.
/// </summary>
public static class DiscomfortChangeDetector
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    /// <summary>How far back <see cref="DetectAll"/> needs history (30 minutes plus tolerance).</summary>
    public static readonly TimeSpan LookBack = Interval * 3 + Interval / 2;

    private const int JumpSteps = 2;

    public static IReadOnlyList<DiscomfortChange> DetectAll(
        IEnumerable<MeterRecord> current,
        IEnumerable<MeterRecord> history,
        Func<string, DiscomfortLevel?> lastNotified)
    {
        var historyByDevice = history.ToLookup(r => r.DeviceId);
        return current
            .Select(r => Detect(r, historyByDevice[r.DeviceId], lastNotified(r.DeviceId)))
            .OfType<DiscomfortChange>()
            .ToList();
    }

    private static DiscomfortChange? Detect(MeterRecord current, IEnumerable<MeterRecord> history, DiscomfortLevel? lastNotified)
    {
        var records = history.ToList();
        var ago10 = LevelAt(records, current.Time - Interval);
        var ago20 = LevelAt(records, current.Time - Interval * 2);
        var ago30 = LevelAt(records, current.Time - Interval * 3);

        if (!ShouldNotify(current.Level, ago10, ago20, ago30, lastNotified))
        {
            return null;
        }

        // ShouldNotify is true only when at least one past level is known
        var from = lastNotified ?? ago30 ?? ago20 ?? ago10!.Value;
        return new DiscomfortChange(current, from);
    }

    /// <summary>
    /// Notify when either
    /// (a) the level differs from 30 minutes ago and has stayed the same for the last 20 minutes, or
    /// (b) the level is two or more steps away from any level in the last 30 minutes.
    /// A level that was already notified is not repeated.
    /// (a) also fires when the level has been stable for 30 minutes but differs from the last notification,
    /// so that a change missed by a restart or a failed post is still reported.
    /// </summary>
    public static bool ShouldNotify(
        DiscomfortLevel current,
        DiscomfortLevel? ago10,
        DiscomfortLevel? ago20,
        DiscomfortLevel? ago30,
        DiscomfortLevel? lastNotified)
    {
        if (current == lastNotified)
        {
            return false;
        }

        var settled = ago30 is { } before
            && ago20 == current
            && ago10 == current
            && (before != current || lastNotified is not null);
        var jumped = new[] { ago10, ago20, ago30 }
            .Any(level => level is { } past && Math.Abs(past - current) >= JumpSteps);
        return settled || jumped;
    }

    /// <summary>
    /// The level of the record closest to <paramref name="target"/>, within half an interval.
    /// </summary>
    public static DiscomfortLevel? LevelAt(IEnumerable<MeterRecord> history, DateTimeOffset target)
    {
        var tolerance = Interval / 2;
        return history
            .Select(r => (Record: r, Distance: (r.Time - target).Duration()))
            .Where(x => x.Distance <= tolerance)
            .OrderBy(x => x.Distance)
            .Select(x => (DiscomfortLevel?)x.Record.Level)
            .FirstOrDefault();
    }
}
