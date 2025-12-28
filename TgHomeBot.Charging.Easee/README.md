# TgHomeBot.Charging.Easee

This project provides integration with the Easee API for electric vehicle charging stations.

## Features

- **Authentication**: Authenticate with Easee API using username and password
- **Token Management**: Automatically persists and loads authentication tokens
- **Token Refresh**: Periodic token refresh to maintain authentication
- **MVC Interface**: Web-based interface for entering credentials

## Configuration

Add the following sections to your `appsettings.json`:

```json
{
  "Application": {
    "BaseUrl": "http://localhost:5271"
  },
  "Easee": {
    "BaseUrl": "https://api.easee.com"
  },
  "FileStorage": {
    "Path": "/path/to/storage"
  }
}
```

- **Application.BaseUrl**: The base URL where your TgHomeBot application is hosted. This is used to generate login links in error messages. Use the same port as defined in `launchSettings.json` (default: 5271).
- **Easee.BaseUrl**: The Easee API endpoint (typically `https://api.easee.com`)
- **FileStorage.Path**: Directory where the authentication token will be stored

The authentication token will be stored in `{FileStorage.Path}/easee-token.json`.

## Authentication

### Web Interface

Navigate to `/Easee/Login` to access the authentication page where you can enter your Easee credentials.

**Important**: The credentials are not stored - they are only used to authenticate with the Easee API. The resulting access token and refresh token are stored persistently.

### Authentication Errors

When using the Telegram bot commands (e.g., `/monthlyreport`, `/detailedreport`) without valid authentication, the bot will respond with an error message containing a clickable link to the login page:

```
❌ Fehler beim Abrufen der Ladevorgänge:
Nicht mit Easee API authentifiziert. Bitte anmelden
```

The word "anmelden" appears as a clickable link that opens the login page directly in the browser.

This makes it easy to authenticate directly from the Telegram error message.

### Programmatic Authentication

You can also authenticate programmatically using the `IChargingConnector` service:

```csharp
var success = await chargingConnector.AuthenticateAsync(username, password);
```

## Token Refresh

To enable automatic token refresh, create a task configuration file in your FileStorage path:

**File**: `{FileStorage.Path}/ScheduledTasks/EaseeTokenRefreshTask.json`

```json
{
  "taskType": "EaseeTokenRefreshTask",
  "cronExpression": "*/30 * * * *",
  "enabled": true
}
```

This configuration runs the refresh task every 30 minutes. The cron expression `*/30 * * * *` means:
- `*/30` - Every 30 minutes
- `*` - Every hour
- `*` - Every day
- `*` - Every month
- `*` - Every day of the week

### Cron Expression Examples

- `*/30 * * * *` - Every 30 minutes (recommended)
- `*/15 * * * *` - Every 15 minutes (more frequent)
- `0 * * * *` - Every hour at minute 0

## API Documentation

The Easee API documentation is available at: https://developer.easee.com/

### Authentication Endpoint

- **URL**: `POST /api/accounts/login`
- **Body**: `{ "userName": "...", "password": "..." }`
- **Response**: Contains `accessToken`, `refreshToken`, `expiresIn`, and `tokenType`

### Token Refresh Endpoint

- **URL**: `POST /api/accounts/refresh_token`
- **Body**: `{ "accessToken": "", "refreshToken": "..." }`
- **Response**: New `accessToken` and `refreshToken`

## Integration

The Easee integration is automatically registered in the API project's `Program.cs`:

```csharp
builder.Services.AddEasee(builder.Configuration);
```

This registers:
- `IChargingConnector` as a singleton
- Configuration options from the `Easee` section
- HTTP client factory for API calls

## Security Notes

- Credentials entered via the web interface are not persisted
- Only authentication tokens are stored in the file system
- The token file should be protected with appropriate file permissions
- Access tokens expire after 1 hour and are automatically refreshed
