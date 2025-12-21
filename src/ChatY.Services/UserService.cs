using ChatY.Core.Entities;
using ChatY.Infrastructure.Data;
using ChatY.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatY.Services;

public class UserService : IUserService
{
    private readonly ChatYDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(ChatYDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(string userName, string email, string? displayName = null)
    {
        var user = new User
        {
            UserName = userName,
            Email = email,
            DisplayName = displayName ?? userName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserStatusAsync(string userId, UserStatus status)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.UserStatus = status;
            user.IsOnline = status == UserStatus.Online;
            if (status == UserStatus.Offline)
            {
                user.LastSeen = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task BlockUserAsync(string blockerUserId, string blockedUserId)
    {
        var existingBlock = await _context.UserBlocks
            .FirstOrDefaultAsync(ub => ub.BlockerUserId == blockerUserId && ub.BlockedUserId == blockedUserId);

        if (existingBlock != null)
            return;

        var block = new UserBlock
        {
            BlockerUserId = blockerUserId,
            BlockedUserId = blockedUserId,
            BlockedAt = DateTime.UtcNow
        };

        _context.UserBlocks.Add(block);
        await _context.SaveChangesAsync();
    }

    public async Task UnblockUserAsync(string blockerUserId, string blockedUserId)
    {
        var block = await _context.UserBlocks
            .FirstOrDefaultAsync(ub => ub.BlockerUserId == blockerUserId && ub.BlockedUserId == blockedUserId);

        if (block != null)
        {
            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsUserBlockedAsync(string userId1, string userId2)
    {
        return await _context.UserBlocks
            .AnyAsync(ub => (ub.BlockerUserId == userId1 && ub.BlockedUserId == userId2) ||
                           (ub.BlockerUserId == userId2 && ub.BlockedUserId == userId1));
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, string currentUserId)
    {
        var blockedUserIds = await _context.UserBlocks
            .Where(ub => ub.BlockerUserId == currentUserId || ub.BlockedUserId == currentUserId)
            .Select(ub => ub.BlockerUserId == currentUserId ? ub.BlockedUserId : ub.BlockerUserId)
            .ToListAsync();

        return await _context.Users
            .Where(u => !blockedUserIds.Contains(u.Id) &&
                        ((u.UserName != null && u.UserName.Contains(searchTerm)) ||
                         (u.DisplayName != null && u.DisplayName.Contains(searchTerm)) ||
                         (u.Email != null && u.Email.Contains(searchTerm))))
            .Take(50)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> AuthenticateOrCreateUserAsync(string userNameOrEmail)
    {
        // Try to find existing user by username or email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail);

        if (user == null)
        {
            // Create new user
            user = new User
            {
                UserName = userNameOrEmail.Contains("@") ? null : userNameOrEmail,
                Email = userNameOrEmail.Contains("@") ? userNameOrEmail : null,
                DisplayName = userNameOrEmail.Contains("@") ? userNameOrEmail.Split('@')[0] : userNameOrEmail,
                UserStatus = UserStatus.Online,
                IsOnline = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update existing user status
            user.UserStatus = UserStatus.Online;
            user.IsOnline = true;
            user.LastSeen = null;
            await _context.SaveChangesAsync();
        }

        return user;
    }
}


