using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Users;
using TaskManagement.Application.Users.DTOs;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminUsersController(IUserService userService) => _userService = userService;

    /// <summary>Get all registered users (Admin only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _userService.GetAllUsersAsync(ct);
        return Ok(users);
    }

    /// <summary>Create a new user (Admin only).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserByAdminRequest request, CancellationToken ct)
    {
        var user = await _userService.CreateUserByAdminAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), user);
    }

    /// <summary>Soft-delete a user (Admin only).</summary>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        await _userService.DeleteUserAsync(userId, ct);
        return NoContent();
    }
}
