# NuGet Feed Setup Guide

This guide explains how to configure the internal NuGet feed for Mystira shared packages.

## Azure DevOps Artifacts Feed

The Mystira shared libraries are published to an Azure DevOps Artifacts feed: `Mystira-Internal`

## Local Development Setup

### 1. Get Feed URL

The feed URL will be provided by your team lead or can be found in Azure DevOps:
- Go to Azure DevOps → Artifacts
- Select the `Mystira-Internal` feed
- Click "Connect to feed"
- Copy the NuGet package source URL

### 2. Configure NuGet Source

**Option A: Command Line** (Recommended)

```bash
dotnet nuget add source <FEED_URL> \
  --name "Mystira-Internal" \
  --username <AZURE_DEVOPS_USERNAME> \
  --password <AZURE_DEVOPS_PAT>
```

Replace:
- `<FEED_URL>`: The feed URL from Azure DevOps
- `<AZURE_DEVOPS_USERNAME>`: Your Azure DevOps username or email
- `<AZURE_DEVOPS_PAT>`: Personal Access Token with "Packaging (Read & write)" scope

**Option B: Edit NuGet.config**

Edit `NuGet.config` in the repository root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Mystira-Internal" 
         value="<FEED_URL>" />
  </packageSources>
  <packageSourceCredentials>
    <Mystira-Internal>
      <add key="Username" value="<USERNAME>" />
      <add key="ClearTextPassword" value="<PAT>" />
    </Mystira-Internal>
  </packageSourceCredentials>
</configuration>
```

**⚠️ Security Note**: Do NOT commit credentials to git. Use user-level NuGet config instead:

```bash
# User-level config location:
# Windows: %APPDATA%\NuGet\NuGet.Config
# Linux/Mac: ~/.nuget/NuGet/NuGet.Config
```

### 3. Create Personal Access Token (PAT)

1. Go to Azure DevOps → User Settings → Personal Access Tokens
2. Click "New Token"
3. Name: `Mystira NuGet Feed`
4. Scope: `Packaging (Read & write)`
5. Organization: Select your organization
6. Click "Create"
7. Copy the token (you won't see it again!)

### 4. Verify Configuration

```bash
dotnet nuget list source
```

You should see `Mystira-Internal` in the list.

### 5. Restore Packages

```bash
cd packages/app
dotnet restore
```

Packages should restore from both nuget.org and Mystira-Internal feed.

## CI/CD Setup

GitHub Actions workflows automatically configure the NuGet feed using secrets:

**Required Secrets** (configured in GitHub repository settings):
- `AZURE_DEVOPS_NUGET_FEED_URL`: The feed URL
- `AZURE_DEVOPS_USER`: Azure DevOps username
- `AZURE_DEVOPS_PAT`: Personal Access Token with Packaging permissions

The workflow automatically configures the feed before restoring/publishing packages.

## Using Shared Packages

### Adding Package Reference

In your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="Mystira.App.Domain" Version="1.0.0" />
  <PackageReference Include="Mystira.App.Application" Version="1.0.0" />
</ItemGroup>
```

### Updating Package Version

When a new version is published:

```bash
# Update to specific version
dotnet add package Mystira.App.Domain --version 1.1.0

# Or manually edit .csproj
# <PackageReference Include="Mystira.App.Domain" Version="1.1.0" />
```

### Available Packages

- `Mystira.App.Domain` (v1.0.0)
- `Mystira.App.Application` (v1.0.0)
- `Mystira.App.Contracts` (v1.0.0)
- `Mystira.App.Infrastructure.Azure` (v1.0.0)
- `Mystira.App.Infrastructure.Data` (v1.0.0)
- `Mystira.App.Infrastructure.Discord` (v1.0.0)
- `Mystira.App.Infrastructure.StoryProtocol` (v1.0.0)
- `Mystira.App.Shared` (v1.0.0)

## Troubleshooting

### Authentication Failed

**Error**: `Unable to load the service index for source`

**Solution**:
1. Verify PAT has correct permissions
2. Check feed URL is correct
3. Try removing and re-adding the source:
   ```bash
   dotnet nuget remove source "Mystira-Internal"
   dotnet nuget add source <FEED_URL> --name "Mystira-Internal" --username <USER> --password <PAT>
   ```

### Package Not Found

**Error**: `NU1101: Unable to find package`

**Solution**:
1. Verify package name is correct
2. Check package version exists in feed
3. Verify feed is configured correctly
4. Try clearing NuGet cache:
   ```bash
   dotnet nuget locals all --clear
   ```

### Version Conflict

**Error**: Package version conflicts with project references

**Solution**: 
- Remove project references when migrating to NuGet packages
- Ensure all projects use NuGet packages consistently

## Related Documentation

- [ADR-0007: NuGet Feed Strategy](../../../docs/architecture/adr/0007-nuget-feed-strategy-for-shared-libraries.md)
- [Migration Plan: Admin API Extraction](../../../docs/migration/ADMIN_API_EXTRACTION_PLAN.md)
- [Azure DevOps Artifacts Documentation](https://docs.microsoft.com/en-us/azure/devops/artifacts/)
