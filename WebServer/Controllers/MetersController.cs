using Microsoft.AspNetCore.Mvc;
using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.WebServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetersController : ControllerBase
{
    private const int MaxHours = 24 * 31;
    private readonly MeterHistoryStore _history;

    public MetersController(MeterHistoryStore history)
    {
        _history = history;
    }

    /// <summary>
    /// Recorded readings of every thermo-hygrometer with the discomfort index computed on the fly.
    /// </summary>
    [HttpGet("history")]
    public IActionResult GetHistory([FromQuery] int hours = 24)
    {
        if (hours < 1 || hours > MaxHours)
        {
            return BadRequest(new
            {
                success = false,
                error = $"hours must be between 1 and {MaxHours}"
            });
        }

        var to = DateTimeOffset.Now;
        var from = to.AddHours(-hours);
        var series = MeterHistory.BuildSeries(_history.Read(from, to));

        return Ok(new
        {
            success = true,
            from,
            to,
            intervalMinutes = DiscomfortChangeDetector.Interval.TotalMinutes,
            levels = Enum.GetValues<DiscomfortLevel>().Select(level => new
            {
                label = level.ToLabel(),
                lowerBound = level.LowerBound(),
                color = level.ToColorHex()
            }),
            devices = series.Select(s => new
            {
                s.DeviceId,
                s.DeviceName,
                points = s.Points.Select(p => new
                {
                    p.Time,
                    p.Temperature,
                    p.Humidity,
                    p.Index,
                    label = DiscomfortIndex.Classify(p.Index).ToLabel()
                })
            })
        });
    }
}
