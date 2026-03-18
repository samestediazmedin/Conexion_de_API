using System.Text.Json.Serialization;

namespace WeatherAppBack.Models;

public class OpenWeatherResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("main")]
    public MainData Main { get; set; } = new();

    [JsonPropertyName("weather")]
    public List<WeatherDescription> Weather { get; set; } = new();
}

public class MainData
{
    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }
}

public class WeatherDescription
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}