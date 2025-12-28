# TgHomeBot
Get alerts about finished household appliances on Telegram

## Features

- **Smart Home Monitoring**: Monitors Home Assistant devices and sends notifications when appliances finish their tasks
- **EV Charging Reports**: Monthly charging reports and detailed charging session history
- **Lottery Notifications**: Eurojackpot results twice weekly
- **Per-Chat Feature Flags**: Each registered chat can enable/disable specific notification types

## Feature Flags

Each registered Telegram chat has individual feature flags that can be toggled to control which notifications are received:

### Available Feature Flags

1. **Eurojackpot Notifications** - Toggle with `/toggle_eurojackpot` or API endpoint
   - Receives Eurojackpot lottery results every Tuesday and Friday at 10pm
   - Default: Enabled

2. **Monthly Charging Report** - Toggle with `/toggle_monthlyreport` or API endpoint
   - Receives monthly EV charging summary on the first of each month at midnight
   - Default: Enabled

3. **Device Notifications** - Toggle with `/toggle_devicenotifications` or API endpoint
   - Receives notifications when monitored smart home devices finish their tasks
   - Default: Enabled

### Toggle Commands

Use these Telegram bot commands to toggle feature flags:
- `/toggle_eurojackpot` - Enable/disable Eurojackpot notifications
- `/toggle_monthlyreport` - Enable/disable monthly charging reports
- `/toggle_devicenotifications` - Enable/disable device notifications

### API Endpoints

Toggle feature flags via HTTP API:
- `POST /api/notification/chat/{chatId}/flags/eurojackpot/toggle`
- `POST /api/notification/chat/{chatId}/flags/monthlyreport/toggle`
- `POST /api/notification/chat/{chatId}/flags/devicenotifications/toggle`

Each endpoint returns: `{ "enabled": true/false }`

List all registered chats with their feature flags:
- `GET /api/notification/chats`

Returns an array of chat objects with IDs and flag states.

**Note**: Manual trigger commands like `/monthlyreport` that respond directly to the requesting user bypass feature flags. However, when running scheduled tasks via `/runtask` command or API endpoints, feature flags are respected.
