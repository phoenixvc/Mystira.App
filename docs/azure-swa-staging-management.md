# Azure Static Web Apps - Staging Environment Management

## Overview

This document explains how staging environments are managed for Azure Static Web Apps (SWA) in the Mystira project and how to handle the staging environment limit.

## Problem Statement

Azure Static Web Apps has a limit on the number of staging (preview) environments:
- **Free tier**: 3 staging environments
- **Standard tier**: 10 staging environments
- **Enterprise tier**: Custom limits

When pull requests are opened, Azure SWA creates a preview environment for testing. If these environments are not properly cleaned up when PRs are closed, they accumulate and eventually hit the limit, causing deployment failures:

```
The content server has rejected the request with: BadRequest
Reason: This Static Web App already has the maximum number of staging environments.
Please remove one and try again.
```

## Solution

### Automatic Cleanup (Implemented)

The PWA CI/CD workflows have been updated to ensure preview environments are **always** cleaned up when a PR is closed, regardless of which files were changed:

**Before:**
```yaml
pull_request:
  branches: [dev]
  types: [opened, synchronize, reopened, closed]
  paths:
    - "src/Mystira.App.PWA/**"
    # ... other paths
```
**Problem**: Path filter applies to ALL PR actions including 'closed', so cleanup only runs if PWA files were changed.

**After:**
```yaml
# Build/deploy - with path filters on push only
push:
  branches: [dev]
  paths:
    - "src/Mystira.App.PWA/**"
    # ... other paths

# All PR events - NO path filter
pull_request:
  branches: [dev]
  types: [opened, synchronize, reopened, closed]
  # Note: No path filter to ensure cleanup job always runs when PR is closed
  # The build-and-deploy job has its own path filtering via job-level conditions
```

**Key Points:**
1. ✅ Build job only runs when action is not 'closed' (controlled by job-level `if` condition)
2. ✅ Cleanup job ALWAYS runs when a PR is closed (no path filter on PR trigger)
3. ⚠️ Trade-off: Workflow runs for all PRs, but jobs are skipped appropriately (minimal CI minute usage)

### Manual Cleanup Workflow

A manual cleanup workflow is available at: `.github/workflows/swa-cleanup-staging-environments.yml`

**How to use:**

1. Go to GitHub Actions → "SWA Cleanup - Staging Environments"
2. Click "Run workflow"
3. Select the environment (dev/staging/prod)
4. Optionally provide a PR number to comment on
5. Click "Run workflow"

This workflow will:
- Send a cleanup request to Azure SWA
- Optionally comment on the PR (if PR number provided)
- Remove the orphaned staging environment

## How to Check Current Staging Environments

You can view current staging environments in the Azure Portal:

1. Navigate to your Azure Static Web App resource
2. Go to "Environments" in the left menu
3. Review the list of staging environments
4. Each environment shows:
   - Environment name
   - Status (Active/Inactive)
   - Creation date
   - Associated branch/PR

## Best Practices

### For Developers

1. **Always close PRs properly** - Use GitHub's "Close" or "Merge" button rather than abandoning PRs
2. **Monitor staging environments** - Check the Azure Portal periodically to ensure environments are being cleaned up
3. **Use manual cleanup if needed** - If you notice orphaned environments, use the manual cleanup workflow

### For Repository Administrators

1. **Monitor SWA quota** - Keep an eye on the number of staging environments
2. **Consider upgrading SWA tier** - If you frequently hit the limit with legitimate PRs, upgrade to Standard tier (10 environments)
3. **Review old PRs** - Periodically check for closed PRs that might have orphaned environments

## Troubleshooting

### Deployment Fails with "Maximum Staging Environments" Error

**Quick Fix:**
1. Identify orphaned staging environments in Azure Portal
2. Run the manual cleanup workflow for each orphaned environment
3. Retry the deployment

**Long-term Fix:**
- Ensure the updated workflow files are merged to all branches (dev, staging, main)
- Consider upgrading Azure SWA tier if you have many concurrent PRs

### Preview URL Not Available

If a PR doesn't get a preview URL:

1. Check if the workflow ran (GitHub Actions tab)
2. Verify the paths changed match the workflow path filters
3. Check if the deployment succeeded in the workflow logs
4. Look for the SWA comment on the PR (posted by github-actions bot)

### Cleanup Not Running

If cleanup doesn't run when a PR is closed:

1. Verify the workflow has the updated trigger configuration (no path filter on `closed` event)
2. Check workflow logs to see if the `close-pr` job ran
3. Manually run the cleanup workflow as a fallback

## Related Workflows

- `mystira-app-pwa-cicd-dev.yml` - Dev environment deployment
- `mystira-app-pwa-cicd-staging.yml` - Staging environment deployment
- `mystira-app-pwa-cicd-prod.yml` - Production environment deployment
- `swa-cleanup-staging-environments.yml` - Manual cleanup workflow
- `swa-preview-tests.yml` - Automated testing of preview deployments

## Additional Resources

- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Azure SWA Pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/static/)
- [Azure SWA GitHub Action](https://github.com/Azure/static-web-apps-deploy)
