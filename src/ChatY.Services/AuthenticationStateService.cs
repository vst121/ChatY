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
        // For now, return the test user. In a real app, this would get the user from claims
        // TODO: Implement proper authentication with claims
        return await _userService.GetUserByIdAsync("user1");
    }

    public string? GetCurrentUserId(ClaimsPrincipal? user = null)
    {
        // For now, return the test user ID. In a real app, this would get the user ID from claims
        // TODO: Implement proper authentication with claims
        return "user1";
    }

    public async Task<string?> GetCurrentUserNameAsync(ClaimsPrincipal? user = null)
    {
        var currentUser = await GetCurrentUserAsync(user);
        return currentUser?.DisplayName ?? currentUser?.UserName;
    }
}