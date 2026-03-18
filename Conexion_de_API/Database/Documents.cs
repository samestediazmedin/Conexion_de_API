using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WeatherLux.Infrastructure.Database.Models;

// ════════════════════════════════════════════════════════
//  users
// ════════════════════════════════════════════════════════
[BsonIgnoreExtraElements]
public class UserDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = "user";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("preferences")]
    public PreferencesDocument Preferences { get; set; } = new();

    [BsonElement("favoriteCities")]
    public List<FavoriteCityDocument> FavoriteCities { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}

[BsonIgnoreExtraElements]
public class PreferencesDocument
{
    [BsonElement("temperatureUnit")]
    public string TemperatureUnit { get; set; } = "celsius";

    [BsonElement("language")]
    public string Language { get; set; } = "es";

    [BsonElement("defaultCity")]
    public string? DefaultCity { get; set; }

    [BsonElement("notificationsEnabled")]
    public bool NotificationsEnabled { get; set; } = false;
}

[BsonIgnoreExtraElements]
public class FavoriteCityDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

// ════════════════════════════════════════════════════════
//  search_history
// ════════════════════════════════════════════════════════
[BsonIgnoreExtraElements]
public class SearchHistoryDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("weatherSnapshot")]
    public WeatherSnapshotDocument WeatherSnapshot { get; set; } = new();

    [BsonElement("searchedAt")]
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }
}

[BsonIgnoreExtraElements]
public class WeatherSnapshotDocument
{
    [BsonElement("temperature")]
    public double Temperature { get; set; }

    [BsonElement("weatherCode")]
    public int WeatherCode { get; set; }

    [BsonElement("condition")]
    public string Condition { get; set; } = string.Empty;

    [BsonElement("humidity")]
    public int Humidity { get; set; }

    [BsonElement("windSpeed")]
    public double WindSpeed { get; set; }
}

// ════════════════════════════════════════════════════════
//  weather_cache
// ════════════════════════════════════════════════════════
[BsonIgnoreExtraElements]
public class WeatherCacheDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("cacheKey")]
    public string CacheKey { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("data")]
    public string Data { get; set; } = string.Empty;

    [BsonElement("fetchedAt")]
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    // TTL index apunta a este campo — MongoDB lo borra automáticamente
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
}

// ════════════════════════════════════════════════════════
//  weather_alerts
// ════════════════════════════════════════════════════════
[BsonIgnoreExtraElements]
public class WeatherAlertDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("conditionType")]
    public string ConditionType { get; set; } = string.Empty;

    [BsonElement("threshold")]
    public double Threshold { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("lastTriggeredAt")]
    public DateTime? LastTriggeredAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ════════════════════════════════════════════════════════════
//  tasks (TODO)
// ════════════════════════════════════════════════════════════
[BsonIgnoreExtraElements]
public class TaskItemDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("isCompleted")]
    public bool IsCompleted { get; set; } = false;

    [BsonElement("priority")]
    public string Priority { get; set; } = "medium";

    [BsonElement("dueDate")]
    public DateTime? DueDate { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

// ════════════════════════════════════════════════════════════
//  api_logs
// ════════════════════════════════════════════════════════════
[BsonIgnoreExtraElements]
public class ApiLogDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("statusCode")]
    public int StatusCode { get; set; }

    [BsonElement("responseTimeMs")]
    public long ResponseTimeMs { get; set; }

    [BsonElement("cacheHit")]
    public bool CacheHit { get; set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    // TTL index — se borra después de 30 días
    [BsonElement("requestedAt")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
