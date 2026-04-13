using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Tasks;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(CreateTaskRequest request, Guid userId, CancellationToken ct = default);
    Task<TaskDto> GetByIdAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<TaskDto>> GetAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task<TaskDto> UpdateStatusAsync(Guid taskId, UpdateTaskStatusRequest request, Guid userId, CancellationToken ct = default);
}

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICacheService _cacheService;
    private readonly ITaskQueueService _taskQueue;

    private static string CacheKey(Guid taskId) => $"task:{taskId}";

    public TaskService(ITaskRepository taskRepository, ICacheService cacheService, ITaskQueueService taskQueue)
    {
        _taskRepository = taskRepository;
        _cacheService = cacheService;
        _taskQueue = taskQueue;
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, Guid userId, CancellationToken ct = default)
    {
        // Business rule: no duplicate titles on same day for same user
        var isDuplicate = await _taskRepository.ExistsTodayAsync(userId, request.Title, ct);
        if (isDuplicate)
            throw new DuplicateTaskException(request.Title);

        var task = TaskItem.Create(request.Title, request.Description, request.Priority, userId);

        await _taskRepository.AddAsync(task, ct);
        await _taskRepository.SaveChangesAsync(ct);

        // Send to background queue for processing simulation
        _taskQueue.EnqueueTask(task.Id);

        return MapToDto(task);
    }

    public async Task<TaskDto> GetByIdAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        // Try cache first
        var cached = await _cacheService.GetAsync<TaskDto>(CacheKey(taskId), ct);
        if (cached is not null)
        {
            if (cached.UserId != userId)
                throw new UnauthorizedException("You can only view your own tasks.");
            return cached;
        }

        // Load from DB
        var task = await _taskRepository.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException(nameof(TaskItem), taskId);

        if (task.UserId != userId)
            throw new UnauthorizedException("You can only view your own tasks.");

        var dto = MapToDto(task);

        // Store in cache for 10 minutes
        await _cacheService.SetAsync(CacheKey(taskId), dto, TimeSpan.FromMinutes(10), ct);

        return dto;
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tasks = await _taskRepository.GetByUserIdAsync(userId, ct);

        // Business rule: sort by priority (high first), then by creation date
        return tasks
            .OrderByDescending(t => (int)t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<TaskDto> UpdateStatusAsync(Guid taskId, UpdateTaskStatusRequest request, Guid userId, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException(nameof(TaskItem), taskId);

        if (task.UserId != userId)
            throw new UnauthorizedException("You can only update your own tasks.");

        task.UpdateStatus(request.Status);
        await _taskRepository.SaveChangesAsync(ct);

        // Invalidate cache
        await _cacheService.RemoveAsync(CacheKey(taskId), ct);

        return MapToDto(task);
    }

    private static TaskDto MapToDto(TaskItem t) =>
        new(t.Id, t.Title, t.Description, t.Status.ToString(), t.Priority.ToString(), t.CreatedAt, t.UserId);
}
