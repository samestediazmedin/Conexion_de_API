using Microsoft.AspNetCore.Mvc;
using WeatherAppBack.Services;

namespace WeatherAppBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly WeatherService _weatherService;

    public WeatherController(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    // El Front llamará a: GET api/weather/Bogota
    [HttpGet("{city}")]
    public async Task<IActionResult> GetCurrentWeather(string city)
    {
        var result = await _weatherService.GetWeatherAndSaveAsync(city);
        
        if (result == null)
            return NotFound(new { mensaje = $"No pudimos encontrar el clima para la ciudad: {city}" });

        return Ok(result);
    }

    // El Front llamará a: GET api/weather/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _weatherService.GetHistoryAsync();
        return Ok(history);
    }
}