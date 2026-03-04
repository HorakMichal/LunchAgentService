FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY LunchAgent.Core/LunchAgent.Core.csproj LunchAgent.Core/
COPY LunchAgent.API/LunchAgent.API.csproj LunchAgent.API/

COPY Directory.Build.props ./

RUN dotnet restore -a $TARGETARCH LunchAgent.API/LunchAgent.API.csproj

COPY . .

RUN dotnet publish LunchAgent.API/LunchAgent.API.csproj \
	-a $TARGETARCH \
	-c Release \
	-o /app

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "LunchAgent.API.dll"]