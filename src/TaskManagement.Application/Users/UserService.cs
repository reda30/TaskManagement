using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Users;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserDto> CreateUserByAdminAsync(CreateUserByAdminRequest request, CancellationToken ct = default);
    Task DeleteUserAsync(Guid userId, CancellationToken ct = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<UserDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new ValidationException($"Email '{request.Email}' is already registered.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Name, request.Email, hash, "User");

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        return MapToDto(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new LoginResponse(accessToken, refreshToken, MapToDto(user));
    }

    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);
        return MapToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto> CreateUserByAdminAsync(CreateUserByAdminRequest request, CancellationToken ct = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new ValidationException($"Email '{request.Email}' is already registered.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Name, request.Email, hash, request.Role);

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        return MapToDto(user);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);
        await _userRepository.DeleteAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);
    }

    private static UserDto MapToDto(User u) =>
        new(u.Id, u.Name, u.Email, u.Role, u.CreatedAt);
}
