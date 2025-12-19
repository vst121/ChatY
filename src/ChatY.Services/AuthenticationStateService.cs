using ChatY.Core.Entities;
using ChatY.Services.Interfaces;
using System.Security.Claims;

namespace ChatY.Services;

public class AuthenticationStateService
{
    private readonly IUserService _userService;

    public AuthenticationStateService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal? user = null)
    {
        var userId = GetCurrentUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }
        return await _userService.GetUserByIdAsync(userId);
    }

    public string? GetCurrentUserId(ClaimsPrincipal? user = null)
    {
        return user?.FindFirst("UserId")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<string?> GetCurrentUserNameAsync(ClaimsPrincipal? user = null)
    {
        var currentUser = await GetCurrentUserAsync(user);
        return currentUser?.DisplayName ?? currentUser?.UserName;
    }
}