using WeatherAppBack.Services;
using WeatherAppBack.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar HttpClient (para consumir la API externa)
builder.Services.AddHttpClient<WeatherService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WeatherSettings:BaseUrl"]!);
});

// 2. Configurar CORS (Para que el Front se pueda conectar sin bloqueos de seguridad)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.AllowAnyOrigin() // En producción, aquí pones la URL del Front
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Middlewares
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("PermitirFrontend"); // Habilitar acceso al Front
app.UseAuthorization();
app.MapControllers();

app.Run();