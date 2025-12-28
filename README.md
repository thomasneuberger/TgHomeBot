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

1. **Eurojackpot Notifications** - Toggle with `/toggleeurojackpot` or API endpoint
   - Receives Eurojackpot lottery results every Tuesday and Friday at 10pm
   - Default: Enabled

2. **Monthly Charging Report** - Toggle with `/togglemonthlyreport` or API endpoint
   - Receives monthly EV charging summary on the first of each month at midnight
   - Default: Enabled

3. **Device Notifications** - Toggle with `/toggledevicenotifications` or API endpoint
   - Receives notifications when monitored smart home devices finish their tasks
   - Default: Enabled

### Toggle Commands

Use these Telegram bot commands to toggle feature flags:
- `/toggleeurojackpot` - Enable/disable Eurojackpot notifications
- `/togglemonthlyreport` - Enable/disable monthly charging reports
- `/toggledevicenotifications` - Enable/disable device notifications

### API Endpoints

Toggle feature flags via HTTP API:
- `POST /api/notification/chat/{chatId}/flags/eurojackpot/toggle`
- `POST /api/notification/chat/{chatId}/flags/monthlyreport/toggle`
- `POST /api/notification/chat/{chatId}/flags/devicenotifications/toggle`

Each endpoint returns: `{ "enabled": true/false }`

**Note**: Manual trigger commands like `/monthlyreport` that respond directly to the requesting user bypass feature flags. However, when running scheduled tasks via `/runtask` command or API endpoints, feature flags are respected.
