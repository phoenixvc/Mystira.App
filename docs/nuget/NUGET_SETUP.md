# NuGet Feed Setup Guide

This guide explains how to set up and use the GitHub Packages NuGet feed for Mystira shared libraries.

## Prerequisites

- GitHub account with access to phoenixvc organization
- Personal Access Token (PAT) with `read:packages` and `write:packages` permissions
- .NET SDK 9.0 or later

## GitHub Packages NuGet Feed

The Mystira shared libraries are published to GitHub Packages: `https://nuget.pkg.github.com/phoenixvc/index.json`

## Local Development Setup

### 1. Create Personal Access Token (PAT)

1. Go to GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Click **Generate new token (classic)**
3. Name: `Mystira NuGet Feed Access`
4. Select scopes:
   - `read:packages` - Download packages
   - `write:packages` - Publish packages (if needed)
5. Click **Generate token** and **copy the token** (save it securely)

### 2. Configure NuGet Source

**Option A: Command Line** (Recommended)

```bash
dotnet nuget add source https://nuget.pkg.github.com/phoenixvc/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text
```

Replace:
- `YOUR_GITHUB_USERNAME`: Your GitHub username
- `YOUR_GITHUB_PAT`: Personal Access Token created above

**Option B: User-Level NuGet.Config** (Most Secure)

Store credentials in user-level NuGet config to avoid committing credentials:

**Windows**: `%APPDATA%\NuGet\NuGet.Config`
**Linux/Mac**: `~/.nuget/NuGet/NuGet.Config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/phoenixvc/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github>
  </packageSourceCredentials>
</configuration>
```

**Option C: Repository NuGet.config**

Copy `NuGet.config.template` to `NuGet.config` and add your credentials.

**⚠️ Security Note**: If using repository NuGet.config, ensure it's in `.gitignore` to prevent committing credentials.

### 3. Verify Configuration

```bash
dotnet nuget list source
```

You should see `github` in the list.

### 4. Restore Packages

```bash
dotnet restore
```

Packages should restore from both nuget.org and GitHub Packages.

## CI/CD Setup

### GitHub Actions Workflow

GitHub Actions workflows automatically use `GITHUB_TOKEN` for authentication - **no additional secrets required**.

The workflow is configured in `.github/workflows/publish-shared-packages.yml` and uses:
- `GITHUB_TOKEN` - Automatically provided by GitHub Actions
- `github.actor` - The GitHub username triggering the workflow

No manual secret configuration needed for CI/CD!

## Testing Package Publishing

### Manual Test

```bash
# Restore dependencies
dotnet restore src/Mystira.App.Domain/Mystira.App.Domain.csproj

# Build
dotnet build src/Mystira.App.Domain/Mystira.App.Domain.csproj --configuration Release

# Pack
dotnet pack src/Mystira.App.Domain/Mystira.App.Domain.csproj --configuration Release --output ./nupkg

# Push to GitHub Packages
dotnet nuget push ./nupkg/Mystira.App.Domain.1.0.0.nupkg \
  --source https://nuget.pkg.github.com/phoenixvc/index.json \
  --api-key YOUR_GITHUB_PAT
```

### Verify Package Published

1. Go to GitHub → **phoenixvc organization** → **Packages**
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
- Verify PAT has correct permissions (`read:packages` and `write:packages` for publishing)
- Check PAT hasn't expired
- Verify GitHub username is correct
- Try regenerating the PAT with correct scopes

### Package Not Found

**Error**: `404 (Not Found)` when restoring

**Solutions**:
- Verify package was published successfully (check GitHub Packages)
- Check package name matches exactly (case-sensitive)
- Verify version number matches
- Ensure you have access to the phoenixvc organization

### Feed URL Issues

**Error**: `404` or connection errors

**Solutions**:
- Verify feed URL is: `https://nuget.pkg.github.com/phoenixvc/index.json`
- Check you have access to phoenixvc organization
- Verify authentication is configured correctly
- Try clearing NuGet cache: `dotnet nuget locals all --clear`

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

- [Implementation Status](./IMPLEMENTATION_STATUS.md)
- [Package Publishing Checklist](./PACKAGE_PUBLISHING_CHECKLIST.md)
- [NuGet.config Template](../../NuGet.config.template)
- [GitHub Packages Documentation](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)

