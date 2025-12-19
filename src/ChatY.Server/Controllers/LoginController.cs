using ChatY.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatY.Server.Controllers;

[Route("api/[controller]")]
public class LoginController : Controller
{
    private readonly IUserService _userService;

    public LoginController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromForm] string userNameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(userNameOrEmail))
        {
            return Redirect("/login?error=Please enter a username or email.");
        }

        try
        {
            // Authenticate or create user
            var user = await _userService.AuthenticateOrCreateUserAsync(userNameOrEmail.Trim());

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.UserName ?? user.Email ?? "User"),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("UserId", user.Id)
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user
            await HttpContext.SignInAsync("Cookies", principal);

            // Redirect to chats page
            return Redirect("/chats");
        }
        catch (Exception ex)
        {
            return Redirect($"/login?error={Uri.EscapeDataString($"An error occurred: {ex.Message}")}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // Get current user and set status to offline
        var userId = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _userService.UpdateUserStatusAsync(userId, ChatY.Core.Entities.UserStatus.Offline);
        }

        // Sign out
        await HttpContext.SignOutAsync("Cookies");

        // Redirect to login page
        return Redirect("/login");
    }
}