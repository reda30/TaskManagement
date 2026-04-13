namespace TaskManagement.Application.Users.DTOs;

public record RegisterUserRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, string RefreshToken, UserDto User);

public record UserDto(Guid Id, string Name, string Email, string Role, DateTime CreatedAt);

public record CreateUserByAdminRequest(string Name, string Email, string Password, string Role = "User");
