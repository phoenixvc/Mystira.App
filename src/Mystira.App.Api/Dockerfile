# Use the official .NET 9.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
# NOTE: Build context should be the repository root, so paths are relative to /
COPY ["src/Mystira.App.Api/Mystira.App.Api.csproj", "Mystira.App.Api/"]
COPY ["src/Mystira.App.Domain/Mystira.App.Domain.csproj", "Mystira.App.Domain/"]
COPY ["src/Mystira.App.Infrastructure.Azure/Mystira.App.Infrastructure.Azure.csproj", "Mystira.App.Infrastructure.Azure/"]
COPY ["src/Mystira.App.Infrastructure.Data/Mystira.App.Infrastructure.Data.csproj", "Mystira.App.Infrastructure.Data/"]
COPY ["src/Mystira.App.Infrastructure.StoryProtocol/Mystira.App.Infrastructure.StoryProtocol.csproj", "Mystira.App.Infrastructure.StoryProtocol/"]
COPY ["src/Mystira.App.Application/Mystira.App.Application.csproj", "Mystira.App.Application/"]
COPY ["src/Mystira.App.Infrastructure.Discord/Mystira.App.Infrastructure.Discord.csproj", "Mystira.App.Infrastructure.Discord/"]
COPY ["src/Mystira.App.Shared/Mystira.App.Shared.csproj", "Mystira.App.Shared/"]

# Restore dependencies
RUN dotnet restore "Mystira.App.Api/Mystira.App.Api.csproj"

# Copy all source files from the src directory
COPY src/ .
WORKDIR "/src/Mystira.App.Api"

# Build the application
RUN dotnet build "Mystira.App.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Mystira.App.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy appsettings files explicitly to ensure they're available at runtime
# These should be overridden by environment variables or volume mounts in production
COPY --from=build /src/Mystira.App.Api/appsettings.json .

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Mystira.App.Api.dll"]