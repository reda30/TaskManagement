using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.BackgroundJobs;


public class TaskProcessingQueue : ITaskQueueService
{
    private readonly ConcurrentQueue<Guid> _queue = new();

    public void EnqueueTask(Guid taskId) => _queue.Enqueue(taskId);

    public bool TryDequeue(out Guid taskId) => _queue.TryDequeue(out taskId);
}


public class TaskProcessingWorker : BackgroundService
{
    private readonly TaskProcessingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskProcessingWorker> _logger;

    public TaskProcessingWorker(
        TaskProcessingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<TaskProcessingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskProcessingWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var taskId))
            {
                await ProcessTaskAsync(taskId, stoppingToken);
            }
            else
            {
                await Task.Delay(500, stoppingToken); // Poll every 500ms
            }
        }
    }

    private async Task ProcessTaskAsync(Guid taskId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Background: processing task {TaskId}...", taskId);

            // Simulate processing delay
            await Task.Delay(2000, ct);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var task = await db.Tasks.FindAsync(new object[] { taskId }, ct);
            if (task is null)
            {
                _logger.LogWarning("Background: task {TaskId} not found.", taskId);
                return;
            }

            // Transition: Pending → InProgress
            task.UpdateStatus(TaskItemStatus.InProgress);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Background: task {TaskId} is now InProgress.", taskId);

            // Simulate further processing
            await Task.Delay(3000, ct);

            // Transition: InProgress → Done
            task.UpdateStatus(TaskItemStatus.Done);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Background: task {TaskId} is now Done.", taskId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background: task processing cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background: error processing task {TaskId}.", taskId);
        }
    }
}
