using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using WeatherLux.Core.DTOs;
using WeatherLux.Core.Interfaces;
using WeatherLux.Core.Models;

namespace WeatherLux.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository         _users;
    private readonly ISearchHistoryRepository _history;
    private readonly IConfiguration          _config;

    public UserService(
        IUserRepository users,
        ISearchHistoryRepository history,
        IConfiguration config)
    {
        _users   = users;
        _history = history;
        _config  = config;
    }

    // ── Registro ─────────────────────────────────────────
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest req)
    {
        if (await _users.EmailExistsAsync(req.Email))
            return null; // email ya en uso

        var user = new User
        {
            Name         = req.Name,
            Email        = req.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12),
            Role         = "user",
        };

        await _users.CreateAsync(user);
        return BuildAuthResponse(user);
    }

    // ── Login ─────────────────────────────────────────────
    public async Task<AuthResponse?> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);
        if (user is null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return null;

        await _users.UpdateLastLoginAsync(user.Id);
        return BuildAuthResponse(user);
    }

    // ── Perfil ───────────────────────────────────────────
    public async Task<UserProfileResponse?> GetProfileAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return null;

        return new UserProfileResponse(
            Id: user.Id, Name: user.Name, Email: user.Email,
            Role: user.Role, CreatedAt: user.CreatedAt, LastLoginAt: user.LastLoginAt,
            Preferences: new UserPreferencesDto(
                user.Preferences.TemperatureUnit,
                user.Preferences.Language,
                user.Preferences.DefaultCity,
                user.Preferences.NotificationsEnabled
            ),
            FavoriteCities: user.FavoriteCities.Select(c =>
                new FavoriteCityDto(c.Name, c.Country, c.Latitude, c.Longitude)).ToList()
        );
    }

    // ── Favoritos ─────────────────────────────────────────
    public async Task<bool> AddFavoriteCityAsync(string userId, AddFavoriteCityRequest req)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return false;

        // Máximo 10 ciudades favoritas
        if (user.FavoriteCities.Count >= 10) return false;

        await _users.AddFavoriteCityAsync(userId, new FavoriteCity
        {
            Name = req.Name, Country = req.Country,
            Latitude = req.Latitude, Longitude = req.Longitude
        });
        return true;
    }

    public async Task<bool> RemoveFavoriteCityAsync(string userId, string cityName)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return false;
        await _users.RemoveFavoriteCityAsync(userId, cityName);
        return true;
    }

    // ── Historial ─────────────────────────────────────────
    public async Task<List<SearchHistoryResponse>> GetSearchHistoryAsync(string userId, int limit = 20)
    {
        var entries = await _history.GetByUserAsync(userId, limit);
        return entries.Select(e => new SearchHistoryResponse(
            Id: e.Id, City: e.City, Country: e.Country,
            Temperature: e.WeatherSnapshot.Temperature,
            Condition:   e.WeatherSnapshot.Condition,
            SearchedAt:  e.SearchedAt
        )).ToList();
    }

    // ── JWT ───────────────────────────────────────────────
    private AuthResponse BuildAuthResponse(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Secret"] ?? "weatherlux_secret_key_change_in_production"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"] ?? "WeatherLux",
            audience: _config["Jwt:Audience"] ?? "WeatherLuxClient",
            claims:   claims,
            expires:  DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new AuthResponse(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
            UserId: user.Id, Name: user.Name,
            Email: user.Email, Role: user.Role
        );
    }
}
