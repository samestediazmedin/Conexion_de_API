using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WeatherLux.Infrastructure.Database.Models;

namespace WeatherLux.Infrastructure.Database.Configuration;

// ════════════════════════════════════════════════════════
//  SETTINGS
// ════════════════════════════════════════════════════════
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName     { get; set; } = "weatherlux_db";
}

// ════════════════════════════════════════════════════════
//  DB CONTEXT — Compatible con MongoDB Atlas
// ════════════════════════════════════════════════════════
public class WeatherLuxDbContext
{
    private readonly IMongoDatabase _db;

    public WeatherLuxDbContext(IOptions<MongoDbSettings> options)
    {
        var settings = MongoClientSettings.FromConnectionString(
            options.Value.ConnectionString);

        // Requerido para MongoDB Atlas
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        // Timeout de conexión optimizado para Atlas
        settings.ConnectTimeout      = TimeSpan.FromSeconds(30);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
        settings.SocketTimeout       = TimeSpan.FromSeconds(30);

        var client = new MongoClient(settings);
        _db = client.GetDatabase(options.Value.DatabaseName);
    }

    // ── Colecciones ───────────────────────────────────────
    public IMongoCollection<UserDocument>          Users         => Col<UserDocument>("users");
    public IMongoCollection<SearchHistoryDocument> SearchHistory => Col<SearchHistoryDocument>("search_history");
    public IMongoCollection<WeatherCacheDocument>  WeatherCache  => Col<WeatherCacheDocument>("weather_cache");
    public IMongoCollection<WeatherAlertDocument>  WeatherAlerts => Col<WeatherAlertDocument>("weather_alerts");
    public IMongoCollection<ApiLogDocument>        ApiLogs       => Col<ApiLogDocument>("api_logs");
    public IMongoCollection<TaskItemDocument>      Tasks         => Col<TaskItemDocument>("tasks");

    public IMongoDatabase Database => _db;

    private IMongoCollection<T> Col<T>(string name) => _db.GetCollection<T>(name);
}

// ════════════════════════════════════════════════════════
//  INICIALIZADOR DE ÍNDICES
// ════════════════════════════════════════════════════════
public class MongoDbInitializer
{
    private readonly WeatherLuxDbContext      _ctx;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(WeatherLuxDbContext ctx, ILogger<MongoDbInitializer> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("🗄️  Inicializando índices en MongoDB Atlas...");
        try
        {
            await CreateUsersIndexesAsync();
            await CreateSearchHistoryIndexesAsync();
            await CreateWeatherCacheIndexesAsync();
            await CreateWeatherAlertsIndexesAsync();
            await CreateApiLogsIndexesAsync();
            await CreateTasksIndexesAsync();
            _logger.LogInformation("✅ Índices creados en Atlas — weatherlux_db");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creando índices en MongoDB Atlas");
            throw;
        }
    }

    private async Task CreateUsersIndexesAsync()
    {
        var col = _ctx.Users;
        await col.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "idx_users_email_unique" }),

            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(u => u.Role),
                new CreateIndexOptions { Name = "idx_users_role" }),

            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(u => u.IsActive),
                new CreateIndexOptions { Name = "idx_users_isActive" }),

            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Descending(u => u.CreatedAt),
                new CreateIndexOptions { Name = "idx_users_createdAt" }),
        });
    }

    private async Task CreateSearchHistoryIndexesAsync()
    {
        var col = _ctx.SearchHistory;
        await col.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<SearchHistoryDocument>(
                Builders<SearchHistoryDocument>.IndexKeys
                    .Ascending(s => s.UserId)
                    .Descending(s => s.SearchedAt),
                new CreateIndexOptions { Name = "idx_history_user_date" }),

            new CreateIndexModel<SearchHistoryDocument>(
                Builders<SearchHistoryDocument>.IndexKeys.Ascending(s => s.City),
                new CreateIndexOptions { Name = "idx_history_city" }),

            // TTL: búsquedas anónimas se borran en 7 días
            new CreateIndexModel<SearchHistoryDocument>(
                Builders<SearchHistoryDocument>.IndexKeys.Ascending(s => s.SearchedAt),
                new CreateIndexOptions
                {
                    Name        = "idx_history_ttl_7d",
                    ExpireAfter = TimeSpan.FromDays(7)
                }),
        });
    }

    private async Task CreateWeatherCacheIndexesAsync()
    {
        var col = _ctx.WeatherCache;
        await col.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<WeatherCacheDocument>(
                Builders<WeatherCacheDocument>.IndexKeys.Ascending(c => c.CacheKey),
                new CreateIndexOptions { Unique = true, Name = "idx_cache_key_unique" }),

            // TTL: caché expira en 30 minutos (controlado por ExpiresAt)
            new CreateIndexModel<WeatherCacheDocument>(
                Builders<WeatherCacheDocument>.IndexKeys.Ascending(c => c.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "idx_cache_ttl_30m" }),
        });
    }

    private async Task CreateWeatherAlertsIndexesAsync()
    {
        var col = _ctx.WeatherAlerts;
        await col.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<WeatherAlertDocument>(
                Builders<WeatherAlertDocument>.IndexKeys
                    .Ascending(a => a.UserId)
                    .Ascending(a => a.IsActive),
                new CreateIndexOptions { Name = "idx_alerts_user_active" }),
        });
    }

    private async Task CreateApiLogsIndexesAsync()
    {
        var col = _ctx.ApiLogs;
        await col.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<ApiLogDocument>(
                Builders<ApiLogDocument>.IndexKeys.Descending(l => l.RequestedAt),
                new CreateIndexOptions { Name = "idx_logs_date_desc" }),

            new CreateIndexModel<ApiLogDocument>(
                Builders<ApiLogDocument>.IndexKeys.Ascending(l => l.Endpoint),
                new CreateIndexOptions { Name = "idx_logs_endpoint" }),

            // TTL: logs se borran después de 30 días
            new CreateIndexModel<ApiLogDocument>(
                Builders<ApiLogDocument>.IndexKeys.Ascending(l => l.RequestedAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30), Name = "idx_logs_ttl_30d" }),
        });
    }

    private async Task CreateTasksIndexesAsync()
    {
        var col = _ctx.Tasks;
        await col.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<TaskItemDocument>(
                Builders<TaskItemDocument>.IndexKeys
                    .Ascending(t => t.UserId)
                    .Descending(t => t.CreatedAt),
                new CreateIndexOptions { Name = "idx_tasks_user_date" }),

            new CreateIndexModel<TaskItemDocument>(
                Builders<TaskItemDocument>.IndexKeys
                    .Ascending(t => t.UserId)
                    .Ascending(t => t.IsCompleted),
                new CreateIndexOptions { Name = "idx_tasks_user_completed" }),

            new CreateIndexModel<TaskItemDocument>(
                Builders<TaskItemDocument>.IndexKeys
                    .Ascending(t => t.UserId)
                    .Ascending(t => t.Priority),
                new CreateIndexOptions { Name = "idx_tasks_user_priority" }),

            new CreateIndexModel<TaskItemDocument>(
                Builders<TaskItemDocument>.IndexKeys
                    .Ascending(t => t.UserId)
                    .Ascending(t => t.DueDate),
                new CreateIndexOptions { Name = "idx_tasks_user_duedate" }),
        });
    }
}
