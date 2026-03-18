using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WeatherAppBack.Models
{
    public class WeatherForecast
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string City { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime SearchDate { get; set; }
    }

    // Clases auxiliares para leer la respuesta de Open-Meteo
    public class OpenMeteoResponse
    {
        public CurrentData Current { get; set; } = new();
    }

    public class CurrentData
    {
        public double Temperature_2m { get; set; }
        public int Weather_code { get; set; }
    }
}