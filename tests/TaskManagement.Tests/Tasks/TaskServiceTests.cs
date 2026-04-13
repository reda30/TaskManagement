using FluentAssertions;
using Moq;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Tests.Tasks;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ITaskQueueService> _queueMock = new();
    private readonly ITaskService _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    public TaskServiceTests()
    {
        _sut = new TaskService(_taskRepoMock.Object, _cacheMock.Object, _queueMock.Object);
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_ReturnsTaskDto()
    {
        // Arrange
        _taskRepoMock.Setup(r => r.ExistsTodayAsync(UserId, "My Task", default)).ReturnsAsync(false);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default)).Returns(Task.CompletedTask);
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _queueMock.Setup(q => q.EnqueueTask(It.IsAny<Guid>()));

        var request = new CreateTaskRequest("My Task", "Description", TaskPriority.High);

        // Act
        var result = await _sut.CreateAsync(request, UserId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("My Task");
        result.Priority.Should().Be("High");
        result.Status.Should().Be("Pending");
        result.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Create_WithDuplicateTitleToday_ThrowsDuplicateTaskException()
    {
        // Arrange
        _taskRepoMock.Setup(r => r.ExistsTodayAsync(UserId, "Duplicate", default)).ReturnsAsync(true);

        var request = new CreateTaskRequest("Duplicate", "Desc", TaskPriority.Low);

        // Act
        var act = async () => await _sut.CreateAsync(request, UserId);

        // Assert
        await act.Should().ThrowAsync<DuplicateTaskException>()
            .WithMessage("*Duplicate*");
    }

    [Fact]
    public async Task Create_EnqueuesTaskForBackgroundProcessing()
    {
        // Arrange
        _taskRepoMock.Setup(r => r.ExistsTodayAsync(UserId, It.IsAny<string>(), default)).ReturnsAsync(false);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default)).Returns(Task.CompletedTask);
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var request = new CreateTaskRequest("Queue Test", "Desc", TaskPriority.Medium);

        // Act
        await _sut.CreateAsync(request, UserId);

        // Assert
        _queueMock.Verify(q => q.EnqueueTask(It.IsAny<Guid>()), Times.Once);
    }

    // ── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_CacheMiss_LoadsFromDbAndCaches()
    {
        // Arrange
        var task = TaskItem.Create("Cached Task", "Desc", TaskPriority.Medium, UserId);
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{task.Id}", default)).ReturnsAsync((TaskDto?)null);
        _taskRepoMock.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<TaskDto>(), It.IsAny<TimeSpan?>(), default))
                  .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.GetByIdAsync(task.Id, UserId);

        // Assert
        result.Id.Should().Be(task.Id);
        _cacheMock.Verify(c => c.SetAsync($"task:{task.Id}", It.IsAny<TaskDto>(), It.IsAny<TimeSpan?>(), default), Times.Once);
    }

    [Fact]
    public async Task GetById_CacheHit_ReturnsCachedAndSkipsDb()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var cached = new TaskDto(taskId, "Cached", "Desc", "Pending", "High", DateTime.UtcNow, UserId);
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}", default)).ReturnsAsync(cached);

        // Act
        var result = await _sut.GetByIdAsync(taskId, UserId);

        // Assert
        result.Should().Be(cached);
        _taskRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task GetById_TaskBelongsToDifferentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var task = TaskItem.Create("Other's Task", "Desc", TaskPriority.Low, otherUserId);
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{task.Id}", default)).ReturnsAsync((TaskDto?)null);
        _taskRepoMock.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);

        // Act
        var act = async () => await _sut.GetByIdAsync(task.Id, UserId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}", default)).ReturnsAsync((TaskDto?)null);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId, default)).ReturnsAsync((TaskItem?)null);

        // Act
        var act = async () => await _sut.GetByIdAsync(taskId, UserId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsSortedByPriorityThenDate()
    {
        // Arrange
        var low    = TaskItem.Create("Low Task",    "d", TaskPriority.Low,    UserId);
        var high   = TaskItem.Create("High Task",   "d", TaskPriority.High,   UserId);
        var medium = TaskItem.Create("Medium Task", "d", TaskPriority.Medium, UserId);

        _taskRepoMock.Setup(r => r.GetByUserIdAsync(UserId, default))
                     .ReturnsAsync(new List<TaskItem> { low, high, medium });

        // Act
        var result = await _sut.GetAllForUserAsync(UserId);

        // Assert
        result[0].Priority.Should().Be("High");
        result[1].Priority.Should().Be("Medium");
        result[2].Priority.Should().Be("Low");
    }

    // ── UpdateStatus ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidOwner_UpdatesAndInvalidatesCache()
    {
        // Arrange
        var task = TaskItem.Create("Task", "Desc", TaskPriority.Medium, UserId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.RemoveAsync($"task:{task.Id}", default)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest(TaskItemStatus.Done), UserId);

        // Assert
        result.Status.Should().Be("Done");
        _cacheMock.Verify(c => c.RemoveAsync($"task:{task.Id}", default), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_WrongOwner_ThrowsUnauthorizedException()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var task = TaskItem.Create("Task", "Desc", TaskPriority.Medium, otherUserId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);

        // Act
        var act = async () => await _sut.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest(TaskItemStatus.Done), UserId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
