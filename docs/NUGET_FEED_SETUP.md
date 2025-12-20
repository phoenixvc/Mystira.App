# NuGet Feed Setup Guide

This guide explains how to configure the GitHub Packages NuGet feed for Mystira shared packages.

## GitHub Packages Feed

The Mystira shared libraries are published to GitHub Packages: `https://nuget.pkg.github.com/phoenixvc/index.json`

## Local Development Setup

### 1. Get Feed URL

The feed URL is: `https://nuget.pkg.github.com/phoenixvc/index.json`

This is automatically available to anyone with access to the phoenixvc organization on GitHub.

### 2. Create Personal Access Token (PAT)

1. Go to GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Click **Generate new token (classic)**
3. Name: `Mystira NuGet Packages`
4. Select scopes:
   - `read:packages` - Download packages from GitHub Packages
   - `write:packages` - Publish packages (if needed)
5. Click **Generate token**
6. **Copy the token immediately** (you won't see it again!)

### 3. Configure NuGet Source

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

**Option B: Edit NuGet.config**

Copy `NuGet.config.template` to `NuGet.config` in the repository root and add your credentials.

**⚠️ Security Note**: Do NOT commit `NuGet.config` with credentials to git. Use user-level NuGet config instead:

```bash
# User-level config location:
# Windows: %APPDATA%\NuGet\NuGet.Config
# Linux/Mac: ~/.nuget/NuGet/NuGet.Config
```

### 4. Verify Configuration

```bash
dotnet nuget list source
```

You should see `Mystira-Internal` in the list.

### 5. Restore Packages

```bash
dotnet restore
```

Packages should restore from both nuget.org and GitHub Packages.

## CI/CD Setup

GitHub Actions workflows automatically authenticate with GitHub Packages using `GITHUB_TOKEN`.

**No secrets configuration required!**

The workflow automatically:
1. Uses `GITHUB_TOKEN` (provided by GitHub Actions)
2. Configures the feed before restoring/publishing packages
3. Publishes packages on push to `main` branch

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

**Error**: `Unable to load the service index for source` or `401 Unauthorized`

**Solution**:
1. Verify PAT has correct permissions (`read:packages`, `write:packages` for publishing)
2. Check PAT hasn't expired
3. Verify GitHub username is correct
4. Try removing and re-adding the source:
   ```bash
   dotnet nuget remove source github
   dotnet nuget add source https://nuget.pkg.github.com/phoenixvc/index.json \
     --name github \
     --username YOUR_GITHUB_USERNAME \
     --password YOUR_GITHUB_PAT \
     --store-password-in-clear-text
   ```

### Package Not Found

**Error**: `NU1101: Unable to find package`

**Solution**:
1. Verify package name is correct
2. Check package version exists in GitHub Packages
3. Ensure you have access to phoenixvc organization
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

- [NuGet Setup Guide (Detailed)](./nuget/NUGET_SETUP.md)
- [Implementation Status](./nuget/IMPLEMENTATION_STATUS.md)
- [GitHub Packages Documentation](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)

