using WeatherLux.Core.DTOs;
using WeatherLux.Core.Models;

namespace WeatherLux.Core.Interfaces;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(string city, string? userId = null, string? ip = null);
    Task<WeatherResponse?> GetWeatherByCoordinatesAsync(double lat, double lon, string? userId = null, string? ip = null);
}

public interface IUserService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<UserProfileResponse?> GetProfileAsync(string userId);
    Task<bool> AddFavoriteCityAsync(string userId, AddFavoriteCityRequest request);
    Task<bool> RemoveFavoriteCityAsync(string userId, string cityName);
    Task<List<SearchHistoryResponse>> GetSearchHistoryAsync(string userId, int limit = 20);
}

public interface IAlertService
{
    Task<AlertResponse> CreateAlertAsync(string userId, CreateAlertRequest request);
    Task<List<AlertResponse>> GetUserAlertsAsync(string userId);
    Task<bool> ToggleAlertAsync(string userId, string alertId);
    Task<bool> DeleteAlertAsync(string userId, string alertId);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(string id, User user);
    Task<bool> EmailExistsAsync(string email);
    Task AddFavoriteCityAsync(string userId, FavoriteCity city);
    Task RemoveFavoriteCityAsync(string userId, string cityName);
    Task UpdateLastLoginAsync(string userId);
}

public interface ISearchHistoryRepository
{
    Task SaveAsync(SearchHistory entry);
    Task<List<SearchHistory>> GetByUserAsync(string userId, int limit = 20);
    Task<List<(string City, string Country, long Count)>> GetTrendingAsync(int limit = 10);
    Task DeleteUserHistoryAsync(string userId);
}

public interface IWeatherCacheRepository
{
    Task<WeatherCache?> GetAsync(double lat, double lon);
    Task SetAsync(WeatherCache entry);
    Task InvalidateAsync(double lat, double lon);
}

public interface IAlertRepository
{
    Task<WeatherAlert> CreateAsync(WeatherAlert alert);
    Task<List<WeatherAlert>> GetByUserAsync(string userId);
    Task<WeatherAlert?> GetByIdAsync(string id);
    Task UpdateAsync(string id, WeatherAlert alert);
    Task DeleteAsync(string id);
    Task<List<WeatherAlert>> GetActiveAlertsAsync();
}

public interface IApiLogRepository
{
    Task LogAsync(ApiLog log);
    Task<(long Total, long CacheHits, double AvgMs, long Errors)> GetStatsAsync(DateTime from, DateTime to);
}

public interface ITaskRepository
{
    Task<TaskItem> CreateAsync(TaskItem task);
    Task<List<TaskItem>> GetByUserAsync(string userId, bool? isCompleted = null, string? tag = null);
    Task<TaskItem?> GetByIdAsync(string id);
    Task UpdateAsync(string id, TaskItem task);
    Task DeleteAsync(string id);
    Task<bool> ToggleCompleteAsync(string id);
    Task<List<TaskItem>> GetOverdueTasksAsync(string userId);
}

public interface ITaskService
{
    Task<TaskItemResponse> CreateTaskAsync(string userId, CreateTaskRequest request);
    Task<List<TaskItemResponse>> GetUserTasksAsync(string userId, bool? isCompleted = null, string? tag = null);
    Task<TaskItemResponse?> GetTaskByIdAsync(string userId, string id);
    Task<TaskItemResponse?> UpdateTaskAsync(string userId, string taskId, UpdateTaskRequest request);
    Task<bool> DeleteTaskAsync(string userId, string taskId);
    Task<bool> ToggleTaskCompleteAsync(string userId, string taskId);
    Task<List<TaskItemResponse>> GetOverdueTasksAsync(string userId);
}
