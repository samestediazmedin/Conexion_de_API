using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WeatherAppBack.Models;

public class WeatherForecast
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string City { get; set; } = null!;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public string Description { get; set; } = null!;
    public DateTime SearchDate { get; set; }
}