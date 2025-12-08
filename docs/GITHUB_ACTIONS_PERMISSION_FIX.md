# GitHub Actions Permission Error Fix

## Error Details

**Error**: `RequestError [HttpError]: Resource not accessible by integration`

**Root Cause**: GitHub Actions workflows require explicit permissions to access GitHub API resources. Without proper permissions, the `GITHUB_TOKEN` cannot perform certain actions.

---

## Fixes Applied

### 1. SWA Preview Tests Workflow (`swa-preview-tests.yml`)

**Problem**: Workflow tried to read PR comments using `github.rest.issues.listComments` without proper permissions.

**Fix**: Added permissions block:
```yaml
permissions:
  contents: read
  pull-requests: read  # Required to read PR comments
  issues: read         # Required for github.rest.issues.listComments
```

**Why**: The `github-script` action needs to read PR comments to extract the SWA preview URL. Without `pull-requests: read` and `issues: read` permissions, it gets a 403 Forbidden error.

---

### 2. Automated Staging Setup Workflow (`staging-automated-setup.yml`)

**Problem**: Workflow tried to set GitHub secrets using `gh secret set` with insufficient permissions.

**Original Permissions**:
```yaml
permissions:
  id-token: write
  contents: read
  actions: write
```

**Updated Permissions**:
```yaml
permissions:
  id-token: write
  contents: read
  actions: write
  secrets: write        # Required for gh secret set
  pull-requests: write  # Required for gh workflow run
```

**Why**: 
- `secrets: write` - Needed to create/update repository secrets
- `pull-requests: write` - Needed to trigger workflows and write PR comments

---

## GitHub Actions Permissions Reference

### Common Permissions

| Permission | Access Level | Use Case |
|------------|--------------|----------|
| `contents: read` | Read repo files | Checkout code, read files |
| `contents: write` | Modify repo files | Commit changes, create tags |
| `pull-requests: read` | Read PRs | Read PR details, comments |
| `pull-requests: write` | Modify PRs | Create/update PRs, add comments |
| `issues: read` | Read issues | Read issue/PR comments (PRs are issues) |
| `issues: write` | Modify issues | Create/update issues, add comments |
| `actions: read` | Read workflows | View workflow runs |
| `actions: write` | Trigger workflows | Run workflows, cancel runs |
| `secrets: write` | Manage secrets | Create/update repo secrets |
| `id-token: write` | OIDC tokens | Azure/AWS authentication |

### Default Permissions

If no `permissions` block is specified, GitHub Actions uses restrictive defaults in newer repos:
- `contents: read` (read-only)
- All other permissions: none

---

## How to Debug Permission Errors

### 1. Check Error Message
```
RequestError [HttpError]: Resource not accessible by integration
```
This indicates missing permissions for the GitHub API call.

### 2. Identify the API Call
Look at the workflow logs to see which GitHub API endpoint failed:
- `github.rest.issues.listComments` → needs `issues: read`
- `gh secret set` → needs `secrets: write`
- `github.rest.pulls.create` → needs `pull-requests: write`

### 3. Add Required Permission
Add the permission to the workflow's `permissions` block:
```yaml
permissions:
  <permission-name>: read|write
```

### 4. Test the Fix
- Commit the change
- Trigger the workflow
- Verify it passes

---

## Best Practices

### 1. Principle of Least Privilege
Only grant permissions that are actually needed:
```yaml
# ✅ Good - Only what's needed
permissions:
  contents: read
  issues: read

# ❌ Too broad - Unnecessary write access
permissions:
  contents: write
  issues: write
```

### 2. Job-Level Permissions (Optional)
You can set permissions at the job level instead of workflow level:
```yaml
permissions: {}  # Disable all at workflow level

jobs:
  build:
    permissions:
      contents: read  # Only for this job
    steps:
      # ...
```

### 3. Document Why Permissions Are Needed
```yaml
permissions:
  contents: read        # Checkout code
  pull-requests: read   # Read PR comments for SWA URL
  issues: read          # Access PR comments (PRs are issues)
```

---

## Verification

After applying these fixes:

1. **SWA Preview Tests** should now:
   - ✅ Successfully read PR comments
   - ✅ Extract SWA preview URLs
   - ✅ Run smoke tests

2. **Automated Staging Setup** should now:
   - ✅ Set GitHub secrets
   - ✅ Trigger workflows
   - ✅ Complete full automation

---

## Related Issues

- GitHub Actions changed default permissions from permissive to restrictive in 2023
- Older repos may have legacy permissions model
- New repos created after Feb 2023 use restrictive defaults

---

## Additional Resources

- [GitHub Actions Permissions Reference](https://docs.github.com/en/actions/security-guides/automatic-token-authentication#permissions-for-the-github_token)
- [GitHub API Scopes](https://docs.github.com/en/rest/overview/permissions-required-for-github-apps)
- [Troubleshooting Permission Errors](https://docs.github.com/en/actions/security-guides/automatic-token-authentication#troubleshooting)

---

**Status**: ✅ Fixed in commit  
**Affected Workflows**: `swa-preview-tests.yml`, `staging-automated-setup.yml`  
**Impact**: Workflows will now execute successfully without permission errors
