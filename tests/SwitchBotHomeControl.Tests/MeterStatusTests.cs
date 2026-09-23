using System.Text.Json;
using SwitchBotHomeControl.Api.Models;
using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tests;

public class MeterStatusTests
{
    // Deserialize the same way SwitchBotClient does, so values arrive as JsonElement
    private static Dictionary<string, object> Body(string json)
    {
        var response = JsonSerializer.Deserialize<DeviceStatusResponse>(
            $$"""{"statusCode":100,"message":"success","body":{{json}}}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return response!.Body;
    }

    [Theory]
    [InlineData("Meter", true)]
    [InlineData("MeterPlus", true)]
    [InlineData("MeterPro", true)]
    [InlineData("MeterPro(CO2)", true)]
    [InlineData("WoIOSensor", true)]
    [InlineData("Hub Mini", false)]
    [InlineData("Color Bulb", false)]
    [InlineData("", false)]
    public void IsThermoHygrometer_MatchesMeterTypes(string deviceType, bool expected)
    {
        Assert.Equal(expected, MeterStatus.IsThermoHygrometer(deviceType));
    }

    [Fact]
    public void TryRead_ReturnsTemperatureAndHumidity()
    {
        var body = Body("""{"temperature":25.4,"battery":100,"humidity":45,"deviceType":"Meter"}""");

        Assert.True(MeterStatus.TryRead(body, out var temperature, out var humidity));
        Assert.Equal(25.4, temperature);
        Assert.Equal(45, humidity);
    }

    [Fact]
    public void TryRead_AcceptsMissingBattery()
    {
        var body = Body("""{"temperature":18,"humidity":60}""");

        Assert.True(MeterStatus.TryRead(body, out var temperature, out var humidity));
        Assert.Equal(18, temperature);
        Assert.Equal(60, humidity);
    }

    [Fact]
    public void TryRead_AcceptsZeroTemperatureWithValidHumidity()
    {
        var body = Body("""{"temperature":0,"battery":80,"humidity":40}""");

        Assert.True(MeterStatus.TryRead(body, out var temperature, out _));
        Assert.Equal(0, temperature);
    }

    // Observed on a meter with a dead battery: the cloud keeps returning zeros
    [Fact]
    public void TryRead_RejectsDeadBattery()
    {
        var body = Body("""{"temperature":0,"battery":0,"humidity":0,"deviceType":"Meter"}""");

        Assert.False(MeterStatus.TryRead(body, out _, out _));
    }

    [Fact]
    public void TryRead_RejectsAllZeroReadingWithoutBattery()
    {
        var body = Body("""{"temperature":0,"humidity":0}""");

        Assert.False(MeterStatus.TryRead(body, out _, out _));
    }

    [Theory]
    [InlineData("""{"humidity":45}""")]
    [InlineData("""{"temperature":25.4}""")]
    [InlineData("""{"temperature":"25.4","humidity":45}""")]
    [InlineData("""{"temperature":null,"humidity":45}""")]
    public void TryRead_RejectsMissingOrNonNumericValues(string json)
    {
        Assert.False(MeterStatus.TryRead(Body(json), out _, out _));
    }
}
