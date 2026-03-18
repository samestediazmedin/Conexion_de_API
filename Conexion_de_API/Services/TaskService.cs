using WeatherLux.Core.DTOs;
using WeatherLux.Core.Interfaces;
using WeatherLux.Core.Models;

namespace WeatherLux.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;

    public TaskService(ITaskRepository tasks) => _tasks = tasks;

    public async Task<TaskItemResponse> CreateTaskAsync(string userId, CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Priority = ParsePriority(request.Priority),
            DueDate = request.DueDate,
            Tags = request.Tags ?? new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _tasks.CreateAsync(task);
        return ToResponse(created);
    }

    public async Task<List<TaskItemResponse>> GetUserTasksAsync(
        string userId,
        bool? isCompleted = null,
        string? tag = null)
    {
        var tasks = await _tasks.GetByUserAsync(userId, isCompleted, tag);
        return tasks.Select(ToResponse).ToList();
    }

    public async Task<TaskItemResponse?> GetTaskByIdAsync(string userId, string id)
    {
        var task = await _tasks.GetByIdAsync(id);
        return task is null || task.UserId != userId ? null : ToResponse(task);
    }

    public async Task<TaskItemResponse?> UpdateTaskAsync(string userId, string taskId, UpdateTaskRequest request)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || task.UserId != userId) return null;

        if (!string.IsNullOrWhiteSpace(request.Title))
            task.Title = request.Title;

        if (request.Description is not null)
            task.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.Priority))
            task.Priority = ParsePriority(request.Priority);

        if (request.DueDate is not null)
            task.DueDate = request.DueDate;

        if (request.Tags is not null)
            task.Tags = request.Tags;

        if (request.IsCompleted is not null)
        {
            task.IsCompleted = request.IsCompleted.Value;
            task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _tasks.UpdateAsync(taskId, task);

        return ToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(string userId, string taskId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || task.UserId != userId) return false;

        await _tasks.DeleteAsync(taskId);
        return true;
    }

    public async Task<bool> ToggleTaskCompleteAsync(string userId, string taskId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || task.UserId != userId) return false;

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;

        await _tasks.UpdateAsync(taskId, task);
        return true;
    }

    public async Task<List<TaskItemResponse>> GetOverdueTasksAsync(string userId)
    {
        var tasks = await _tasks.GetOverdueTasksAsync(userId);
        return tasks.Select(ToResponse).ToList();
    }

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

    private static TaskItemResponse ToResponse(TaskItem task) => new(
        Id: task.Id,
        Title: task.Title,
        Description: task.Description,
        IsCompleted: task.IsCompleted,
        Priority: task.Priority.ToString().ToLowerInvariant(),
        DueDate: task.DueDate,
        Tags: task.Tags,
        CreatedAt: task.CreatedAt,
        UpdatedAt: task.UpdatedAt,
        CompletedAt: task.CompletedAt
    );
}
