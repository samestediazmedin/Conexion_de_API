using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherLux.Core.DTOs;
using WeatherLux.Core.Interfaces;

namespace WeatherLux.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    /// <summary>GET /api/tasks</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isCompleted, [FromQuery] string? tag)
        => Ok(await _tasks.GetUserTasksAsync(UserId, isCompleted, tag));

    /// <summary>GET /api/tasks/overdue</summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
        => Ok(await _tasks.GetOverdueTasksAsync(UserId));

    /// <summary>GET /api/tasks/{id}</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var task = await _tasks.GetTaskByIdAsync(UserId, id);
        return task is null ? NotFound() : Ok(task);
    }

    /// <summary>POST /api/tasks</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest req)
        => Ok(await _tasks.CreateTaskAsync(UserId, req));

    /// <summary>PATCH /api/tasks/{id}</summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskRequest req)
    {
        var updated = await _tasks.UpdateTaskAsync(UserId, id, req);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>PATCH /api/tasks/{id}/toggle</summary>
    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(string id)
        => await _tasks.ToggleTaskCompleteAsync(UserId, id) ? Ok() : NotFound();

    /// <summary>DELETE /api/tasks/{id}</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
        => await _tasks.DeleteTaskAsync(UserId, id) ? NoContent() : NotFound();
}
