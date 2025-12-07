# Developer Guide

This guide provides comprehensive information for developers working on the ChatY project. It covers development setup, coding standards, architecture guidelines, testing practices, and contribution workflows.

## Development Environment Setup

### Prerequisites

- **Operating System**: Windows 10/11, macOS, or Linux
- **.NET 10 SDK**: Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
- **IDE**: Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- **Database**: SQL Server 2022, SQL Server Express, or LocalDB
- **Git**: Version control system

### Optional Tools

- **Azure CLI**: For Azure service development
- **Docker**: For containerized development
- **Node.js**: For frontend tooling (if needed)
- **Postman**: For API testing

### Getting Started

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/your-organization/ChatY.git
   cd ChatY
   ```

2. **Restore Dependencies**:

   ```bash
   dotnet restore
   ```

3. **Database Setup**:

   ```bash
   # Create database (LocalDB example)
   dotnet ef database update --project src/ChatY.Infrastructure --startup-project src/ChatY.Server
   ```

4. **Run the Application**:

   ```bash
   dotnet run --project src/ChatY.Server
   ```

5. **Access the Application**:
   - Open browser to `https://localhost:5001`
   - Default development credentials may be configured in appsettings

## Project Structure

### Solution Overview

```
ChatY.sln
├── src/
│   ├── ChatY.Core/           # Domain layer
│   │   ├── Entities/         # Domain entities
│   │   └── ChatY.Core.csproj
│   ├── ChatY.Infrastructure/ # Infrastructure layer
│   │   ├── Data/            # EF Core context and configurations
│   │   ├── Migrations/      # Database migrations
│   │   ├── Repositories/    # Repository implementations
│   │   └── Services/        # External service integrations
│   ├── ChatY.Services/      # Application layer
│   │   ├── Interfaces/      # Service contracts
│   │   └── Services/        # Business logic implementations
│   ├── ChatY.Server/        # Presentation layer
│   │   ├── Components/      # Blazor components
│   │   ├── Hubs/           # SignalR hubs
│   │   ├── Pages/          # Razor pages
│   │   └── wwwroot/        # Static assets
│   └── ChatY.Shared/        # Shared models and DTOs
├── docs/                    # Documentation
├── tests/                   # Unit and integration tests
└── tools/                   # Development tools and scripts
```

### Architecture Principles

ChatY follows Clean Architecture (Onion Architecture) principles:

- **Dependency Direction**: Outer layers depend on inner layers
- **Domain Independence**: Core business logic has no external dependencies
- **Testability**: Each layer can be tested in isolation
- **Separation of Concerns**: Clear boundaries between layers

## Coding Standards

### C# Coding Conventions

- Follow [Microsoft C# Coding Guidelines](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-class/coding-conventions)
- Use meaningful, descriptive names for classes, methods, and variables
- Use PascalCase for public members, camelCase for private fields
- Use async/await for asynchronous operations
- Prefer LINQ over loops when appropriate

### Naming Conventions

```csharp
// Classes and Interfaces
public class ChatService : IChatService
public interface IUserRepository

// Methods
public async Task<Message> SendMessageAsync(MessageDto messageDto)
public IEnumerable<Chat> GetUserChats(Guid userId)

// Properties
public string DisplayName { get; set; }
public DateTime CreatedAt { get; set; }

// Private fields
private readonly IChatRepository _chatRepository;
private readonly ILogger<ChatService> _logger;
```

### Code Organization

- Group related functionality in regions or partial classes
- Keep methods small and focused (Single Responsibility Principle)
- Use dependency injection for service dependencies
- Implement proper error handling and logging

### Example Code Structure

```csharp
public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatRepository chatRepository,
        IUserRepository userRepository,
        ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Chat> CreateChatAsync(CreateChatDto createChatDto)
    {
        // Validation
        if (createChatDto.Participants.Count < 2)
            throw new ValidationException("Chat must have at least 2 participants");

        // Business logic
        var chat = new Chat
        {
            Name = createChatDto.Name,
            Type = createChatDto.Type,
            CreatedAt = DateTime.UtcNow
        };

        // Persistence
        await _chatRepository.AddAsync(chat);

        _logger.LogInformation("Created new chat {ChatId} with {ParticipantCount} participants",
            chat.Id, createChatDto.Participants.Count);

        return chat;
    }
}
```

## Database Development

### Entity Framework Core

- Use Code-First approach with migrations
- Define entities in the Core layer
- Configure relationships and constraints in the Infrastructure layer

### Migration Workflow

```bash
# Create migration
dotnet ef migrations add MigrationName --project src/ChatY.Infrastructure --startup-project src/ChatY.Server

# Update database
dotnet ef database update --project src/ChatY.Infrastructure --startup-project src/ChatY.Server

# Generate SQL script
dotnet ef migrations script --project src/ChatY.Infrastructure --startup-project src/ChatY.Server
```

### Database Best Practices

- Use appropriate data types and constraints
- Index frequently queried columns
- Use navigation properties for relationships
- Implement soft deletes where appropriate
- Use database views for complex queries

## API Development

### SignalR Hubs

ChatY uses SignalR for real-time communication. Hub methods should:

- Be asynchronous
- Validate input parameters
- Handle errors gracefully
- Log important operations

```csharp
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task SendMessage(SendMessageDto messageDto)
    {
        try
        {
            var message = await _chatService.SendMessageAsync(messageDto, Context.UserIdentifier);

            await Clients.Group(message.ChatId.ToString())
                .SendAsync("ReceiveMessage", message);

            _logger.LogInformation("Message sent in chat {ChatId} by user {UserId}",
                message.ChatId, Context.UserIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }
}
```

### RESTful APIs (Future)

If REST APIs are added:

- Use attribute routing
- Implement proper HTTP status codes
- Use DTOs for request/response models
- Implement API versioning
- Add comprehensive documentation with Swagger/OpenAPI

## Testing

### Unit Testing

- Test business logic in isolation
- Mock external dependencies
- Use xUnit or NUnit testing frameworks
- Follow AAA pattern (Arrange, Act, Assert)

```csharp
public class ChatServiceTests
{
    [Fact]
    public async Task CreateChatAsync_WithValidData_CreatesChat()
    {
        // Arrange
        var mockRepository = new Mock<IChatRepository>();
        var mockUserRepo = new Mock<IUserRepository>();
        var service = new ChatService(mockRepository.Object, mockUserRepo.Object, null);

        var createChatDto = new CreateChatDto
        {
            Name = "Test Chat",
            Participants = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        var result = await service.CreateChatAsync(createChatDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Chat", result.Name);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Chat>()), Times.Once);
    }
}
```

### Integration Testing

- Test complete workflows
- Use test database (in-memory or local)
- Test SignalR hub methods
- Verify database operations

### Test Organization

```
tests/
├── ChatY.Core.Tests/
├── ChatY.Services.Tests/
├── ChatY.Infrastructure.Tests/
└── ChatY.Server.Tests/
```

## Frontend Development (Blazor)

### Component Structure

- Use Razor components for UI
- Implement component logic in code-behind files
- Use dependency injection for services
- Handle component lifecycle properly

### State Management

- Use built-in Blazor state management for simple scenarios
- Consider Fluxor for complex state management
- Implement proper event handling

### Example Component

```razor
@page "/chat/{chatId:guid}"
@inject IChatService ChatService
@inject NavigationManager Navigation

<div class="chat-container">
    <div class="messages" @ref="messagesContainer">
        @foreach (var message in messages)
        {
            <MessageItem Message="message" />
        }
    </div>

    <div class="message-input">
        <input @bind="newMessage" @onkeydown="HandleKeyDown" />
        <button @onclick="SendMessage">Send</button>
    </div>
</div>

@code {
    [Parameter] public Guid ChatId { get; set; }

    private List<Message> messages = new();
    private string newMessage = "";
    private ElementReference messagesContainer;

    protected override async Task OnInitializedAsync()
    {
        messages = await ChatService.GetMessagesAsync(ChatId);
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(newMessage)) return;

        var message = await ChatService.SendMessageAsync(ChatId, newMessage);
        messages.Add(message);
        newMessage = "";

        await ScrollToBottom();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
        }
    }

    private async Task ScrollToBottom()
    {
        await messagesContainer.ScrollIntoViewAsync(ScrollIntoViewPosition.End);
    }
}
```

## Security Considerations

### Authentication & Authorization

- Implement proper authentication mechanisms
- Use role-based authorization
- Validate user permissions
- Protect against common vulnerabilities (XSS, CSRF, etc.)

### Data Protection

- Encrypt sensitive data
- Use secure connection strings
- Implement proper session management
- Follow OWASP guidelines

## Performance Optimization

### Database Optimization

- Use efficient queries with proper indexing
- Implement caching where appropriate
- Use asynchronous operations
- Monitor query performance

### Application Performance

- Minimize bundle size for Blazor WebAssembly (if used)
- Implement lazy loading for components
- Use virtualization for large lists
- Optimize SignalR connection management

## Debugging and Troubleshooting

### Common Issues

1. **Database Connection Issues**:

   - Verify connection string
   - Check firewall settings
   - Ensure SQL Server is running

2. **SignalR Connection Problems**:

   - Check CORS configuration
   - Verify WebSocket support
   - Monitor network connectivity

3. **Build Errors**:
   - Clean and rebuild solution
   - Check NuGet package versions
   - Verify .NET SDK version

### Logging

- Use structured logging with Serilog or Microsoft.Extensions.Logging
- Log important business operations
- Include correlation IDs for request tracing
- Configure appropriate log levels

```csharp
_logger.LogInformation("User {UserId} created chat {ChatId}", userId, chatId);
_logger.LogError(ex, "Failed to send message in chat {ChatId}", chatId);
```

## Deployment

### Development Deployment

- Use `dotnet run` for local development
- Configure appsettings.Development.json
- Use LocalDB or SQL Server Express

### Production Deployment

- Follow the deployment guide in `docs/deployment.md`
- Use environment-specific configuration
- Implement proper monitoring and logging
- Set up automated deployments

## Contributing

### Pull Request Process

1. Create a feature branch from `main`
2. Implement changes with tests
3. Ensure all tests pass
4. Update documentation if needed
5. Submit pull request with clear description
6. Address review feedback

### Code Review Guidelines

- Review for code quality and standards
- Check test coverage
- Verify documentation updates
- Ensure proper error handling
- Validate performance implications

## Resources

### Documentation Links

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)

### Tools and Extensions

- [ReSharper](https://www.jetbrains.com/resharper/) - Code analysis and refactoring
- [SonarQube](https://www.sonarsource.com/products/sonarqube/) - Code quality analysis
- [Postman](https://www.postman.com/) - API testing
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) - Storage development

## Support

- **Issues**: Report bugs and request features on GitHub
- **Discussions**: Ask questions and share ideas
- **Documentation**: Check this guide and related docs
- **Team**: Contact the development team for urgent issues

Remember to keep this guide updated as the project evolves. Happy coding!
