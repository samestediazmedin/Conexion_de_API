using WeatherLux.Core.DTOs;
using WeatherLux.Core.Interfaces;
using WeatherLux.Core.Models;

namespace WeatherLux.Infrastructure.Services;

public class AlertService : IAlertService
{
    private static readonly HashSet<string> AllowedConditionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "temp_above",
        "temp_below",
        "rain",
        "wind_above",
        "uv_above"
    };

    private readonly IAlertRepository _alerts;

    public AlertService(IAlertRepository alerts) => _alerts = alerts;

    public async Task<AlertResponse> CreateAlertAsync(string userId, CreateAlertRequest request)
    {
        var type = AllowedConditionTypes.Contains(request.ConditionType)
            ? request.ConditionType
            : "temp_above";

        var alert = new WeatherAlert
        {
            UserId = userId,
            City = request.City,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Condition = new AlertCondition
            {
                Type = type,
                Threshold = request.Threshold
            },
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _alerts.CreateAsync(alert);
        return ToResponse(created);
    }

    public async Task<List<AlertResponse>> GetUserAlertsAsync(string userId)
    {
        var alerts = await _alerts.GetByUserAsync(userId);
        return alerts.Select(ToResponse).ToList();
    }

    public async Task<bool> ToggleAlertAsync(string userId, string alertId)
    {
        var alert = await _alerts.GetByIdAsync(alertId);
        if (alert is null || alert.UserId != userId) return false;

        alert.IsActive = !alert.IsActive;
        await _alerts.UpdateAsync(alertId, alert);
        return true;
    }

    public async Task<bool> DeleteAlertAsync(string userId, string alertId)
    {
        var alert = await _alerts.GetByIdAsync(alertId);
        if (alert is null || alert.UserId != userId) return false;

        await _alerts.DeleteAsync(alertId);
        return true;
    }

    private static AlertResponse ToResponse(WeatherAlert alert) => new(
        Id: alert.Id,
        City: alert.City,
        ConditionType: alert.Condition.Type,
        Threshold: alert.Condition.Threshold,
        IsActive: alert.IsActive,
        CreatedAt: alert.CreatedAt,
        LastTriggeredAt: alert.LastTriggeredAt
    );
}
