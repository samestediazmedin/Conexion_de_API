using System.Text.Json;
using WeatherLux.Core.DTOs;
using WeatherLux.Core.Interfaces;
using WeatherLux.Core.Models;

namespace WeatherLux.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private readonly IWeatherCacheRepository  _cache;
    private readonly ISearchHistoryRepository _history;
    private readonly IApiLogRepository        _logs;
    private readonly HttpClient               _http;
    private readonly ILogger<WeatherService>  _logger;

    private const string GEO_URL = "https://geocoding-api.open-meteo.com/v1/search";
    private const string WX_URL  = "https://api.open-meteo.com/v1/forecast";

    public WeatherService(
        IWeatherCacheRepository cache,
        ISearchHistoryRepository history,
        IApiLogRepository logs,
        HttpClient http,
        ILogger<WeatherService> logger)
    {
        _cache   = cache;
        _history = history;
        _logs    = logs;
        _http    = http;
        _logger  = logger;
    }

    // ── Por nombre de ciudad ─────────────────────────────
    public async Task<WeatherResponse?> GetWeatherAsync(
        string city, string? userId = null, string? ip = null)
    {
        var geoJson = await _http.GetStringAsync(
            $"{GEO_URL}?name={Uri.EscapeDataString(city)}&count=1&language=es&format=json");

        var geo = JsonSerializer.Deserialize<JsonElement>(geoJson);
        if (!geo.TryGetProperty("results", out var r) || r.GetArrayLength() == 0)
            return null;

        var loc = r[0];
        return await FetchAndCacheAsync(
            loc.GetProperty("latitude").GetDouble(),
            loc.GetProperty("longitude").GetDouble(),
            loc.GetProperty("name").GetString()!,
            loc.TryGetProperty("country", out var c) ? c.GetString()! : "—",
            loc.TryGetProperty("timezone", out var t) ? t.GetString()! : "auto",
            userId, ip);
    }

    // ── Por coordenadas ──────────────────────────────────
    public async Task<WeatherResponse?> GetWeatherByCoordinatesAsync(
        double lat, double lon, string? userId = null, string? ip = null)
        => await FetchAndCacheAsync(lat, lon, $"{lat:F2}", $"{lon:F2}", "auto", userId, ip);

    // ── Core: cache → API → guardar ──────────────────────
    private async Task<WeatherResponse?> FetchAndCacheAsync(
        double lat, double lon, string city, string country, string tz,
        string? userId, string? ip)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool fromCache = false;
        WeatherResponse? response;

        // 1. Revisar cache MongoDB
        var cached = await _cache.GetAsync(lat, lon);
        if (cached is not null)
        {
            _logger.LogInformation("✅ Cache HIT — {City}", city);
            fromCache = true;
            response  = JsonSerializer.Deserialize<WeatherResponse>(cached.Data,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        else
        {
            // 2. Llamar Open-Meteo
            _logger.LogInformation("🌐 Cache MISS — consultando Open-Meteo para {City}", city);
            var url     = BuildUrl(lat, lon, tz);
            var wxJson  = await _http.GetStringAsync(url);
            var wx      = JsonSerializer.Deserialize<JsonElement>(wxJson);

            response = MapResponse(city, country, lat, lon, tz, wx);

            // 3. Guardar en cache
            await _cache.SetAsync(new WeatherCache
            {
                City = city, Country = country, Latitude = lat, Longitude = lon,
                Data = JsonSerializer.Serialize(response)
            });
        }

        sw.Stop();

        // 4. Guardar historial (fire-and-forget)
        if (response is not null)
            _ = SaveHistoryFireAndForget(userId, ip, city, country, lat, lon, response);

        // 5. Log
        _ = _logs.LogAsync(new ApiLog
        {
            Endpoint = "/api/weather", City = city, UserId = userId,
            StatusCode = 200, ResponseTimeMs = sw.ElapsedMilliseconds,
            CacheHit = fromCache, IpAddress = ip
        });

        return response;
    }

    private async Task SaveHistoryFireAndForget(
        string? userId, string? ip, string city, string country,
        double lat, double lon, WeatherResponse wx)
    {
        try
        {
            await _history.SaveAsync(new SearchHistory
            {
                UserId = userId, City = city, Country = country,
                Latitude = lat, Longitude = lon, IpAddress = ip,
                WeatherSnapshot = new WeatherSnapshot
                {
                    Temperature = wx.Current.Temperature,
                    WeatherCode = wx.Current.WeatherCode,
                    Condition   = wx.Current.Condition,
                    Humidity    = wx.Current.Humidity,
                    WindSpeed   = wx.Current.WindSpeed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo guardar historial");
        }
    }

    // ── URL Open-Meteo ───────────────────────────────────
    private static string BuildUrl(double lat, double lon, string tz) =>
        $"{WX_URL}?latitude={lat}&longitude={lon}" +
        "&current=temperature_2m,apparent_temperature,relative_humidity_2m,precipitation," +
        "weather_code,wind_speed_10m,wind_direction_10m,surface_pressure,visibility,uv_index" +
        "&hourly=temperature_2m,weather_code,precipitation_probability" +
        "&daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,uv_index_max,precipitation_sum" +
        $"&timezone={Uri.EscapeDataString(tz)}&forecast_days=7";

    // ── Mapear JSON → WeatherResponse ────────────────────
    private static WeatherResponse MapResponse(
        string city, string country, double lat, double lon, string tz, JsonElement d)
    {
        var c    = d.GetProperty("current");
        var h    = d.GetProperty("hourly");
        var day  = d.GetProperty("daily");
        var now  = DateTime.Now.ToString("yyyy-MM-ddTHH");

        var times = h.GetProperty("time").EnumerateArray().ToList();
        var idx   = times.FindIndex(t => t.GetString()!.StartsWith(now));
        if (idx < 0) idx = 0;

        var hourly = Enumerable.Range(0, Math.Min(24, times.Count - idx))
            .Select(i =>
            {
                var code = h.GetProperty("weather_code")[idx + i].GetInt32();
                return new HourlyWeatherDto(
                    Time:      times[idx + i].GetString()![11..16],
                    Temp:      h.GetProperty("temperature_2m")[idx + i].GetDouble(),
                    Code:      code,
                    Condition: WmoDesc(code),
                    Precip:    h.GetProperty("precipitation_probability")[idx + i].GetInt32(),
                    IsCurrent: i == 0
                );
            }).ToList();

        var dailyTimes = day.GetProperty("time").EnumerateArray().ToList();
        var daily = dailyTimes.Select((t, i) =>
        {
            var code = day.GetProperty("weather_code")[i].GetInt32();
            return new DailyWeatherDto(
                Date:    t.GetString()!,
                Code:    code,
                Condition: WmoDesc(code),
                MaxTemp: day.GetProperty("temperature_2m_max")[i].GetDouble(),
                MinTemp: day.GetProperty("temperature_2m_min")[i].GetDouble(),
                Sunrise: day.GetProperty("sunrise")[i].GetString()?[11..16] ?? "—",
                Sunset:  day.GetProperty("sunset")[i].GetString()?[11..16]  ?? "—",
                Uv:      day.GetProperty("uv_index_max")[i].GetDouble(),
                Precip:  day.GetProperty("precipitation_sum")[i].GetDouble()
            );
        }).ToList();

        var curCode = c.GetProperty("weather_code").GetInt32();

        return new WeatherResponse(
            City: city, Country: country, Latitude: lat, Longitude: lon, Timezone: tz,
            Current: new CurrentWeatherDto(
                Temperature:        c.GetProperty("temperature_2m").GetDouble(),
                ApparentTemperature: c.GetProperty("apparent_temperature").GetDouble(),
                Humidity:           c.GetProperty("relative_humidity_2m").GetInt32(),
                Precipitation:      c.GetProperty("precipitation").GetDouble(),
                WeatherCode:        curCode,
                Condition:          WmoDesc(curCode),
                WindSpeed:          c.GetProperty("wind_speed_10m").GetDouble(),
                WindDirection:      c.GetProperty("wind_direction_10m").GetInt32(),
                Pressure:           c.GetProperty("surface_pressure").GetDouble(),
                Visibility:         Math.Round(c.GetProperty("visibility").GetDouble() / 1000, 1),
                UvIndex:            c.GetProperty("uv_index").GetDouble()
            ),
            Hourly:    hourly,
            Daily:     daily,
            FromCache: false,
            FetchedAt: DateTime.UtcNow
        );
    }

    private static string WmoDesc(int code) => code switch
    {
        0     => "Cielo despejado",
        1     => "Mayormente despejado",
        2     => "Parcialmente nublado",
        3     => "Nublado",
        <= 48 => "Niebla",
        <= 55 => "Llovizna",
        <= 65 => "Lluvia",
        <= 75 => "Nevada",
        <= 82 => "Chubascos",
        _     => "Tormenta"
    };
}
