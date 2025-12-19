using ChatY.Core.Entities;
using ChatY.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ChatY.Services;

public class AuthenticationStateService
{
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationStateService(IUserService userService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }
        return await _userService.GetUserByIdAsync(userId);
    }

    public string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst("UserId")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<string?> GetCurrentUserNameAsync()
    {
        var currentUser = await GetCurrentUserAsync();
        return currentUser?.DisplayName ?? currentUser?.UserName;
    }
}