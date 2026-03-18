using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherLux.Core.DTOs;
using WeatherLux.Core.Interfaces;

namespace WeatherLux.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alerts;

    public AlertsController(IAlertService alerts) => _alerts = alerts;

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    /// <summary>GET /api/alerts</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _alerts.GetUserAlertsAsync(UserId));

    /// <summary>POST /api/alerts</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest req)
        => Ok(await _alerts.CreateAlertAsync(UserId, req));

    /// <summary>PATCH /api/alerts/{alertId}/toggle</summary>
    [HttpPatch("{alertId}/toggle")]
    public async Task<IActionResult> Toggle(string alertId)
        => await _alerts.ToggleAlertAsync(UserId, alertId) ? Ok() : NotFound();

    /// <summary>DELETE /api/alerts/{alertId}</summary>
    [HttpDelete("{alertId}")]
    public async Task<IActionResult> Delete(string alertId)
        => await _alerts.DeleteAlertAsync(UserId, alertId) ? NoContent() : NotFound();
}
