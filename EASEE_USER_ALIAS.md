# Easee User Alias Feature

This document describes the Easee user alias feature that allows managing user-friendly names for charging session users.

## Overview

The feature automatically tracks all user IDs from Easee charging sessions and allows administrators to assign human-readable aliases and token ID mappings for each user.

## Features

### 1. Automatic User Tracking
- All user IDs encountered in charging sessions are automatically tracked
- Stored in `easee-users.json` file in the FileStorage path
- Duplicates are automatically removed

### 2. User Alias Management
- Assign a user-friendly alias (display name) to any user ID
- Associate multiple token IDs with a user
- Web-based interface for easy management

### 3. Smart User Name Resolution
When displaying charging sessions in reports, the system resolves user names using this priority:
1. **Direct alias match**: If the user ID has an alias, use it
2. **Token ID match**: If the session's authToken matches any user's token IDs, use that user's alias
3. **Fallback**: Use the user ID as-is if no alias is found

### 4. Updated Reports
Both monthly and detailed reports now display user aliases instead of raw user IDs.

## Usage

### Accessing the Management Page

1. Navigate to the Easee login page: `/Easee/Login`
2. Click "Benutzer-Aliase verwalten" button
3. Or directly access: `/UserAlias`

### Managing User Aliases

On the management page, you can:

1. **View all users**: See all tracked user IDs and their current aliases
2. **Edit an alias**: 
   - Click "Bearbeiten" for any user
   - Enter a friendly name (e.g., "Max Mustermann")
   - Optionally add comma-separated token IDs
   - Click "Speichern" to save
3. **Delete an alias**: Click "Löschen" to remove an alias (the user ID remains tracked)

### Token IDs

Token IDs are useful when:
- A user has multiple authentication tokens
- You want to associate different charging sessions (with different tokens) to the same user alias
- Sessions are recorded with an authToken that differs from the user ID

Format: Enter as comma-separated values (e.g., `token1, token2, token3`)

## File Storage

The feature creates two JSON files in the `FileStorage.Path`:

1. **easee-users.json**: List of all encountered user IDs
   ```json
   [
     "123",
     "456",
     "789"
   ]
   ```

2. **easee-user-aliases.json**: User alias configurations
   ```json
   [
     {
       "UserId": "123",
       "Alias": "Max Mustermann",
       "TokenIds": ["token1", "token2"]
     }
   ]
   ```

## Technical Implementation

### Components

1. **UserAlias Model** (`TgHomeBot.Charging.Contract.Models.UserAlias`)
   - Represents a user alias configuration
   - Properties: UserId, Alias, TokenIds

2. **IUserAliasService** (`TgHomeBot.Charging.Contract.IUserAliasService`)
   - Service interface for managing aliases
   - Methods: GetAllAliases, SaveAlias, DeleteAlias, ResolveUserName, TrackUserId, etc.

3. **UserAliasService** (`TgHomeBot.Charging.Easee.UserAliasService`)
   - Implementation of IUserAliasService
   - Handles file persistence and alias resolution

4. **UserAliasController** (`TgHomeBot.Api.Controllers.UserAliasController`)
   - Web controller for the management UI
   - Actions: Index (list), Edit (create/update), Delete

5. **Views**
   - `Views/UserAlias/Index.cshtml`: List all users and aliases
   - `Views/UserAlias/Edit.cshtml`: Edit form for aliases

### Integration Points

1. **EaseeConnector**: 
   - Tracks user IDs when fetching sessions
   - Resolves user names using the alias service
   - Sets the `UserName` property on `ChargingSession` objects

2. **ChargingSession Model**:
   - Extended with `UserName` property
   - Contains both `UserId` (original ID) and `UserName` (resolved display name)

3. **Report Commands**:
   - `MonthlyReportCommand`: Groups by and displays UserName
   - `DetailedReportCommand`: Groups by and displays UserName

## Security

- User input is properly HTML-encoded to prevent XSS attacks
- File writes use atomic operations (temp file + move) to prevent corruption
- Temp files use unique GUIDs to prevent race conditions
- Thread-safe access to shared data structures using locks

## Example Workflow

1. User authenticates with Easee API
2. System fetches charging sessions from API
3. For each session:
   - User ID is tracked (if not already tracked)
   - User name is resolved using alias service
   - Session is created with both UserId and UserName
4. Administrator navigates to `/UserAlias`
5. Administrator sees all tracked user IDs
6. Administrator edits user "123" and sets alias "Max Mustermann"
7. Future reports show "Max Mustermann" instead of "123"

## Future Enhancements

Potential improvements:
- Bulk import/export of aliases
- User groups or categories
- Integration with external user directories
- Automatic alias suggestions based on session metadata
