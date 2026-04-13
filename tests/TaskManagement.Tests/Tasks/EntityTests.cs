using FluentAssertions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Tasks;

public class TaskItemEntityTests
{
    [Fact]
    public void Create_SetsDefaultStatusToPending()
    {
        var task = TaskItem.Create("Title", "Desc", TaskPriority.High, Guid.NewGuid());
        task.Status.Should().Be(TaskItemStatus.Pending);
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        var task = TaskItem.Create("Title", "Desc", TaskPriority.Medium, Guid.NewGuid());
        task.UpdateStatus(TaskItemStatus.InProgress);
        task.Status.Should().Be(TaskItemStatus.InProgress);
    }

    [Fact]
    public void Create_AssignsNewGuidId()
    {
        var t1 = TaskItem.Create("A", "d", TaskPriority.Low, Guid.NewGuid());
        var t2 = TaskItem.Create("B", "d", TaskPriority.Low, Guid.NewGuid());
        t1.Id.Should().NotBe(t2.Id);
    }
}

public class UserEntityTests
{
    [Fact]
    public void Create_SetsIsDeletedFalse()
    {
        var user = User.Create("Alice", "alice@test.com", "hash");
        user.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrue()
    {
        var user = User.Create("Alice", "alice@test.com", "hash");
        user.SoftDelete();
        user.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Create_DefaultRoleIsUser()
    {
        var user = User.Create("Alice", "alice@test.com", "hash");
        user.Role.Should().Be("User");
    }
}
