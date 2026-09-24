using System.Text.Json;

namespace SwitchBotHomeControl.Monitoring;

public static class MeterStatus
{
    // deviceType values of SwitchBot thermo-hygrometers (WoIOSensor is the Outdoor Meter)
    private static readonly HashSet<string> ThermoHygrometerTypes = new(StringComparer.Ordinal)
    {
        "Meter",
        "MeterPlus",
        "MeterPro",
        "MeterPro(CO2)",
        "WoIOSensor"
    };

    public static bool IsThermoHygrometer(string deviceType) => ThermoHygrometerTypes.Contains(deviceType);

    /// <summary>
    /// Reads temperature and humidity from a device status body.
    /// Returns false when the values are missing or when the meter reports no measurement:
    /// a meter with a dead battery keeps returning battery 0, temperature 0 and humidity 0.
    /// </summary>
    public static bool TryRead(Dictionary<string, object> body, out double temperature, out double humidity)
    {
        temperature = 0;
        humidity = 0;

        if (!TryGetNumber(body, "temperature", out var t) || !TryGetNumber(body, "humidity", out var h))
        {
            return false;
        }

        var hasDeadBattery = TryGetNumber(body, "battery", out var battery) && battery == 0;
        var isAllZero = t == 0 && h == 0;
        if (hasDeadBattery || isAllZero)
        {
            return false;
        }

        temperature = t;
        humidity = h;
        return true;
    }

    private static bool TryGetNumber(Dictionary<string, object> body, string key, out double value)
    {
        value = 0;
        return body.TryGetValue(key, out var raw)
            && raw is JsonElement { ValueKind: JsonValueKind.Number } element
            && element.TryGetDouble(out value);
    }
}
