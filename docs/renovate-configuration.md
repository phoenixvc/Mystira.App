# Renovate Configuration

## Overview

This document explains the Renovate Bot configuration for the Mystira.App repository.

## Configuration File

The configuration is stored in `renovate.json` at the repository root.

## Key Configuration Details

### Base Configuration

- **Base Branch**: `dev` - All dependency updates target the `dev` branch
- **Labels**: Automatically adds `dependencies` label to PRs
- **Rate Limits**:
  - PR Hourly Limit: 5 PRs per hour
  - PR Concurrent Limit: 10 PRs open at once

### Package Rules

#### Internal Mystira Packages

The following packages are **disabled** from Renovate updates because they are internal packages managed separately:

- `Mystira.Shared` - Internal shared utilities package
- `Mystira.Contracts` - Internal contract definitions package
- All `Mystira.App.*` packages - Internal project packages

**Rationale**: These packages are hosted in a private GitHub Package Registry (`https://nuget.pkg.github.com/phoenixvc/index.json`) that Renovate cannot authenticate to. Since these are internal packages under our control, they should be updated manually or through separate CI/CD processes.

#### External Package Updates

- Minor and patch updates for external packages are not auto-merged
- All updates require manual review before merging

## Common Issues

### Package Lookup Failures

**Problem**: Renovate reports warnings like "Failed to look up nuget package Mystira.Shared"

**Solution**: These packages are intentionally disabled in the configuration. The warnings can be safely ignored as they indicate Renovate is correctly skipping these packages.

### Custom Registry Warnings

**Problem**: Renovate warns "Custom registries are not allowed for this datasource and will be ignored"

**Solution**: We previously tried to add GitHub Package Registry to the `nuget.registryUrls` configuration, but Renovate doesn't support custom NuGet registries in this way. The solution is to disable updates for packages that require custom registries.

## Updating Internal Packages

Internal `Mystira.*` packages should be updated through the following process:

1. Update the package in its source repository
2. Publish the new version to GitHub Packages
3. Manually update the version references in `.csproj` files
4. Test the changes locally
5. Create a PR with the package updates

## Testing Configuration Changes

After modifying `renovate.json`:

1. Validate JSON syntax using one of these methods:
   - `python3 -m json.tool renovate.json` (if Python 3 is available)
   - `jq . renovate.json` (if jq is installed)
   - Use any online JSON validator
2. Check against schema: The `$schema` property provides IntelliSense in most editors
3. Trigger a Renovate run by checking the box in the Dependency Dashboard issue

## References

- [Renovate Documentation](https://docs.renovatebot.com/)
- [Renovate Configuration Options](https://docs.renovatebot.com/configuration-options/)
- [NuGet Manager Documentation](https://docs.renovatebot.com/modules/manager/nuget/)
