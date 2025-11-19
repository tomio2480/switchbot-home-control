using Microsoft.AspNetCore.Mvc;
using SwitchBotHomeControl.Api;
using SwitchBotHomeControl.Api.Models;

namespace SwitchBotHomeControl.WebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly SwitchBotClient _client;

    public DevicesController(SwitchBotClient client)
    {
        _client = client;
    }

    [HttpGet]
    public async Task<IActionResult> GetDevices()
    {
        try
        {
            var response = await _client.GetDevicesAsync();
            if (response.StatusCode == 100)
            {
                return Ok(new
                {
                    success = true,
                    devices = response.Body.DeviceList,
                    infraredDevices = response.Body.InfraredRemoteList
                });
            }

            return StatusCode(500, new
            {
                success = false,
                error = response.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetDevicesWithStatus()
    {
        try
        {
            var devicesResponse = await _client.GetDevicesAsync();
            if (devicesResponse.StatusCode != 100)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = devicesResponse.Message
                });
            }

            var devicesWithStatus = await Task.WhenAll(
                devicesResponse.Body.DeviceList.Select(async device =>
                {
                    try
                    {
                        var statusResponse = await _client.GetDeviceStatusAsync(device.DeviceId);
                        return new
                        {
                            device.DeviceId,
                            device.DeviceName,
                            device.DeviceType,
                            device.EnableCloudService,
                            device.HubDeviceId,
                            status = statusResponse.StatusCode == 100 ? statusResponse.Body : null,
                            statusError = statusResponse.StatusCode != 100 ? statusResponse.Message : null
                        };
                    }
                    catch (Exception ex)
                    {
                        return new
                        {
                            device.DeviceId,
                            device.DeviceName,
                            device.DeviceType,
                            device.EnableCloudService,
                            device.HubDeviceId,
                            status = (Dictionary<string, object>?)null,
                            statusError = (string?)ex.Message
                        };
                    }
                })
            );

            return Ok(new
            {
                success = true,
                devices = devicesWithStatus,
                infraredDevices = devicesResponse.Body.InfraredRemoteList
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("{deviceId}/status")]
    public async Task<IActionResult> GetDeviceStatus(string deviceId)
    {
        try
        {
            var response = await _client.GetDeviceStatusAsync(deviceId);

            if (response.StatusCode == 100)
            {
                return Ok(new
                {
                    success = true,
                    status = response.Body
                });
            }

            return StatusCode(500, new
            {
                success = false,
                error = response.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("{deviceId}/command")]
    public async Task<IActionResult> SendCommand(string deviceId, [FromBody] DeviceCommand command)
    {
        if (string.IsNullOrEmpty(command.Command))
        {
            return BadRequest(new
            {
                success = false,
                error = "Command is required"
            });
        }

        try
        {
            var response = await _client.SendCommandAsync(deviceId, command);

            if (response.StatusCode == 100)
            {
                return Ok(new
                {
                    success = true,
                    message = "Command sent successfully"
                });
            }

            return StatusCode(500, new
            {
                success = false,
                error = response.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("{deviceId}/on")]
    public async Task<IActionResult> TurnOn(string deviceId)
    {
        try
        {
            var response = await _client.TurnOnAsync(deviceId);

            if (response.StatusCode == 100)
            {
                // Wait a moment for device to update
                await Task.Delay(1000);

                // Get actual device status
                var statusResponse = await _client.GetDeviceStatusAsync(deviceId);
                string? actualPowerState = null;

                if (statusResponse.StatusCode == 100 && statusResponse.Body != null && statusResponse.Body.ContainsKey("power"))
                {
                    actualPowerState = statusResponse.Body["power"]?.ToString()?.ToLower();
                }

                return Ok(new
                {
                    success = true,
                    message = "Device turned on",
                    powerState = actualPowerState ?? "on"
                });
            }

            return StatusCode(500, new { success = false, error = response.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("{deviceId}/off")]
    public async Task<IActionResult> TurnOff(string deviceId)
    {
        try
        {
            var response = await _client.TurnOffAsync(deviceId);

            if (response.StatusCode == 100)
            {
                // Wait a moment for device to update
                await Task.Delay(1000);

                // Get actual device status
                var statusResponse = await _client.GetDeviceStatusAsync(deviceId);
                string? actualPowerState = null;

                if (statusResponse.StatusCode == 100 && statusResponse.Body != null && statusResponse.Body.ContainsKey("power"))
                {
                    actualPowerState = statusResponse.Body["power"]?.ToString()?.ToLower();
                }

                return Ok(new
                {
                    success = true,
                    message = "Device turned off",
                    powerState = actualPowerState ?? "off"
                });
            }

            return StatusCode(500, new { success = false, error = response.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("{deviceId}/toggle")]
    public async Task<IActionResult> Toggle(string deviceId)
    {
        try
        {
            var response = await _client.ToggleAsync(deviceId);

            if (response.StatusCode == 100)
            {
                // Wait and retry to get actual device status
                string? actualPowerState = null;

                for (int retry = 0; retry < 3; retry++)
                {
                    await Task.Delay(retry == 0 ? 1000 : 500);

                    try
                    {
                        var statusResponse = await _client.GetDeviceStatusAsync(deviceId);

                        if (statusResponse.StatusCode == 100 && statusResponse.Body != null && statusResponse.Body.ContainsKey("power"))
                        {
                            actualPowerState = statusResponse.Body["power"]?.ToString()?.ToLower();
                            if (!string.IsNullOrEmpty(actualPowerState))
                            {
                                break; // Success, exit retry loop
                            }
                        }
                    }
                    catch
                    {
                        // Continue to next retry
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Device toggled",
                    powerState = actualPowerState ?? "unknown"
                });
            }

            return StatusCode(500, new { success = false, error = response.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("{deviceId}/brightness")]
    public async Task<IActionResult> SetBrightness(string deviceId, [FromBody] BrightnessRequest request)
    {
        if (request.Brightness < 0 || request.Brightness > 100)
        {
            return BadRequest(new
            {
                success = false,
                error = "Brightness must be between 0 and 100"
            });
        }

        try
        {
            var response = await _client.SetBrightnessAsync(deviceId, request.Brightness);

            if (response.StatusCode == 100)
            {
                return Ok(new { success = true, message = "Brightness set" });
            }

            return StatusCode(500, new { success = false, error = response.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

public class BrightnessRequest
{
    public int Brightness { get; set; }
}
