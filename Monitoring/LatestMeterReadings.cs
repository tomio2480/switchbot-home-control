namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// Hands the readings of the latest monitoring cycle from the background service to the tray icon.
/// </summary>
public class LatestMeterReadings
{
    private MeterSnapshot? _current;

    public event Action<MeterSnapshot>? Updated;

    public MeterSnapshot? Current => Volatile.Read(ref _current);

    public void Update(MeterSnapshot snapshot)
    {
        Volatile.Write(ref _current, snapshot);
        Updated?.Invoke(snapshot);
    }
}
