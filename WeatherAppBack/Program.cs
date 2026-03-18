using MongoDB.Driver;
using WeatherAppBack.Services;


var builder = WebApplication.CreateBuilder(args);


// 1. Configurar MongoDB
builder.Services.AddSingleton<IMongoClient>(sp => 
    new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));

// 2. Registrar el Servicio y HttpClient
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Habilitar CORS (Permitir que el Front hable con el Back)
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); // Aplicar la política
app.UseAuthorization();
app.MapControllers();

// En Program.cs
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
    
app.Run();