using MongoDB.Bson;
using MongoDB.Driver;
using WeatherLux.Core.Interfaces;
using WeatherLux.Core.Models;
using WeatherLux.Infrastructure.Database.Configuration;
using WeatherLux.Infrastructure.Database.Models;

namespace WeatherLux.Infrastructure.Database.Repositories;

// ════════════════════════════════════════════════════════
//  USER REPOSITORY
// ════════════════════════════════════════════════════════
public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<UserDocument> _col;

    public UserRepository(WeatherLuxDbContext ctx) => _col = ctx.Users;

    public async Task<User?> GetByIdAsync(string id)
    {
        var doc = await _col.Find(u => u.Id == id).FirstOrDefaultAsync();
        return doc is null ? null : Map(doc);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var doc = await _col.Find(u => u.Email == email.ToLowerInvariant()).FirstOrDefaultAsync();
        return doc is null ? null : Map(doc);
    }

    public async Task<bool> EmailExistsAsync(string email) =>
        await _col.Find(u => u.Email == email.ToLowerInvariant()).AnyAsync();

    public async Task<User> CreateAsync(User user)
    {
        var doc = MapToDoc(user);
        doc.Email     = doc.Email.ToLowerInvariant();
        doc.CreatedAt = DateTime.UtcNow;
        await _col.InsertOneAsync(doc);
        user.Id = doc.Id;
        return user;
    }

    public async Task UpdateAsync(string id, User user) =>
        await _col.ReplaceOneAsync(u => u.Id == id, MapToDoc(user));

    public async Task AddFavoriteCityAsync(string userId, FavoriteCity city)
    {
        var cityDoc = new FavoriteCityDocument
        {
            Name = city.Name, Country = city.Country,
            Latitude = city.Latitude, Longitude = city.Longitude,
            AddedAt = DateTime.UtcNow
        };
        var update = Builders<UserDocument>.Update.AddToSet(u => u.FavoriteCities, cityDoc);
        await _col.UpdateOneAsync(u => u.Id == userId, update);
    }

    public async Task RemoveFavoriteCityAsync(string userId, string cityName)
    {
        var pull = Builders<UserDocument>.Update.PullFilter(
            u => u.FavoriteCities,
            Builders<FavoriteCityDocument>.Filter.Eq(c => c.Name, cityName));
        await _col.UpdateOneAsync(u => u.Id == userId, pull);
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        var update = Builders<UserDocument>.Update.Set(u => u.LastLoginAt, DateTime.UtcNow);
        await _col.UpdateOneAsync(u => u.Id == userId, update);
    }

    // ── Mappers ───────────────────────────────────────────
    private static User Map(UserDocument d) => new()
    {
        Id = d.Id, Name = d.Name, Email = d.Email,
        PasswordHash = d.PasswordHash, Role = d.Role, IsActive = d.IsActive,
        CreatedAt = d.CreatedAt, LastLoginAt = d.LastLoginAt,
        Preferences = new()
        {
            TemperatureUnit      = d.Preferences.TemperatureUnit,
            Language             = d.Preferences.Language,
            DefaultCity          = d.Preferences.DefaultCity,
            NotificationsEnabled = d.Preferences.NotificationsEnabled
        },
        FavoriteCities = d.FavoriteCities.Select(c => new FavoriteCity
        {
            Name = c.Name, Country = c.Country,
            Latitude = c.Latitude, Longitude = c.Longitude, AddedAt = c.AddedAt
        }).ToList()
    };

    private static UserDocument MapToDoc(User u) => new()
    {
        Id = u.Id, Name = u.Name, Email = u.Email,
        PasswordHash = u.PasswordHash, Role = u.Role, IsActive = u.IsActive,
        CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt,
        Preferences = new()
        {
            TemperatureUnit      = u.Preferences.TemperatureUnit,
            Language             = u.Preferences.Language,
            DefaultCity          = u.Preferences.DefaultCity,
            NotificationsEnabled = u.Preferences.NotificationsEnabled
        },
        FavoriteCities = u.FavoriteCities.Select(c => new FavoriteCityDocument
        {
            Name = c.Name, Country = c.Country,
            Latitude = c.Latitude, Longitude = c.Longitude, AddedAt = c.AddedAt
        }).ToList()
    };
}

// ════════════════════════════════════════════════════════
//  SEARCH HISTORY REPOSITORY
// ════════════════════════════════════════════════════════
public class SearchHistoryRepository : ISearchHistoryRepository
{
    private readonly IMongoCollection<SearchHistoryDocument> _col;

    public SearchHistoryRepository(WeatherLuxDbContext ctx) => _col = ctx.SearchHistory;

    public async Task SaveAsync(SearchHistory entry)
    {
        var doc = new SearchHistoryDocument
        {
            UserId    = entry.UserId,
            City      = entry.City,
            Country   = entry.Country,
            Latitude  = entry.Latitude,
            Longitude = entry.Longitude,
            IpAddress = entry.IpAddress,
            SearchedAt = DateTime.UtcNow,
            WeatherSnapshot = new WeatherSnapshotDocument
            {
                Temperature = entry.WeatherSnapshot.Temperature,
                WeatherCode = entry.WeatherSnapshot.WeatherCode,
                Condition   = entry.WeatherSnapshot.Condition,
                Humidity    = entry.WeatherSnapshot.Humidity,
                WindSpeed   = entry.WeatherSnapshot.WindSpeed
            }
        };
        await _col.InsertOneAsync(doc);
    }

    public async Task<List<SearchHistory>> GetByUserAsync(string userId, int limit = 20)
    {
        var docs = await _col
            .Find(s => s.UserId == userId)
            .SortByDescending(s => s.SearchedAt)
            .Limit(limit)
            .ToListAsync();

        return docs.Select(d => new SearchHistory
        {
            Id = d.Id, UserId = d.UserId, City = d.City, Country = d.Country,
            Latitude = d.Latitude, Longitude = d.Longitude,
            SearchedAt = d.SearchedAt, IpAddress = d.IpAddress,
            WeatherSnapshot = new WeatherSnapshot
            {
                Temperature = d.WeatherSnapshot.Temperature,
                WeatherCode = d.WeatherSnapshot.WeatherCode,
                Condition   = d.WeatherSnapshot.Condition,
                Humidity    = d.WeatherSnapshot.Humidity,
                WindSpeed   = d.WeatherSnapshot.WindSpeed
            }
        }).ToList();
    }

    public async Task<List<(string City, string Country, long Count)>> GetTrendingAsync(int limit = 10)
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument { { "city", "$city" }, { "country", "$country" } } },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("count", -1)),
            new BsonDocument("$limit", limit)
        };

        var results = await _col.Aggregate<BsonDocument>(pipeline).ToListAsync();

        return results.Select(r => (
            City:    r["_id"]["city"].AsString,
            Country: r["_id"]["country"].AsString,
            Count:   r["count"].AsInt64 == 0 ? (long)r["count"].AsInt32 : r["count"].AsInt64
        )).ToList();
    }

    public async Task DeleteUserHistoryAsync(string userId) =>
        await _col.DeleteManyAsync(s => s.UserId == userId);
}

// ════════════════════════════════════════════════════════
//  WEATHER CACHE REPOSITORY
// ════════════════════════════════════════════════════════
public class WeatherCacheRepository : IWeatherCacheRepository
{
    private readonly IMongoCollection<WeatherCacheDocument> _col;

    public WeatherCacheRepository(WeatherLuxDbContext ctx) => _col = ctx.WeatherCache;

    public async Task<WeatherCache?> GetAsync(double lat, double lon)
    {
        var key = CacheKey(lat, lon);
        var doc = await _col.Find(c => c.CacheKey == key && c.ExpiresAt > DateTime.UtcNow)
                            .FirstOrDefaultAsync();
        if (doc is null) return null;
        return new WeatherCache
        {
            Id = doc.Id, CacheKey = doc.CacheKey,
            City = doc.City, Country = doc.Country,
            Latitude = doc.Latitude, Longitude = doc.Longitude,
            Data = doc.Data, FetchedAt = doc.FetchedAt, ExpiresAt = doc.ExpiresAt
        };
    }

    public async Task SetAsync(WeatherCache entry)
    {
        var key = CacheKey(entry.Latitude, entry.Longitude);
        var doc = new WeatherCacheDocument
        {
            CacheKey  = key,
            City      = entry.City,
            Country   = entry.Country,
            Latitude  = entry.Latitude,
            Longitude = entry.Longitude,
            Data      = entry.Data,
            FetchedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        await _col.ReplaceOneAsync(
            c => c.CacheKey == key,
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task InvalidateAsync(double lat, double lon) =>
        await _col.DeleteOneAsync(c => c.CacheKey == CacheKey(lat, lon));

    private static string CacheKey(double lat, double lon) =>
        $"{lat:F2}_{lon:F2}";
}

// ════════════════════════════════════════════════════════
//  ALERT REPOSITORY
// ════════════════════════════════════════════════════════
public class AlertRepository : IAlertRepository
{
    private readonly IMongoCollection<WeatherAlertDocument> _col;

    public AlertRepository(WeatherLuxDbContext ctx) => _col = ctx.WeatherAlerts;

    public async Task<WeatherAlert> CreateAsync(WeatherAlert alert)
    {
        var doc = new WeatherAlertDocument
        {
            UserId        = alert.UserId,
            City          = alert.City,
            Latitude      = alert.Latitude,
            Longitude     = alert.Longitude,
            ConditionType = alert.Condition.Type,
            Threshold     = alert.Condition.Threshold,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };
        await _col.InsertOneAsync(doc);
        alert.Id = doc.Id;
        return alert;
    }

    public async Task<List<WeatherAlert>> GetByUserAsync(string userId)
    {
        var docs = await _col.Find(a => a.UserId == userId)
                             .SortByDescending(a => a.CreatedAt)
                             .ToListAsync();
        return docs.Select(Map).ToList();
    }

    public async Task<WeatherAlert?> GetByIdAsync(string id)
    {
        var doc = await _col.Find(a => a.Id == id).FirstOrDefaultAsync();
        return doc is null ? null : Map(doc);
    }

    public async Task UpdateAsync(string id, WeatherAlert alert)
    {
        var update = Builders<WeatherAlertDocument>.Update
            .Set(a => a.IsActive,       alert.IsActive)
            .Set(a => a.LastTriggeredAt, alert.LastTriggeredAt);
        await _col.UpdateOneAsync(a => a.Id == id, update);
    }

    public async Task DeleteAsync(string id) =>
        await _col.DeleteOneAsync(a => a.Id == id);

    public async Task<List<WeatherAlert>> GetActiveAlertsAsync()
    {
        var docs = await _col.Find(a => a.IsActive).ToListAsync();
        return docs.Select(Map).ToList();
    }

    private static WeatherAlert Map(WeatherAlertDocument d) => new()
    {
        Id = d.Id, UserId = d.UserId, City = d.City,
        Latitude = d.Latitude, Longitude = d.Longitude,
        IsActive = d.IsActive, CreatedAt = d.CreatedAt,
        LastTriggeredAt = d.LastTriggeredAt,
        Condition = new AlertCondition { Type = d.ConditionType, Threshold = d.Threshold }
    };
}

// ════════════════════════════════════════════════════════
//  API LOG REPOSITORY
// ════════════════════════════════════════════════════════
public class ApiLogRepository : IApiLogRepository
{
    private readonly IMongoCollection<ApiLogDocument> _col;

    public ApiLogRepository(WeatherLuxDbContext ctx) => _col = ctx.ApiLogs;

    public async Task LogAsync(ApiLog log)
    {
        var doc = new ApiLogDocument
        {
            Endpoint       = log.Endpoint,
            City           = log.City,
            UserId         = log.UserId,
            StatusCode     = log.StatusCode,
            ResponseTimeMs = log.ResponseTimeMs,
            CacheHit       = log.CacheHit,
            IpAddress      = log.IpAddress,
            RequestedAt    = DateTime.UtcNow
        };
        await _col.InsertOneAsync(doc);
    }

    public async Task<(long Total, long CacheHits, double AvgMs, long Errors)> GetStatsAsync(
        DateTime from, DateTime to)
    {
        var filter = Builders<ApiLogDocument>.Filter.And(
            Builders<ApiLogDocument>.Filter.Gte(l => l.RequestedAt, from),
            Builders<ApiLogDocument>.Filter.Lte(l => l.RequestedAt, to));

        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "requestedAt", new BsonDocument { { "$gte", from }, { "$lte", to } } }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id",       BsonNull.Value },
                { "total",     new BsonDocument("$sum", 1) },
                { "cacheHits", new BsonDocument("$sum",
                    new BsonDocument("$cond", new BsonArray { "$cacheHit", 1, 0 })) },
                { "avgMs",     new BsonDocument("$avg", "$responseTimeMs") },
                { "errors",    new BsonDocument("$sum",
                    new BsonDocument("$cond", new BsonArray {
                        new BsonDocument("$gte", new BsonArray { "$statusCode", 400 }), 1, 0
                    })) }
            })
        };

        var result = await _col.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        if (result is null) return (0, 0, 0, 0);

        return (
            Total:     result["total"].AsInt32,
            CacheHits: result["cacheHits"].AsInt32,
            AvgMs:     result["avgMs"].AsDouble,
            Errors:    result["errors"].AsInt32
        );
    }
}

// ════════════════════════════════════════════════════════
//  TASK REPOSITORY
// ════════════════════════════════════════════════════════
public class TaskRepository : ITaskRepository
{
    private readonly IMongoCollection<TaskItemDocument> _col;

    public TaskRepository(WeatherLuxDbContext ctx) => _col = ctx.Tasks;

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        var doc = MapToDoc(task);
        doc.CreatedAt = DateTime.UtcNow;
        doc.UpdatedAt = DateTime.UtcNow;
        await _col.InsertOneAsync(doc);
        task.Id = doc.Id;
        return task;
    }

    public async Task<List<TaskItem>> GetByUserAsync(string userId, bool? isCompleted = null, string? tag = null)
    {
        var filter = Builders<TaskItemDocument>.Filter.Eq(t => t.UserId, userId);

        if (isCompleted is not null)
            filter &= Builders<TaskItemDocument>.Filter.Eq(t => t.IsCompleted, isCompleted.Value);

        if (!string.IsNullOrWhiteSpace(tag))
            filter &= Builders<TaskItemDocument>.Filter.AnyEq(t => t.Tags, tag);

        var docs = await _col.Find(filter)
            .SortByDescending(t => t.CreatedAt)
            .ToListAsync();

        return docs.Select(Map).ToList();
    }

    public async Task<TaskItem?> GetByIdAsync(string id)
    {
        var doc = await _col.Find(t => t.Id == id).FirstOrDefaultAsync();
        return doc is null ? null : Map(doc);
    }

    public async Task UpdateAsync(string id, TaskItem task) =>
        await _col.ReplaceOneAsync(t => t.Id == id, MapToDoc(task));

    public async Task DeleteAsync(string id) =>
        await _col.DeleteOneAsync(t => t.Id == id);

    public async Task<bool> ToggleCompleteAsync(string id)
    {
        var doc = await _col.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (doc is null) return false;

        var newValue = !doc.IsCompleted;
        var update = Builders<TaskItemDocument>.Update
            .Set(t => t.IsCompleted, newValue)
            .Set(t => t.CompletedAt, newValue ? DateTime.UtcNow : null)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        await _col.UpdateOneAsync(t => t.Id == id, update);
        return true;
    }

    public async Task<List<TaskItem>> GetOverdueTasksAsync(string userId)
    {
        var filter = Builders<TaskItemDocument>.Filter.And(
            Builders<TaskItemDocument>.Filter.Eq(t => t.UserId, userId),
            Builders<TaskItemDocument>.Filter.Eq(t => t.IsCompleted, false),
            Builders<TaskItemDocument>.Filter.Lt(t => t.DueDate, DateTime.UtcNow));

        var docs = await _col.Find(filter)
            .SortBy(t => t.DueDate)
            .ToListAsync();

        return docs.Select(Map).ToList();
    }

    private static TaskItem Map(TaskItemDocument d) => new()
    {
        Id = d.Id,
        UserId = d.UserId,
        Title = d.Title,
        Description = d.Description,
        IsCompleted = d.IsCompleted,
        Priority = ParsePriority(d.Priority),
        DueDate = d.DueDate,
        Tags = d.Tags,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        CompletedAt = d.CompletedAt
    };

    private static TaskItemDocument MapToDoc(TaskItem t) => new()
    {
        Id = t.Id,
        UserId = t.UserId,
        Title = t.Title,
        Description = t.Description,
        IsCompleted = t.IsCompleted,
        Priority = t.Priority.ToString().ToLowerInvariant(),
        DueDate = t.DueDate,
        Tags = t.Tags,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CompletedAt = t.CompletedAt
    };

    private static TaskPriority ParsePriority(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return TaskPriority.Medium;

        return value.Trim().ToLowerInvariant() switch
        {
            "low" => TaskPriority.Low,
            "medium" => TaskPriority.Medium,
            "high" => TaskPriority.High,
            "urgent" => TaskPriority.Urgent,
            _ => TaskPriority.Medium
        };
    }
}
