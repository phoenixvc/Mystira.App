# Hotfix Process

**Severity**: Critical  
**Time to Complete**: 1-2 hours  
**Prerequisites**: Critical bug identified, fix developed and tested

---

## Overview

This runbook guides you through deploying an urgent fix to production outside the normal release cycle. Use this process only for critical bugs that affect user experience or data integrity.

## Prerequisites

- [ ] Critical bug confirmed (production outage, data loss risk, security vulnerability)
- [ ] Fix developed and code-reviewed
- [ ] Fix tested locally
- [ ] Incident ticket created
- [ ] Stakeholders notified
- [ ] On-call engineer available

## When to Use This Process

Use hotfix process for:
- **Critical bugs**: Production outages, data corruption
- **Security vulnerabilities**: Authentication bypass, data exposure
- **Data loss risks**: Issues that could cause data deletion or corruption
- **Regulatory compliance**: Issues that could result in compliance violations (e.g., COPPA)

Do NOT use for:
- Feature requests
- Minor bugs
- Performance improvements (unless causing outage)
- Cosmetic issues

## Hotfix Steps

### Step 1: Create Hotfix Branch

```bash
# Create hotfix branch from main/production branch
git checkout main
git pull origin main
git checkout -b hotfix/critical-bug-description

# Make your fix
# Commit changes
git add .
git commit -m "fix: [HOTFIX] Brief description of fix

Fixes #[issue-number]

- Detailed description of the bug
- Root cause analysis
- Fix implemented
- Testing performed"
```

### Step 2: Fast-Track Code Review

Get urgent code review:
- Tag reviewers in PR with "URGENT HOTFIX" label
- Include:
  - Description of the bug
  - Impact assessment
  - Root cause analysis
  - Fix description
  - Testing done
  - Rollback plan

### Step 3: Run Automated Tests

```bash
# Run all tests locally
dotnet test

# Ensure no regressions
```

### Step 4: Deploy to Staging

Deploy hotfix to staging first:

```bash
gh workflow run mystira-app-api-cicd-staging.yml \
  --ref hotfix/critical-bug-description
```

### Step 5: Verify in Staging

Test the fix in staging:

```bash
# Check health
curl https://api.staging.mystira.app/health

# Test the specific bug fix
# [Add specific test commands]

# Verify no regressions
```

### Step 6: Get Approval

Obtain emergency approval for production deployment:
- [ ] Technical lead approval
- [ ] Product owner notification
- [ ] Security review (if applicable)

### Step 7: Deploy to Production

```bash
# Merge hotfix to main
git checkout main
git merge hotfix/critical-bug-description
git push origin main

# Trigger production deployment
gh workflow run mystira-app-api-cicd-prod.yml \
  --ref main
```

### Step 8: Monitor Deployment

Closely monitor the hotfix deployment:

```bash
# Watch deployment
gh run watch

# Monitor Application Insights
# - Error rates
# - Response times
# - Specific metrics related to the bug

# Check logs
az webapp log tail --name mys-prod-mystira-api-san --resource-group rg-mystira-prod
```

### Step 9: Verify Fix

Confirm the bug is fixed in production:

```bash
# Test the specific issue
# [Add specific verification commands]

# Check metrics
# Ensure error rate decreased
# Verify user reports
```

## Post-Hotfix Steps

### Step 10: Update Documentation

- [ ] Update release notes with hotfix details
- [ ] Document root cause in incident report
- [ ] Update monitoring alerts if needed

### Step 11: Backport to Development

Ensure the fix is in development branches:

```bash
# Merge hotfix to develop branch
git checkout develop
git merge main
git push origin develop
```

### Step 12: Create Incident Report

Document the incident:
- Bug description
- Impact (users affected, duration)
- Root cause
- Fix applied
- Prevention measures

### Step 13: Notify Stakeholders

Send hotfix completion notification:
- Bug fixed in production
- Hotfix version deployed
- Impact summary
- Prevention measures

## Verification

### Success Criteria

- [ ] Bug no longer reproducible in production
- [ ] No new errors introduced
- [ ] Error rate returned to normal
- [ ] User-facing functionality restored
- [ ] Metrics show issue resolved

### Monitoring

Monitor for next 2-4 hours:
- [ ] Application Insights errors
- [ ] User feedback
- [ ] Related metrics
- [ ] Support tickets

## Rollback

If hotfix causes new issues:

```bash
# Immediate rollback
gh workflow run mystira-app-api-rollback.yml \
  --ref main \
  -f target_slot=production \
  -f reason="Hotfix caused regression - [description]"
```

See [Emergency Rollback](./emergency-rollback.md) for full rollback procedure.

## Troubleshooting

### Issue: Hotfix didn't resolve the bug

**Resolution**:
1. Verify the root cause analysis was correct
2. Check if there are multiple instances of the bug
3. Review logs for additional context
4. Prepare second hotfix if needed

### Issue: Hotfix caused regressions

**Resolution**:
1. Assess severity of regression
2. If critical: immediate rollback
3. If minor: monitor and plan fix
4. Update tests to catch similar issues

## Post-Procedure

- [ ] Complete incident postmortem
- [ ] Update tests to prevent recurrence
- [ ] Review alerting to detect similar issues earlier
- [ ] Update runbook with lessons learned
- [ ] Schedule team retrospective
