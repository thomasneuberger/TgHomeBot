# GitHub Copilot Instructions for [PROJECT_NAME]

## Project Overview

[PROJECT_NAME] is a .NET 8.0 application that [PROJECT_DESCRIPTION].

## Architecture

### Project Structure

The solution follows a **modular, multi-project architecture** organized into functional domains:

- **[PROJECT_NAME].Api** - Main ASP.NET Core Web API application (entry point)
- **[Domain1]** domain:
  - **[PROJECT_NAME].[Domain1].Contract** - Contracts/interfaces for [domain1 purpose]
  - **[PROJECT_NAME].[Domain1].[Technology]** - [Technology]-specific implementation
- **[Domain2]** domain:
  - **[PROJECT_NAME].[Domain2].Contract** - Contracts/interfaces for [domain2 purpose]
  - **[PROJECT_NAME].[Domain2].[Technology]** - [Technology] implementation
- **[PROJECT_NAME].[SharedDomain]** - [Shared functionality description]
- **[PROJECT_NAME].Common.Contract** - Shared contracts and utilities

### Design Patterns

1. **CQRS with MediatR**
   - Use MediatR `IRequest<TResponse>` for queries and commands
   - Place request classes in `Requests/` folders within Contract projects
   - Implement handlers in `RequestHandlers/` folders within implementation projects
   - Example: `Get[Entity]Request` → `Get[Entity]RequestHandler`

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
   - Use `I[Domain]Connector` for external system access
   - Use `I[Domain]Service` for business logic
   - Implement `I[Domain]Monitor` for state monitoring when applicable

5. **Command Pattern**
   - Commands implement `ICommand` interface
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
   internal class [ClassName]([Dependency1] dependency1, [Dependency2] dependency2) : [IInterface]
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
   namespace [PROJECT_NAME].[Domain].Contract;
   
   public class MyClass { }
   ```

5. **Pattern Matching and Null-Coalescing**
   ```csharp
   if (message.From?.Username is null) { return; }
   return _monitor?.State ?? MonitorState.Unknown;
   ```

6. **Collection Expressions and LINQ**
   ```csharp
   return items.Select(item => ConvertItem(item)).ToArray();
   ```

### Naming Conventions

- **Projects**: `[PROJECT_NAME].<Domain>.<Technology>` (e.g., `[PROJECT_NAME].[Domain].[Technology]`)
- **Contracts**: `[PROJECT_NAME].<Domain>.Contract`
- **Interfaces**: Prefix with `I` (e.g., `I[Domain]Connector`, `ICommand`)
- **Internal Classes**: Mark implementation classes as `internal` when they implement public interfaces
- **Request/Response**: Suffix with `Request` or `Response` (e.g., `Get[Entity]Request`)
- **Handlers**: Suffix with `Handler` (e.g., `Get[Entity]RequestHandler`)
- **Options**: Suffix configuration classes with `Options` (e.g., `[Domain]Options`, `[Service]Options`)

### Dependency Registration

Use extension methods in `Bootstrap.cs` files:

```csharp
public static class Bootstrap
{
    public static IServiceCollection Add[Domain](this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<[Domain]Options>().Configure(options => configuration.GetSection("[DomainConfigSection]").Bind(options));
        services.AddSingleton<I[Domain]Connector, [Domain]Connector>();
        services.AddTransient<IRequestHandler<Get[Entity]Request, IReadOnlyList<[Entity]>>, Get[Entity]RequestHandler>();
        return services;
    }
}
```

### Configuration

- Use `IOptions<T>` pattern for strongly-typed configuration
- Configuration sections match the domain (e.g., `[Domain1]`, `[Domain2]`, `[Domain3]`)
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
- **MIT License Requirement**: Only use external packages with MIT license to ensure compliance and compatibility
- Key dependencies:
  - **MediatR** - CQRS pattern
  - **Serilog.AspNetCore** - Structured logging
  - [Add project-specific dependencies here]

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

4. **WebSocket Communication** (if applicable)
   - Implement message types as records/classes with `IMessage` interface
   - Use JSON serialization with `JsonPropertyName` attributes

5. **Controllers**
   - Use `[Route("api/[controller]")]` attribute routing
   - Inject `IMediator` for CQRS operations
   - Return `ActionResult<T>` for type-safe responses
   - Use `[FromServices]` for additional dependencies

6. **Feature Flags** (if applicable)
   - **All new features that require opt-in/opt-out functionality must include a corresponding feature flag**
   - Add boolean properties with default values
   - Create toggle methods in appropriate service interfaces
   - Implement toggle commands and API endpoints
   - Use feature flags to control feature availability per user/context

## Development Workflow

1. **Build**: `dotnet build [PROJECT_NAME].sln`
2. **Run**: Navigate to `[PROJECT_NAME].Api` and run `dotnet run`
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
- **User-Facing Messages**: [Language for user-facing content, e.g., German, English, etc.]
- Example: `"[Example user-facing message in the target language]"`

---

## Personal GitHub Copilot Instructions

This section contains personal preferences and instructions for working with GitHub Copilot on this project.

### Code Generation Preferences

- **Always use the latest C# features**: Leverage C# 12 features including primary constructors, collection expressions, and pattern matching
- **Prefer immutability**: Use `init` over `set` when possible, use readonly fields
- **Be explicit with nullability**: Always handle null cases explicitly, avoid null-forgiving operator unless absolutely necessary
- **Keep it DRY**: Don't repeat yourself - extract common patterns into reusable methods or classes
- **Single Responsibility**: Each class/method should have one clear purpose

### Testing Preferences

- **Test naming**: Use descriptive test method names that explain the scenario and expected outcome
- **Arrange-Act-Assert**: Follow AAA pattern in unit tests
- **Test one thing**: Each test should verify a single behavior
- **Use meaningful test data**: Avoid magic numbers/strings, use descriptive constants or variables

### Documentation Preferences

- **Self-documenting code first**: Write clear code that doesn't need comments
- **Update documentation**: When changing functionality, update related documentation
- **Keep it current**: Remove outdated comments rather than leaving them in

### Review and Quality

- **Security first**: Always consider security implications (input validation, sanitization, authentication)
- **Performance awareness**: Be mindful of performance implications (N+1 queries, unnecessary allocations)
- **Error handling**: Don't swallow exceptions, log appropriately, fail fast when appropriate
- **Backwards compatibility**: Consider impact on existing functionality when making changes

### Communication Style

- **Clear commit messages**: Use descriptive commit messages following conventional commits format
- **Explain why, not what**: In comments and documentation, explain the reasoning, not just the implementation
- **Ask when uncertain**: If requirements are unclear, ask for clarification rather than making assumptions
