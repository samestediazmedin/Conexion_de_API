using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WeatherLux.Core.Interfaces;
using WeatherLux.Infrastructure.Database.Configuration;
using WeatherLux.Infrastructure.Database.Repositories;
using WeatherLux.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ════════════════════════════════════════════════════════
//  MONGODB ATLAS
// ════════════════════════════════════════════════════════
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<WeatherLuxDbContext>();
builder.Services.AddSingleton<MongoDbInitializer>();

// Repositorios
builder.Services.AddScoped<IUserRepository,          UserRepository>();
builder.Services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
builder.Services.AddScoped<IWeatherCacheRepository,  WeatherCacheRepository>();
builder.Services.AddScoped<IAlertRepository,         AlertRepository>();
builder.Services.AddScoped<IApiLogRepository,        ApiLogRepository>();

// ════════════════════════════════════════════════════════
//  SERVICIOS
// ════════════════════════════════════════════════════════
builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "WeatherLux/1.0");
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// ════════════════════════════════════════════════════════
//  JWT
// ════════════════════════════════════════════════════════
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret no configurado.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                         Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ════════════════════════════════════════════════════════
//  CORS — acepta peticiones del frontend
// ════════════════════════════════════════════════════════
builder.Services.AddCors(opt => opt.AddPolicy("Frontend", p => p
    .WithOrigins(
        "http://localhost:5500",
        "http://127.0.0.1:5500",
        "http://localhost:3000",
        "null"   // file:// en desarrollo local
    )
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// ════════════════════════════════════════════════════════
//  CONTROLLERS + SWAGGER
// ════════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WeatherLux API", Version = "v1",
        Description = "API de clima conectada a MongoDB Atlas" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa: Bearer {tu_token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ════════════════════════════════════════════════════════
//  INICIALIZAR ÍNDICES EN MONGODB ATLAS
// ════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var init   = scope.ServiceProvider.GetRequiredService<MongoDbInitializer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await init.InitializeAsync();
        logger.LogInformation("✅ MongoDB Atlas conectado — weatherlux_db");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error conectando a MongoDB Atlas");
        throw;
    }
}

// ════════════════════════════════════════════════════════
//  PIPELINE
// ════════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherLux API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check — muestra estado de la conexión Atlas
app.MapGet("/health", () => new
{
    status    = "OK",
    service   = "WeatherLux API",
    database  = "MongoDB Atlas",
    cluster   = "cluster0.wntyp4y.mongodb.net",
    version   = "1.0.0",
    timestamp = DateTime.UtcNow
});

app.Run();
