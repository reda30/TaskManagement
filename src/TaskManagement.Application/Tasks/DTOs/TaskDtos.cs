using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks.DTOs;

public record CreateTaskRequest(string Title, string Description, TaskPriority Priority);

public record UpdateTaskStatusRequest(TaskItemStatus Status);

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateTime CreatedAt,
    Guid UserId);
