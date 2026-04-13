namespace TaskManagement.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private User() { }

    public static User Create(string name, string email, string passwordHash, string role = "User")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void SoftDelete() => IsDeleted = true;
}
