using System.ComponentModel.DataAnnotations;

namespace WeatherLux.Core.DTOs;

// ════════════════════════════════════════════════════════
//  AUTH
// ════════════════════════════════════════════════════════
public record RegisterRequest(
    [Required, MinLength(2)] string Name,
    [Required, EmailAddress]  string Email,
    [Required, MinLength(8)]  string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required]               string Password
);

public record AuthResponse(
    string AccessToken,
    string UserId,
    string Name,
    string Email,
    string Role
);

// ════════════════════════════════════════════════════════
//  USUARIO
// ════════════════════════════════════════════════════════
public record UserProfileResponse(
    string   Id,
    string   Name,
    string   Email,
    string   Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    UserPreferencesDto Preferences,
    List<FavoriteCityDto> FavoriteCities
);

public record UserPreferencesDto(
    string TemperatureUnit,
    string Language,
    string? DefaultCity,
    bool   NotificationsEnabled
);

public record FavoriteCityDto(
    string Name,
    string Country,
    double Latitude,
    double Longitude
);

public record AddFavoriteCityRequest(
    [Required] string Name,
    [Required] string Country,
    [Required] double Latitude,
    [Required] double Longitude
);

// ════════════════════════════════════════════════════════
//  CLIMA
// ════════════════════════════════════════════════════════
public record WeatherResponse(
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string Timezone,
    CurrentWeatherDto Current,
    List<HourlyWeatherDto> Hourly,
    List<DailyWeatherDto>  Daily,
    bool   FromCache,
    DateTime FetchedAt
);

public record CurrentWeatherDto(
    double Temperature,
    double ApparentTemperature,
    int    Humidity,
    double Precipitation,
    int    WeatherCode,
    string Condition,
    double WindSpeed,
    int    WindDirection,
    double Pressure,
    double Visibility,
    double UvIndex
);

public record HourlyWeatherDto(
    string Time,
    double Temp,
    int    Code,
    string Condition,
    int    Precip,
    bool   IsCurrent
);

public record DailyWeatherDto(
    string Date,
    int    Code,
    string Condition,
    double MaxTemp,
    double MinTemp,
    string Sunrise,
    string Sunset,
    double Uv,
    double Precip
);

// ════════════════════════════════════════════════════════
//  HISTORIAL
// ════════════════════════════════════════════════════════
public record SearchHistoryResponse(
    string   Id,
    string   City,
    string   Country,
    double   Temperature,
    string   Condition,
    DateTime SearchedAt
);

// ════════════════════════════════════════════════════════
//  ALERTAS
// ════════════════════════════════════════════════════════
public record CreateAlertRequest(
    [Required] string City,
    [Required] double Latitude,
    [Required] double Longitude,
    [Required] string ConditionType,   // temp_above | temp_below | rain | wind_above | uv_above
    [Required] double Threshold
);

public record AlertResponse(
    string   Id,
    string   City,
    string   ConditionType,
    double   Threshold,
    bool     IsActive,
    DateTime CreatedAt,
    DateTime? LastTriggeredAt
);

// ════════════════════════════════════════════════════════
//  STATS (admin)
// ════════════════════════════════════════════════════════
public record ApiStatsResponse(
    long   TotalRequests,
    long   CacheHits,
    double CacheHitRate,
    double AvgResponseMs,
    long   Errors,
    List<TrendingCityDto> TrendingCities
);

public record TrendingCityDto(string City, string Country, long Searches);

// ════════════════════════════════════════════════════════════
//  ERROR
// ════════════════════════════════════════════════════════════
public record ErrorResponse(string Message, int Status = 400);

// ════════════════════════════════════════════════════════════
//  TASK (TODO)
// ════════════════════════════════════════════════════════════
public record CreateTaskRequest(
    [Required, MinLength(1)] string Title,
    string? Description = null,
    string Priority = "medium",
    DateTime? DueDate = null,
    List<string>? Tags = null
);

public record UpdateTaskRequest(
    string? Title = null,
    string? Description = null,
    string? Priority = null,
    bool? IsCompleted = null,
    DateTime? DueDate = null,
    List<string>? Tags = null
);

public record TaskItemResponse(
    string Id,
    string Title,
    string? Description,
    bool IsCompleted,
    string Priority,
    DateTime? DueDate,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt
);
