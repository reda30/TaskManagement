using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Users;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.BackgroundJobs;
using TaskManagement.Infrastructure.Caching;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using TaskManagement.Infrastructure.Seeding;
using TaskManagement.Infrastructure.Services;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();

        // Application services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITaskService, TaskService>();

        // Infrastructure services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICacheService, RedisCacheService>();

        // Redis
        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration = configuration.GetConnectionString("Redis");
            opts.InstanceName = "TaskMgmt:";
        });

        // Background processing
        services.AddSingleton<TaskProcessingQueue>();
        services.AddSingleton<ITaskQueueService>(sp => sp.GetRequiredService<TaskProcessingQueue>());
        services.AddHostedService<TaskProcessingWorker>();

        // Seeder
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
