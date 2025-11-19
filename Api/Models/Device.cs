namespace SwitchBotHomeControl.Api.Models;

public class Device
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool EnableCloudService { get; set; }
    public string HubDeviceId { get; set; } = string.Empty;
}

public class InfraredRemoteDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string RemoteType { get; set; } = string.Empty;
    public string HubDeviceId { get; set; } = string.Empty;
}

public class DeviceListResponseBody
{
    public List<Device> DeviceList { get; set; } = new();
    public List<InfraredRemoteDevice> InfraredRemoteList { get; set; } = new();
}

public class DeviceListResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public DeviceListResponseBody Body { get; set; } = new();
}

public class DeviceStatusResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Body { get; set; } = new();
}

public class DeviceCommandResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DeviceCommand
{
    public string Command { get; set; } = string.Empty;
    public string? Parameter { get; set; }
    public string? CommandType { get; set; }
}
