using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WeatherAppBack.Models;
using System.Text.Json;

namespace WeatherAppBack.Services
{
    public class WeatherService
    {
        private readonly IMongoCollection<WeatherForecast> _forecastCollection;
        private readonly HttpClient _httpClient;

        public WeatherService(IMongoClient mongoClient, HttpClient httpClient)
        {
            var database = mongoClient.GetDatabase("WeatherLuxDB");
            _forecastCollection = database.GetCollection<WeatherForecast>("Forecasts");
            _httpClient = httpClient;
            // User-Agent es requerido por la API de Geocoding de Open-Meteo
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WeatherApp/1.0");
        }

        public async Task<WeatherForecast?> GetWeatherAndSaveAsync(string city)
{
    try 
    {
        // 1. Limpiar y codificar el nombre de la ciudad (evita errores con espacios)
        string encodedCity = Uri.EscapeDataString(city.Trim());
        var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={encodedCity}&count=1&language=es&format=json";
        
        var geoResponse = await _httpClient.GetAsync(geoUrl);
        if (!geoResponse.IsSuccessStatusCode) return null;

        var geoData = await geoResponse.Content.ReadFromJsonAsync<JsonElement>();
        
        // Verificamos si hay resultados
        if (!geoData.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            Console.WriteLine($"[Aviso]: No se encontraron coordenadas para {city}");
            return null;
        }

        // Extraer coordenadas usando la cultura invariante (asegura el punto decimal)
        var first = results[0];
        string lat = first.GetProperty("latitude").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
        string lon = first.GetProperty("longitude").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);

        // 2. Pedir el clima (usando las coordenadas extraídas)
        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code";
        
        var weatherResponse = await _httpClient.GetAsync(weatherUrl);
        if (!weatherResponse.IsSuccessStatusCode) 
        {
            var errorContent = await weatherResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[Error Open-Meteo]: {errorContent}"); // Esto te dirá el porqué exacto del 400
            return null;
        }

        var weatherData = await weatherResponse.Content.ReadFromJsonAsync<OpenMeteoResponse>();

        // 3. Crear el objeto
        var forecast = new WeatherForecast
        {
            City = first.GetProperty("name").GetString() ?? city,
            Temperature = weatherData?.Current.Temperature_2m ?? 0,
            FeelsLike = weatherData?.Current.Temperature_2m ?? 0,
            Description = MapWeatherCode(weatherData?.Current.Weather_code ?? 0),
            SearchDate = DateTime.UtcNow
        };

        // 4. Guardar en MongoDB
        try {
            await _forecastCollection.InsertOneAsync(forecast);
        } catch { /* Silenciar error de DB para no romper la respuesta */ }

        return forecast;
    }
    catch (Exception ex) 
    {
        Console.WriteLine($"[Service Error Detallado]: {ex.Message}");
        return null;
    }
}

        public async Task<List<WeatherForecast>> GetHistoryAsync() =>
            await _forecastCollection.Find(_ => true).Limit(10).ToListAsync();

        private string MapWeatherCode(int code) => code switch {
            0 => "Despejado",
            1 or 2 or 3 => "Parcialmente nublado",
            45 or 48 => "Niebla",
            51 or 53 or 55 => "Llovizna",
            61 or 63 or 65 => "Lluvia",
            71 or 73 or 75 => "Nieve",
            95 => "Tormenta",
            _ => "Nublado"
        };
    }
}