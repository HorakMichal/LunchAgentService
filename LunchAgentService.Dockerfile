FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY LunchAgent.Core/LunchAgent.Core.csproj src/LunchAgent.Core/
COPY LunchAgent.Webhook/LunchAgent.Webhook.csproj src/LunchAgent.Webhook/

RUN dotnet restore src/LunchAgent.Webhook/LunchAgent.Webhook.csproj

COPY LunchAgent.Core/. src/LunchAgent.Core/
COPY LunchAgent.Webhook/. src/LunchAgent.Webhook/

RUN dotnet publish src/LunchAgent.Webhook/LunchAgent.Webhook.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "LunchAgent.Webhook.dll"]