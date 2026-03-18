using Microsoft.AspNetCore.Mvc;
using WeatherAppBack.Models;
using WeatherAppBack.Services;

namespace WeatherAppBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("{city}")]
        public async Task<IActionResult> GetWeather(string city)
        {
            var result = await _weatherService.GetWeatherAndSaveAsync(city);
            return result == null ? NotFound("No se encontró la ciudad") : Ok(result);
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<WeatherForecast>>> GetHistory()
        {
            return Ok(await _weatherService.GetHistoryAsync());
        }
    }
}