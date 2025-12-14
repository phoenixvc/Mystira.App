# NuGet Feed Setup Guide

This guide explains how to set up and use the internal NuGet feed for Mystira shared libraries.

## Prerequisites

- Azure DevOps account with Artifacts access
- Personal Access Token (PAT) with Packaging (Read & Write) permissions
- .NET SDK 9.0 or later

## Azure DevOps Artifacts Feed Setup

### 1. Create Feed

1. Go to Azure DevOps: `https://dev.azure.com/{your-org}/{your-project}`
2. Navigate to **Artifacts**
3. Click **+ Create Feed**
4. Name: `Mystira-Internal`
5. Visibility: **Organization** or **Project** (as needed)
6. Upstream sources: Enable **nuget.org** (recommended)
7. Click **Create**

### 2. Configure Permissions

1. Click on the feed **Mystira-Internal**
2. Go to **Feed settings** → **Permissions**
3. Add users/groups:
   - **Readers**: All developers
   - **Contributors**: Team leads, CI/CD service principals
   - **Owners**: Admins

### 3. Get Feed URL

1. In feed settings, click **Connect to feed**
2. Select **NuGet.exe** or **.NET CLI**
3. Copy the feed URL (format):
   ```
   https://pkgs.dev.azure.com/{org}/{project}/_packaging/{feed}/nuget/v3/index.json
   ```

## Local Development Setup

### 1. Create Personal Access Token (PAT)

1. Go to Azure DevOps → **User settings** → **Personal access tokens**
2. Click **+ New Token**
3. Name: `NuGet Feed Access`
4. Organization: Select your organization
5. Scopes: **Packaging** → **Read & write**
6. Click **Create** and **copy the token** (save it securely)

### 2. Configure NuGet Source

**Option A: Using NuGet.config (Recommended)**

Create or update `NuGet.config` in your repository root or solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Mystira-Internal" 
         value="https://pkgs.dev.azure.com/{org}/{project}/_packaging/{feed}/nuget/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <Mystira-Internal>
      <add key="Username" value="{your-email@example.com}" />
      <add key="ClearTextPassword" value="{your-pat-token}" />
    </Mystira-Internal>
  </packageSourceCredentials>
</configuration>
```

**⚠️ Security Note**: Add `NuGet.config` to `.gitignore` if storing credentials, or use user-level config.

**Option B: Using dotnet CLI**

```bash
dotnet nuget add source https://pkgs.dev.azure.com/{org}/{project}/_packaging/{feed}/nuget/v3/index.json \
  --name "Mystira-Internal" \
  --username "{your-email@example.com}" \
  --password "{your-pat-token}" \
  --store-password-in-clear-text
```

**Option C: User-Level Config (Most Secure)**

Store credentials in user-level NuGet config:

```bash
# Windows
%APPDATA%\NuGet\NuGet.Config

# Linux/Mac
~/.nuget/NuGet/NuGet.Config
```

## CI/CD Setup

### GitHub Secrets Required

Add these secrets to your GitHub repository:

- `MYSTIRA_DEVOPS_AZURE_ORG` - Your Azure DevOps organization name
- `MYSTIRA_DEVOPS_AZURE_PROJECT` - Your Azure DevOps project name
- `MYSTIRA_DEVOPS_AZURE_PAT` - Personal Access Token with Packaging (Read & Write) permissions
- `MYSTIRA_DEVOPS_NUGET_FEED` - Feed name (e.g., `Mystira-Internal`)

### Service Principal (Recommended for CI/CD)

Instead of using a user PAT, create a service principal:

1. In Azure DevOps, go to **Project settings** → **Service connections**
2. Create new service connection (type: **Azure Resource Manager**)
3. Use **Managed Identity** or **Service Principal**
4. Grant **Packaging (Read & Write)** permissions
5. Use service principal credentials in GitHub secrets

## Testing Package Publishing

### Manual Test

```bash
# Restore dependencies
dotnet restore src/Mystira.App.Domain/Mystira.App.Domain.csproj

# Build
dotnet build src/Mystira.App.Domain/Mystira.App.Domain.csproj --configuration Release

# Pack
dotnet pack src/Mystira.App.Domain/Mystira.App.Domain.csproj --configuration Release --output ./nupkg

# Push to feed
dotnet nuget push ./nupkg/Mystira.App.Domain.1.0.0.nupkg \
  --source "Mystira-Internal" \
  --api-key "{your-pat-token}"
```

### Verify Package Published

1. Go to Azure DevOps → Artifacts → Mystira-Internal feed
2. Check if package appears: `Mystira.App.Domain` version `1.0.0`
3. Verify package contents and metadata

## Consuming Packages

### In Admin API (After Extraction)

Update `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="Mystira.App.Domain" Version="1.0.0" />
  <PackageReference Include="Mystira.App.Application" Version="1.0.0" />
  <!-- ... other packages -->
</ItemGroup>
```

Then restore:

```bash
dotnet restore
dotnet build
```

### Update Package Versions

When a new version is published:

1. Update `Version` in consuming project's `.csproj`:
   ```xml
   <PackageReference Include="Mystira.App.Domain" Version="1.1.0" />
   ```

2. Restore and build:
   ```bash
   dotnet restore
   dotnet build
   ```

## Troubleshooting

### Authentication Failed

**Error**: `401 (Unauthorized)` or authentication errors

**Solutions**:
- Verify PAT has correct permissions (Packaging Read & Write)
- Check PAT hasn't expired
- Verify feed URL is correct
- Check username matches Azure DevOps account

### Package Not Found

**Error**: `404 (Not Found)` when restoring

**Solutions**:
- Verify package was published successfully
- Check package name matches exactly (case-sensitive)
- Verify version number matches
- Check feed permissions (must have Read access)

### Feed URL Issues

**Error**: `404` or connection errors

**Solutions**:
- Verify feed URL format is correct
- Check organization and project names are correct
- Verify feed name matches exactly
- Try regenerating feed URL from Azure DevOps UI

### Version Conflicts

**Error**: Version resolution conflicts

**Solutions**:
- Ensure all packages use compatible versions
- Check dependency graph (Domain → Application → Infrastructure)
- Update all related packages together
- Review package dependency versions

## Package Version Management

### Semantic Versioning

- **Major** (`2.0.0`): Breaking changes
- **Minor** (`1.1.0`): New features (backward compatible)
- **Patch** (`1.0.1`): Bug fixes (backward compatible)

### Versioning Process

1. **Update Version** in `.csproj`:
   ```xml
   <Version>1.1.0</Version>
   ```

2. **Commit and Push**:
   ```bash
   git add src/Mystira.App.Domain/Mystira.App.Domain.csproj
   git commit -m "chore: bump Mystira.App.Domain to 1.1.0"
   git push
   ```

3. **CI/CD Publishes** automatically on push to `main`

4. **Update Consumers** to new version

## Related Documentation

- [ADR-0007: NuGet Feed Strategy](../../../../docs/architecture/adr/0007-nuget-feed-strategy-for-shared-libraries.md)
- [ADR-0006: Admin API Repository Extraction](../../../../docs/architecture/adr/0006-admin-api-repository-extraction.md)
- [Migration Plan](../../../../docs/migration/ADMIN_API_EXTRACTION_PLAN.md)

