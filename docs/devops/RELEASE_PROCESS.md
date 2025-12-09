# Release Process

This document outlines the process for releasing new versions of the application to production.

## Versioning

We follow [Semantic Versioning](https://semver.org/) (SemVer):

```
MAJOR.MINOR.PATCH

Example: v1.2.3
```

| Component | When to Increment |
|-----------|-------------------|
| `MAJOR` | Breaking changes, incompatible API changes |
| `MINOR` | New features, backwards compatible |
| `PATCH` | Bug fixes, backwards compatible |

## Release Types

### Standard Release (MINOR/PATCH)

Regular planned releases with new features or bug fixes.

### Hotfix Release (PATCH)

Emergency fixes for critical production issues.

### Major Release (MAJOR)

Significant changes with potential breaking changes.

## Release Workflow

### Pre-Release Checklist

Before starting a release:

- [ ] All target features merged to `dev`
- [ ] All tests passing on `dev`
- [ ] Dev environment tested and validated
- [ ] Documentation updated
- [ ] CHANGELOG updated

### Step 1: Merge to Main

1. Create PR from `dev` to `main`:
   ```bash
   # Ensure dev is up to date
   git checkout dev
   git pull origin dev

   # Create PR via GitHub UI or CLI
   ```

2. Review PR:
   - Check all changes since last release
   - Verify test results
   - Review infrastructure changes

3. Merge PR (squash merge recommended)

### Step 2: Staging Validation

After merge to `main`:

1. Auto-deployment to staging triggers
2. Wait for deployment to complete
3. Perform testing:
   - [ ] Smoke tests pass
   - [ ] Key user flows work
   - [ ] No performance regressions
   - [ ] Logs show no errors

### Step 3: Create Release Tag

Once staging validation passes:

```bash
# Checkout main and pull latest
git checkout main
git pull origin main

# Determine version number
# Check previous tags: git tag --list 'v*' --sort=-v:refname | head -5

# Create annotated tag
git tag -a v1.2.0 -m "Release v1.2.0

Features:
- Feature 1 description
- Feature 2 description

Fixes:
- Fix 1 description
- Fix 2 description
"

# Push the tag
git push origin v1.2.0
```

### Step 4: Production Deployment

1. Tag push triggers production deployment workflow
2. **Manual approval required** in GitHub Actions
3. Reviewer approves deployment
4. Deployment proceeds to production
5. Smoke tests run automatically

### Step 5: Post-Release

1. Verify production deployment:
   - [ ] Application accessible
   - [ ] Health endpoints responding
   - [ ] No error spikes in logs

2. Create GitHub Release:
   - Go to Releases in GitHub
   - Select the tag
   - Add release notes
   - Publish release

3. Notify stakeholders:
   - Update status page (if applicable)
   - Announce in team channels

## Hotfix Process

For critical production issues requiring immediate fix:

### Step 1: Create Hotfix Branch

```bash
# Branch from main (production code)
git checkout main
git pull origin main
git checkout -b bugfix/critical-fix-description
```

### Step 2: Implement Fix

1. Make minimal required changes
2. Add tests for the fix
3. Test locally

### Step 3: Fast-Track to Production

```bash
# Push and create PR to main
git push -u origin bugfix/critical-fix-description

# After PR approved and merged, create patch tag
git checkout main
git pull origin main
git tag -a v1.2.1 -m "Hotfix: Description of critical fix"
git push origin v1.2.1
```

### Step 4: Back-merge to Dev

```bash
# Ensure dev gets the fix too
git checkout dev
git pull origin dev
git merge main
git push origin dev
```

## Rollback Procedure

If production deployment fails or causes issues:

### Immediate Rollback

```bash
# If using deployment slots, swap back
az webapp deployment slot swap \
  --resource-group mys-prod-mystira-rg-euw \
  --name mys-prod-mystira-api-euw \
  --slot staging \
  --target-slot production
```

### Revert Release

If rollback needed after slot swap:

1. Identify last good version tag
2. Re-deploy that version:
   ```bash
   # Trigger deployment of previous version
   git checkout v1.1.0
   # Re-run deployment workflow manually
   ```

## Release Schedule

### Regular Releases

- **Frequency**: As needed, typically weekly or bi-weekly
- **Day**: Tuesday or Wednesday (avoid Friday deployments)
- **Time**: Morning (local time) to allow monitoring

### Freeze Periods

No releases during:
- Major holidays
- Critical business periods
- Scheduled maintenance windows

## Release Checklist Template

```markdown
## Release v[X.Y.Z] Checklist

### Pre-Release
- [ ] All target features merged to dev
- [ ] All tests passing
- [ ] Dev environment validated
- [ ] CHANGELOG updated
- [ ] PR from dev to main created
- [ ] PR reviewed and approved

### Staging
- [ ] PR merged to main
- [ ] Staging deployment successful
- [ ] Staging smoke tests pass
- [ ] Manual testing complete
- [ ] No blocking issues found

### Production
- [ ] Release tag created: v[X.Y.Z]
- [ ] Tag pushed to origin
- [ ] Production deployment approved
- [ ] Production deployment successful
- [ ] Production smoke tests pass
- [ ] Health checks passing
- [ ] No error spikes in monitoring

### Post-Release
- [ ] GitHub Release created
- [ ] Stakeholders notified
- [ ] Documentation updated (if needed)
- [ ] Dev branch updated with any hotfixes
```

## Versioning Examples

| Change Type | Current | New Version |
|-------------|---------|-------------|
| New feature (backward compatible) | v1.2.3 | v1.3.0 |
| Bug fix | v1.2.3 | v1.2.4 |
| Breaking API change | v1.2.3 | v2.0.0 |
| Hotfix to production | v1.2.3 | v1.2.4 |
| Security patch | v1.2.3 | v1.2.4 |

## Contact

For release-related questions or emergency releases:

- Primary: [Team Lead / Release Manager]
- Backup: [Secondary Contact]
- Emergency: [On-call Contact]
