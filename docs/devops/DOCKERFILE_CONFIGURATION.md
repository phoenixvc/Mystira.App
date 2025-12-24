# Dockerfile Configuration for Linux Deployment

## Overview

This document describes the Dockerfile configuration for the Mystira.App API services to ensure proper Linux/Azure App Service deployment.

## Issue Identified

The original `Mystira.App.Admin.Api/Dockerfile` had incorrect project references that pointed to `Mystira.App.Api` instead of `Mystira.App.Admin.Api`. This would cause build failures during Azure App Service deployment on Linux.

## Fixed Configuration

### Admin API Dockerfile (`src/Mystira.App.Admin.Api/Dockerfile`)

The Dockerfile now correctly references:
- Project: `Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj`
- Working Directory: `/src/Mystira.App.Admin.Api`
- Entry Point DLL: `Mystira.App.Admin.Api.dll`

All project dependencies are properly copied:
```dockerfile
COPY ["Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj", "Mystira.App.Admin.Api/"]
COPY ["Mystira.App.Domain/Mystira.App.Domain.csproj", "Mystira.App.Domain/"]
COPY ["Mystira.App.Infrastructure.Azure/Mystira.App.Infrastructure.Azure.csproj", "Mystira.App.Infrastructure.Azure/"]
COPY ["Mystira.App.Infrastructure.Data/Mystira.App.Infrastructure.Data.csproj", "Mystira.App.Infrastructure.Data/"]
COPY ["Mystira.App.Infrastructure.StoryProtocol/Mystira.App.Infrastructure.StoryProtocol.csproj", "Mystira.App.Infrastructure.StoryProtocol/"]
COPY ["Mystira.App.Shared/Mystira.App.Shared.csproj", "Mystira.App.Shared/"]
COPY ["Mystira.Contracts.App/Mystira.Contracts.App.csproj", "Mystira.Contracts.App/"]
COPY ["Mystira.App.Application/Mystira.App.Application.csproj", "Mystira.App.Application/"]
```

### Main API Dockerfile (`src/Mystira.App.Api/Dockerfile`)

Similarly updated to include all required project dependencies, including the `Mystira.App.Infrastructure.Discord` reference which was previously missing.

## Key Points

1. **Multi-stage Build**: Both Dockerfiles use a multi-stage build pattern:
   - `base`: Runtime image (mcr.microsoft.com/dotnet/aspnet:9.0)
   - `build`: SDK image for compilation
   - `publish`: Published output
   - `final`: Final runtime image with published files

2. **Security**: Non-root user (`appuser`) is created for running the application

3. **Dependencies**: All project references from the `.csproj` files must be included in the COPY commands to ensure proper restore and build

4. **Linux Compatibility**: Uses `adduser --disabled-password --gecos ''` which works on Debian-based Linux images

## Testing

To test the Dockerfiles locally:

```bash
# Build Admin API
dotnet restore src/Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj
dotnet build src/Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj --configuration Release
dotnet publish src/Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj --configuration Release --output ./publish

# Verify DLL exists
ls ./publish/Mystira.App.Admin.Api.dll
```

## Azure App Service Deployment

Azure App Service on Linux uses Oryx to detect and run .NET applications. The system:
1. Detects the .NET 9.0 runtime requirement
2. Finds the startup DLL (`Mystira.App.Admin.Api.dll` or `Mystira.App.Api.dll`)
3. Runs: `dotnet "AppName.dll"`

The fixed Dockerfiles ensure the correct DLL is produced and all dependencies are included.

## Related Files

- `src/Mystira.App.Admin.Api/Dockerfile`
- `src/Mystira.App.Api/Dockerfile`
- `.github/workflows/mystira-app-admin-api-cicd-*.yml`
- `.github/workflows/mystira-app-api-cicd-*.yml`

## Date

December 10, 2025
