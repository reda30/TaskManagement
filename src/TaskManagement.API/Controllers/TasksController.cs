using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Tasks.DTOs;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService) => _taskService = taskService;

    /// <summary>
    /// Get all tasks for the authenticated user.
    /// Results are sorted by priority (High → Low) then by creation date (oldest first).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tasks = await _taskService.GetAllForUserAsync(GetCurrentUserId(), ct);
        return Ok(tasks);
    }

    /// <summary>
    /// Get a task by ID. Result is cached in Redis for 10 minutes.
    /// </summary>
    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid taskId, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(taskId, GetCurrentUserId(), ct);
        return Ok(task);
    }

    /// <summary>
    /// Create a new task. Duplicate titles on the same day are rejected.
    /// A background worker will automatically transition the task through Pending → InProgress → Done.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _taskService.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { taskId = task.Id }, task);
    }

    /// <summary>
    /// Update the status of a task. This also invalidates the Redis cache for that task.
    /// </summary>
    [HttpPatch("{taskId:guid}/status")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var task = await _taskService.UpdateStatusAsync(taskId, request, GetCurrentUserId(), ct);
        return Ok(task);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}
