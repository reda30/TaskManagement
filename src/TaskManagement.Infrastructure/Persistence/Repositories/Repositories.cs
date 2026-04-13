using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => await _context.Users.ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);

    public Task DeleteAsync(User user, CancellationToken ct = default)
    {
        user.SoftDelete();
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context) => _context = context;

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Tasks.Where(t => t.UserId == userId).ToListAsync(ct);

    public async Task<bool> ExistsTodayAsync(Guid userId, string title, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Tasks.AnyAsync(
            t => t.UserId == userId
              && t.Title.ToLower() == title.ToLower()
              && t.CreatedAt.Date == today,
            ct);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
        => await _context.Tasks.AddAsync(task, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
