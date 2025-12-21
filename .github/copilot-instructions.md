# GitHub Copilot Instructions for TgHomeBot

## Project Overview

TgHomeBot is a .NET 8.0 application that monitors smart home devices (primarily Home Assistant) and sends notifications via Telegram when household appliances finish their tasks (e.g., washing machine, dishwasher).

## Architecture

### Project Structure

The solution follows a **modular, multi-project architecture** organized into functional domains:

- **TgHomeBot.Api** - Main ASP.NET Core Web API application (entry point)
- **SmartHome** domain:
  - **TgHomeBot.SmartHome.Contract** - Contracts/interfaces for smart home integration
  - **TgHomeBot.SmartHome.HomeAssistant** - Home Assistant-specific implementation
- **Notifications** domain:
  - **TgHomeBot.Notifications.Contract** - Contracts/interfaces for notifications
  - **TgHomeBot.Notifications.Telegram** - Telegram bot implementation
- **TgHomeBot.Scheduling** - Task scheduling functionality
- **TgHomeBot.Common.Contract** - Shared contracts and utilities

### Design Patterns

1. **CQRS with MediatR**
   - Use MediatR `IRequest<TResponse>` for queries and commands
   - Place request classes in `Requests/` folders within Contract projects
   - Implement handlers in `RequestHandlers/` folders within implementation projects
   - Example: `GetDevicesRequest` → `GetDevicesRequestHandler`

2. **Dependency Injection**
   - Use constructor injection with primary constructors (C# 12)
   - Register services via extension methods in `Bootstrap.cs` files
   - Use `IOptions<T>` pattern for configuration
   - Prefer interface-based abstractions over concrete implementations

3. **Contract-Implementation Separation**
   - Define interfaces and models in `.Contract` projects
   - Implement functionality in separate implementation projects
   - Internal implementations, public contracts

4. **Repository/Connector Pattern**
   - Use `ISmartHomeConnector` for smart home device access
   - Use `INotificationConnector` for notification delivery
   - Implement `ISmartHomeMonitor` for device state monitoring

5. **Command Pattern**
   - Telegram commands implement `ICommand` interface
   - Commands are registered as singletons in DI container
   - Each command has `Name`, `Description`, and `ProcessMessage` method

## Coding Conventions

### C# Style

- **Target Framework**: .NET 8.0
- **Language Version**: C# 12 with latest features
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)

### Code Patterns

1. **Primary Constructors**
   ```csharp
   internal class StartCommand(IOptions<TelegramOptions> options, IRegisteredChatService registeredChatService) : ICommand
   ```

2. **Required Properties**
   ```csharp
   public required string Id { get; set; }
   public required string Name { get; set; }
   ```

3. **Init-Only Properties**
   ```csharp
   public string Type { get; init; }
   ```

4. **File-Scoped Namespaces**
   ```csharp
   namespace TgHomeBot.SmartHome.Contract;
   
   public class MyClass { }
   ```

5. **Pattern Matching and Null-Coalescing**
   ```csharp
   if (message.From?.Username is null) { return; }
   return _smartHomeMonitor?.State ?? MonitorState.Unknown;
   ```

6. **Collection Expressions and LINQ**
   ```csharp
   return devices.Select(d => ConvertDevice(d)).ToArray();
   ```

### Naming Conventions

- **Projects**: `TgHomeBot.<Domain>.<Technology>` (e.g., `TgHomeBot.SmartHome.HomeAssistant`)
- **Contracts**: `TgHomeBot.<Domain>.Contract`
- **Interfaces**: Prefix with `I` (e.g., `ISmartHomeConnector`, `ICommand`)
- **Internal Classes**: Mark implementation classes as `internal` when they implement public interfaces
- **Request/Response**: Suffix with `Request` or `Response` (e.g., `GetDevicesRequest`)
- **Handlers**: Suffix with `Handler` (e.g., `GetDevicesRequestHandler`)
- **Options**: Suffix configuration classes with `Options` (e.g., `HomeAssistantOptions`, `TelegramOptions`)

### Dependency Registration

Use extension methods in `Bootstrap.cs` files:

```csharp
public static class Bootstrap
{
    public static IServiceCollection AddHomeAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HomeAssistantOptions>().Configure(options => configuration.GetSection("HomeAssistant").Bind(options));
        services.AddSingleton<ISmartHomeConnector, HomeAssistantConnector>();
        services.AddTransient<IRequestHandler<GetDevicesRequest, IReadOnlyList<SmartDevice>>, GetDevicesRequestHandler>();
        return services;
    }
}
```

### Configuration

- Use `IOptions<T>` pattern for strongly-typed configuration
- Configuration sections match the domain (e.g., `HomeAssistant`, `Telegram`, `SmartHome`)
- Store sensitive data externally (marked as `<set externally>` in appsettings.json)
- Use user secrets for development, environment variables for production

### Logging

- Use **Serilog** for structured logging
- Inject `ILogger<T>` via constructor
- Use structured logging with message templates
- Configure via `appsettings.json` with `Serilog` section

### Package Management

- **Central Package Management**: All package versions defined in `Directory.Packages.props`
- Use `<PackageReference>` without version attributes in project files
- Key dependencies:
  - **MediatR** (12.2.0) - CQRS pattern
  - **Telegram.Bot** (19.0.0) - Telegram integration
  - **Serilog.AspNetCore** (8.0.1) - Structured logging
  - **Cronos** (0.8.4) - Scheduling

## Best Practices

1. **Async/Await**
   - Use `async`/`await` consistently
   - Cancel operations with `CancellationToken`
   - Use `SafeFireAndForget` from `AsyncAwaitBestPractices` for fire-and-forget tasks with error handling

2. **Error Handling**
   - Log exceptions with structured logging
   - Use `try-catch` for expected exceptions
   - Return appropriate HTTP status codes in controllers

3. **Hosted Services**
   - Implement `IHostedService` for background tasks
   - Register as singleton
   - Start async operations without blocking startup using `SafeFireAndForget`

4. **WebSocket Communication**
   - Used for Home Assistant real-time monitoring
   - Implement message types as records/classes with `IMessage` interface
   - Use JSON serialization with `JsonPropertyName` attributes

5. **Controllers**
   - Use `[Route("api/[controller]")]` attribute routing
   - Inject `IMediator` for CQRS operations
   - Return `ActionResult<T>` for type-safe responses
   - Use `[FromServices]` for additional dependencies

## Development Workflow

1. **Build**: `dotnet build TgHomeBot.sln`
2. **Run**: Navigate to `TgHomeBot.Api` and run `dotnet run`
3. **Docker**: Use the provided `Dockerfile` for containerization
4. **Swagger**: Available in development mode for API exploration

## Comments

- **Minimal Comments**: Code should be self-documenting through clear naming
- **When to Comment**:
  - Complex business logic
  - Non-obvious workarounds
  - Public API documentation (if needed)
- **XML Documentation**: Not extensively used; prefer clear code over comments

## Language

- **Code and Comments**: English
- **User-Facing Messages**: German (Telegram bot messages are in German)
- Example: `"Willkommen zu TgHomeBot. Du kannst die Verbindung mit /end trennen."`
