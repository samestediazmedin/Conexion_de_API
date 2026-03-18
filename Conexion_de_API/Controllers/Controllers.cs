using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherLux.Core.DTOs;
using WeatherLux.Core.Interfaces;

namespace WeatherLux.API.Controllers;

// ════════════════════════════════════════════════════════
//  WEATHER — /api/weather
// ════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weather;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weather, ILogger<WeatherController> logger)
    {
        _weather = weather;
        _logger  = logger;
    }

    /// <summary>GET /api/weather?city=Bogotá</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest(new ErrorResponse("El parámetro 'city' es requerido."));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _weather.GetWeatherAsync(city, userId, ip);
        return result is null
            ? NotFound(new ErrorResponse($"Ciudad '{city}' no encontrada.", 404))
            : Ok(result);
    }

    /// <summary>GET /api/weather/coordinates?lat=4.6&lon=-74.08</summary>
    [HttpGet("coordinates")]
    public async Task<IActionResult> GetByCoordinates([FromQuery] double lat, [FromQuery] double lon)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _weather.GetWeatherByCoordinatesAsync(lat, lon, userId, ip);
        return result is null ? NotFound() : Ok(result);
    }
}

// ════════════════════════════════════════════════════════
//  AUTH — /api/auth
// ════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;

    public AuthController(IUserService users) => _users = users;

    /// <summary>POST /api/auth/register</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var result = await _users.RegisterAsync(req);
        return result is null
            ? Conflict(new ErrorResponse("El email ya está registrado.", 409))
            : CreatedAtAction(nameof(Register), result);
    }

    /// <summary>POST /api/auth/login</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _users.LoginAsync(req);
        return result is null
            ? Unauthorized(new ErrorResponse("Credenciales inválidas.", 401))
            : Ok(result);
    }
}

// ════════════════════════════════════════════════════════
//  USERS — /api/users
// ════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    private string UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    /// <summary>GET /api/users/me</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _users.GetProfileAsync(UserId);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>GET /api/users/me/history</summary>
    [HttpGet("me/history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
    {
        var history = await _users.GetSearchHistoryAsync(UserId, Math.Clamp(limit, 1, 100));
        return Ok(history);
    }

    /// <summary>POST /api/users/me/favorites</summary>
    [HttpPost("me/favorites")]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteCityRequest req)
    {
        var ok = await _users.AddFavoriteCityAsync(UserId, req);
        return ok ? Ok(new { message = "Ciudad agregada a favoritos." })
                  : BadRequest(new ErrorResponse("No se pudo agregar. Máximo 10 ciudades.", 400));
    }

    /// <summary>DELETE /api/users/me/favorites/{cityName}</summary>
    [HttpDelete("me/favorites/{cityName}")]
    public async Task<IActionResult> RemoveFavorite(string cityName)
    {
        await _users.RemoveFavoriteCityAsync(UserId, cityName);
        return NoContent();
    }
}

// ════════════════════════════════════════════════════════
//  ADMIN — /api/admin  (solo role=admin)
// ════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IApiLogRepository _logs;
    private readonly ISearchHistoryRepository _history;

    public AdminController(IApiLogRepository logs, ISearchHistoryRepository history)
    {
        _logs    = logs;
        _history = history;
    }

    /// <summary>GET /api/admin/stats?hours=24</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int hours = 24)
    {
        var from  = DateTime.UtcNow.AddHours(-hours);
        var to    = DateTime.UtcNow;
        var stats = await _logs.GetStatsAsync(from, to);
        var trend = await _history.GetTrendingAsync(10);

        return Ok(new ApiStatsResponse(
            TotalRequests: stats.Total,
            CacheHits:     stats.CacheHits,
            CacheHitRate:  stats.Total > 0 ? Math.Round((double)stats.CacheHits / stats.Total * 100, 1) : 0,
            AvgResponseMs: Math.Round(stats.AvgMs, 1),
            Errors:        stats.Errors,
            TrendingCities: trend.Select(t => new TrendingCityDto(t.City, t.Country, t.Count)).ToList()
        ));
    }
}
