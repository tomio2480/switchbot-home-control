using System.Text.Json.Serialization;

namespace SwitchBotHomeControl.Monitoring;

/// <summary>
/// One temperature/humidity measurement of a thermo-hygrometer.
/// Only the raw values are stored; the discomfort index is derived on demand.
/// </summary>
public record MeterRecord(DateTimeOffset Time, string DeviceId, string DeviceName, double Temperature, double Humidity)
{
    [JsonIgnore]
    public double Index => DiscomfortIndex.Calculate(Temperature, Humidity);

    [JsonIgnore]
    public DiscomfortLevel Level => DiscomfortIndex.Classify(Index);
}

/// <summary>
/// The readings of every meter taken in one monitoring cycle. Empty when the cycle failed.
/// </summary>
public record MeterSnapshot(DateTimeOffset Time, IReadOnlyList<MeterRecord> Records);
