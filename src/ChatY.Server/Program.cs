using ChatY.Core.Entities;
using ChatY.Infrastructure;
using ChatY.Infrastructure.Data;
using ChatY.Server.Hubs;
using ChatY.Services;
using ChatY.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

// Database
builder.Services.AddDbContext<ChatYDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserService, UserService>();

// Azure Services
builder.Services.AddScoped<ChatY.Infrastructure.Services.IAzureBlobStorageService, ChatY.Infrastructure.Services.AzureBlobStorageService>();
builder.Services.AddScoped<ChatY.Infrastructure.Services.IAzureKeyVaultService, ChatY.Infrastructure.Services.AzureKeyVaultService>();

// Authentication (simplified for now - can be extended with Identity)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<ChatHub>("/chathub");
app.MapFallbackToPage("/_Host");

// Ensure database is created and seed data (for development)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatYDbContext>();
    dbContext.Database.EnsureCreated();

    // Seed test data
    if (!dbContext.Users.Any())
    {
        var user1 = new User
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            UserStatus = UserStatus.Online,
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = "user2",
            UserName = "testuser2",
            DisplayName = "Test User 2",
            Email = "test2@example.com",
            UserStatus = UserStatus.Online,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.AddRange(user1, user2);
        dbContext.SaveChanges();

        var chat = new Chat
        {
            Id = "chat1",
            Name = "Test Chat",
            Type = ChatType.Private,
            CreatedByUserId = "user1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Chats.Add(chat);
        dbContext.SaveChanges();

        dbContext.ChatParticipants.AddRange(
            new ChatParticipant
            {
                Id = "part1",
                ChatId = "chat1",
                UserId = "user1",
                Role = ParticipantRole.Owner,
                JoinedAt = DateTime.UtcNow
            },
            new ChatParticipant
            {
                Id = "part2",
                ChatId = "chat1",
                UserId = "user2",
                Role = ParticipantRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        );
        dbContext.SaveChanges();
    }
}

app.Run();


