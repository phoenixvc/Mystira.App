# Use the official .NET 9.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Mystira.App.Api/Mystira.App.Api.csproj", "Mystira.App.Api/"]
COPY ["Mystira.App.Domain/Mystira.App.Domain.csproj", "Mystira.App.Domain/"]
RUN dotnet restore "Mystira.App.Api/Mystira.App.Api.csproj"

# Copy all source files
COPY . .
WORKDIR "/src/Mystira.App.Api"

# Build the application
RUN dotnet build "Mystira.App.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Mystira.App.Api.csproj" -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Mystira.App.Api.dll"]