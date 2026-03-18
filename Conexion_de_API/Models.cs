namespace WeatherLux.Core.Models;

// ════════════════════════════════════════════════════════
//  USUARIO
// ════════════════════════════════════════════════════════
public class User
{
    public string Id           { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role         { get; set; } = "user";       // user | admin
    public bool   IsActive     { get; set; } = true;

    public UserPreferences    Preferences    { get; set; } = new();
    public List<FavoriteCity> FavoriteCities { get; set; } = new();

    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

public class UserPreferences
{
    public string TemperatureUnit        { get; set; } = "celsius"; // celsius | fahrenheit
    public string Language               { get; set; } = "es";
    public string? DefaultCity           { get; set; }
    public bool   NotificationsEnabled   { get; set; } = false;
}

public class FavoriteCity
{
    public string   Name      { get; set; } = string.Empty;
    public string   Country   { get; set; } = string.Empty;
    public double   Latitude  { get; set; }
    public double   Longitude { get; set; }
    public DateTime AddedAt   { get; set; } = DateTime.UtcNow;
}

// ════════════════════════════════════════════════════════
//  HISTORIAL DE BÚSQUEDAS
// ════════════════════════════════════════════════════════
public class SearchHistory
{
    public string   Id        { get; set; } = string.Empty;
    public string?  UserId    { get; set; }   // null = anónimo
    public string   City      { get; set; } = string.Empty;
    public string   Country   { get; set; } = string.Empty;
    public double   Latitude  { get; set; }
    public double   Longitude { get; set; }

    public WeatherSnapshot WeatherSnapshot { get; set; } = new();

    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
    public string?  IpAddress  { get; set; }
}

public class WeatherSnapshot
{
    public double Temperature { get; set; }
    public int    WeatherCode { get; set; }
    public string Condition   { get; set; } = string.Empty;
    public int    Humidity    { get; set; }
    public double WindSpeed   { get; set; }
}

// ════════════════════════════════════════════════════════
//  CACHE DE CLIMA
// ════════════════════════════════════════════════════════
public class WeatherCache
{
    public string   Id        { get; set; } = string.Empty;
    public string   CacheKey  { get; set; } = string.Empty;  // "lat_lon"
    public string   City      { get; set; } = string.Empty;
    public string   Country   { get; set; } = string.Empty;
    public double   Latitude  { get; set; }
    public double   Longitude { get; set; }
    public string   Data      { get; set; } = string.Empty;  // JSON serializado
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
}

// ════════════════════════════════════════════════════════
//  ALERTAS METEOROLÓGICAS
// ════════════════════════════════════════════════════════
public class WeatherAlert
{
    public string   Id        { get; set; } = string.Empty;
    public string   UserId    { get; set; } = string.Empty;
    public string   City      { get; set; } = string.Empty;
    public double   Latitude  { get; set; }
    public double   Longitude { get; set; }

    public AlertCondition Condition { get; set; } = new();

    public bool      IsActive         { get; set; } = true;
    public DateTime? LastTriggeredAt  { get; set; }
    public DateTime  CreatedAt        { get; set; } = DateTime.UtcNow;
}

public class AlertCondition
{
    /// <summary>temp_above | temp_below | rain | wind_above | uv_above</summary>
    public string Type      { get; set; } = string.Empty;
    public double Threshold { get; set; }
}

// ════════════════════════════════════════════════════════
//  LOGS DE API
// ════════════════════════════════════════════════════════
public class ApiLog
{
    public string   Id             { get; set; } = string.Empty;
    public string   Endpoint       { get; set; } = string.Empty;
    public string?  City           { get; set; }
    public string?  UserId         { get; set; }
    public int      StatusCode     { get; set; }
    public long     ResponseTimeMs { get; set; }
    public bool     CacheHit       { get; set; }
    public string?  IpAddress      { get; set; }
    public DateTime RequestedAt    { get; set; } = DateTime.UtcNow;
}

// ════════════════════════════════════════════════════════════
//  TASK (TODO)
// ════════════════════════════════════════════════════════════
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public class TaskItem
{
    public string   Id          { get; set; } = string.Empty;
    public string   UserId      { get; set; } = string.Empty;
    public string   Title       { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public bool     IsCompleted { get; set; } = false;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate    { get; set; }
    public List<string> Tags    { get; set; } = new();

    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
