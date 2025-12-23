FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["TgHomeBot.Api/TgHomeBot.Api.csproj", "TgHomeBot.Api/"]
COPY ["TgHomeBot.SmartHome.Contract/TgHomeBot.SmartHome.Contract.csproj", "TgHomeBot.SmartHome.Contract/"]
COPY ["TgHomeBot.SmartHome.HomeAssistant/TgHomeBot.SmartHome.HomeAssistant.csproj", "TgHomeBot.SmartHome.HomeAssistant/"]
COPY ["TgHomeBot.Notifications.Contract/TgHomeBot.Notifications.Contract.csproj", "TgHomeBot.Notifications.Contract/"]
COPY ["TgHomeBot.Notifications.Telegram/TgHomeBot.Notifications.Telegram.csproj", "TgHomeBot.Notifications.Telegram/"]
COPY ["TgHomeBot.Common.Contract/TgHomeBot.Common.Contract.csproj", "TgHomeBot.Common.Contract/"]
COPY ["TgHomeBot.Scheduling/TgHomeBot.Scheduling.csproj", "TgHomeBot.Scheduling/"]
COPY ["TgHomeBot.Scheduling.Contract/TgHomeBot.Scheduling.Contract.csproj", "TgHomeBot.Scheduling.Contract/"]
COPY ["TgHomeBot.Charging.Contract/TgHomeBot.Charging.Contract.csproj", "TgHomeBot.Charging.Contract/"]
COPY ["TgHomeBot.Charging.Easee/TgHomeBot.Charging.Easee.csproj", "TgHomeBot.Charging.Easee/"]
RUN dotnet restore "TgHomeBot.Api/TgHomeBot.Api.csproj"
COPY . .
WORKDIR "/src/TgHomeBot.Api"
RUN dotnet build "TgHomeBot.Api.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TgHomeBot.Api.csproj" -c $BUILD_CONFIGURATION --no-build --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TgHomeBot.Api.dll"]
