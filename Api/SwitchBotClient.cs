using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SwitchBotHomeControl.Api.Models;

namespace SwitchBotHomeControl.Api;

public class SwitchBotClient
{
    private const string BaseUrl = "https://api.switch-bot.com";
    private readonly string _token;
    private readonly string _secret;
    private readonly HttpClient _httpClient;

    public SwitchBotClient(string token, string secret)
    {
        _token = token;
        _secret = secret;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>
    /// Generate authentication headers for SwitchBot API v1.1
    /// </summary>
    private Dictionary<string, string> GenerateHeaders()
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Guid.NewGuid().ToString();
        var data = $"{_token}{t}{nonce}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var signBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        var sign = Convert.ToBase64String(signBytes);

        return new Dictionary<string, string>
        {
            { "Authorization", _token },
            { "t", t.ToString() },
            { "sign", sign },
            { "nonce", nonce }
        };
    }

    /// <summary>
    /// Get list of all devices
    /// </summary>
    public async Task<DeviceListResponse> GetDevicesAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v1.1/devices");
        foreach (var header in GenerateHeaders())
        {
            request.Headers.Add(header.Key, header.Value);
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DeviceListResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize device list response");
    }

    /// <summary>
    /// Get device status
    /// </summary>
    public async Task<DeviceStatusResponse> GetDeviceStatusAsync(string deviceId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v1.1/devices/{deviceId}/status");
        foreach (var header in GenerateHeaders())
        {
            request.Headers.Add(header.Key, header.Value);
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DeviceStatusResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize device status response");
    }

    /// <summary>
    /// Send command to device
    /// </summary>
    public async Task<DeviceCommandResponse> SendCommandAsync(string deviceId, DeviceCommand command)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1.1/devices/{deviceId}/commands");
        foreach (var header in GenerateHeaders())
        {
            request.Headers.Add(header.Key, header.Value);
        }

        var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DeviceCommandResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize command response");
    }

    /// <summary>
    /// Turn on a device
    /// </summary>
    public Task<DeviceCommandResponse> TurnOnAsync(string deviceId)
    {
        return SendCommandAsync(deviceId, new DeviceCommand { Command = "turnOn" });
    }

    /// <summary>
    /// Turn off a device
    /// </summary>
    public Task<DeviceCommandResponse> TurnOffAsync(string deviceId)
    {
        return SendCommandAsync(deviceId, new DeviceCommand { Command = "turnOff" });
    }

    /// <summary>
    /// Toggle a device
    /// </summary>
    public Task<DeviceCommandResponse> ToggleAsync(string deviceId)
    {
        return SendCommandAsync(deviceId, new DeviceCommand { Command = "toggle" });
    }

    /// <summary>
    /// Set brightness (for supported devices like Color Bulb)
    /// </summary>
    public Task<DeviceCommandResponse> SetBrightnessAsync(string deviceId, int brightness)
    {
        if (brightness < 0 || brightness > 100)
        {
            throw new ArgumentException("Brightness must be between 0 and 100", nameof(brightness));
        }

        return SendCommandAsync(deviceId, new DeviceCommand
        {
            Command = "setBrightness",
            Parameter = brightness.ToString()
        });
    }

    /// <summary>
    /// Set temperature for air conditioner
    /// </summary>
    public Task<DeviceCommandResponse> SetTemperatureAsync(string deviceId, int temperature)
    {
        return SendCommandAsync(deviceId, new DeviceCommand
        {
            Command = "setAll",
            Parameter = $"{temperature},1,3,on",
            CommandType = "command"
        });
    }
}
