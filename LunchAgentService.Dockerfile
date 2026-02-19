FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY LunchAgent.Core/LunchAgent.Core.csproj LunchAgent.Core/
COPY LunchAgent.Webhook/LunchAgent.Webhook.csproj LunchAgent.Webhook/

COPY Directory.Build.props ./

RUN dotnet restore -a $TARGETARCH LunchAgent.Webhook/LunchAgent.Webhook.csproj

COPY . .

RUN dotnet publish LunchAgent.Webhook/LunchAgent.Webhook.csproj \
	-a $TARGETARCH \
	-c Release \
	-o /app

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "LunchAgent.Webhook.dll"]