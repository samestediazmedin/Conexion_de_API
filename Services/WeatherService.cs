using MongoDB.Driver;
using WeatherAppBack.Models;

namespace WeatherAppBack.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IMongoCollection<WeatherForecast> _forecastCollection;

    public WeatherService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["WeatherSettings:ApiKey"] ?? throw new ArgumentNullException("Falta el ApiKey");

        // Conexión a MongoDB
        var mongoClient = new MongoClient(config["WeatherDbSettings:ConnectionString"]);
        var mongoDatabase = mongoClient.GetDatabase(config["WeatherDbSettings:DatabaseName"]);
        _forecastCollection = mongoDatabase.GetCollection<WeatherForecast>(config["WeatherDbSettings:CollectionName"]);
    }

    public async Task<WeatherForecast?> GetWeatherAndSaveAsync(string city)
    {
        // 1. Consultar a OpenWeather
        var response = await _httpClient.GetAsync($"?q={city}&appid={_apiKey}&units=metric&lang=es");
        
        if (!response.IsSuccessStatusCode) return null;

        // 2. C# convierte los datos automáticamente a tus clases
        var data = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>();
        
        if (data == null || data.Weather.Count == 0) return null;

        // 3. Crear el objeto para tu base de datos
        var newForecast = new WeatherForecast
        {
            City = data.Name,
            Temperature = data.Main.Temp,
            FeelsLike = data.Main.FeelsLike,
            Description = data.Weather[0].Description,
            SearchDate = DateTime.UtcNow
        };

        // 4. Guardar en MongoDB
        await _forecastCollection.InsertOneAsync(newForecast);

        return newForecast;
    }

    // Método extra para que el Front pueda ver el historial de búsquedas
    public async Task<List<WeatherForecast>> GetHistoryAsync() =>
        await _forecastCollection.Find(_ => true)
            .SortByDescending(x => x.SearchDate)
            .Limit(10) // Trae los últimos 10
            .ToListAsync();
}