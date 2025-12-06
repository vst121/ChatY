using ChatY.Core.Entities;

namespace ChatY.Services.Interfaces;

public interface IUserService
{
    Task<User> CreateUserAsync(string userName, string email, string? displayName = null);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUserNameAsync(string userName);
    Task UpdateUserAsync(User user);
    Task UpdateUserStatusAsync(string userId, UserStatus status);
    Task BlockUserAsync(string blockerUserId, string blockedUserId);
    Task UnblockUserAsync(string blockerUserId, string blockedUserId);
    Task<bool> IsUserBlockedAsync(string userId1, string userId2);
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, string currentUserId);
}


