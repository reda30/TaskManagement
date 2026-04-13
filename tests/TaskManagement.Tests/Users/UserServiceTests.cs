using FluentAssertions;
using Moq;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Users;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Tests.Users;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<ITokenService> _tokenMock = new();
    private readonly IUserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepoMock.Object, _hasherMock.Object, _tokenMock.Object);
    }

    // ── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithNewEmail_ReturnsUserDto()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var request = new RegisterUserRequest("Alice", "alice@example.com", "password123");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("alice@example.com");
        result.Name.Should().Be("Alice");
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsValidationException()
    {
        // Arrange
        var existingUser = User.Create("Bob", "bob@example.com", "hash");
        _userRepoMock.Setup(r => r.GetByEmailAsync("bob@example.com", default)).ReturnsAsync(existingUser);

        var request = new RegisterUserRequest("Bob", "bob@example.com", "password123");

        // Act
        var act = async () => await _sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already registered*");
    }

    // ── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var user = User.Create("Alice", "alice@example.com", "hashed");
        _userRepoMock.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("password123", "hashed")).Returns(true);
        _tokenMock.Setup(t => t.GenerateAccessToken(user)).Returns("access_token");
        _tokenMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh_token");

        // Act
        var result = await _sut.LoginAsync(new LoginRequest("alice@example.com", "password123"));

        // Assert
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.User.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = User.Create("Alice", "alice@example.com", "hashed");
        _userRepoMock.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        // Act
        var act = async () => await _sut.LoginAsync(new LoginRequest("alice@example.com", "wrong"));

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.LoginAsync(new LoginRequest("nobody@example.com", "pass"));

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // ── GetProfile ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WithValidId_ReturnsUserDto()
    {
        // Arrange
        var user = User.Create("Alice", "alice@example.com", "hashed");
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        // Act
        var result = await _sut.GetProfileAsync(user.Id);

        // Assert
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetProfile_WithUnknownId_ThrowsNotFoundException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.GetProfileAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
