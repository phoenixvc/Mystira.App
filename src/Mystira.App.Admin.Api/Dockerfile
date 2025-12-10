# Use the official .NET 9.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj", "Mystira.App.Admin.Api/"]
COPY ["Mystira.App.Domain/Mystira.App.Domain.csproj", "Mystira.App.Domain/"]
COPY ["Mystira.App.Infrastructure.Azure/Mystira.App.Infrastructure.Azure.csproj", "Mystira.App.Infrastructure.Azure/"]
COPY ["Mystira.App.Infrastructure.Data/Mystira.App.Infrastructure.Data.csproj", "Mystira.App.Infrastructure.Data/"]
COPY ["Mystira.App.Infrastructure.StoryProtocol/Mystira.App.Infrastructure.StoryProtocol.csproj", "Mystira.App.Infrastructure.StoryProtocol/"]
COPY ["Mystira.App.Shared/Mystira.App.Shared.csproj", "Mystira.App.Shared/"]
COPY ["Mystira.App.Contracts/Mystira.App.Contracts.csproj", "Mystira.App.Contracts/"]
COPY ["Mystira.App.Application/Mystira.App.Application.csproj", "Mystira.App.Application/"]
RUN dotnet restore "Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj"

# Copy all source files
COPY . .
WORKDIR "/src/Mystira.App.Admin.Api"

# Build the application
RUN dotnet build "Mystira.App.Admin.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Mystira.App.Admin.Api.csproj" -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Mystira.App.Admin.Api.dll"]