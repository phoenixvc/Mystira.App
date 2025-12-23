# Production Deployment

**Severity**: High  
**Time to Complete**: 30-45 minutes  
**Prerequisites**: Production release approved, tested in staging

---

## Overview

This runbook guides you through deploying a new version to the production environment. Follow these steps carefully to ensure a smooth deployment with minimal downtime.

## Prerequisites

- [ ] Release has been tested in staging environment
- [ ] All tests pass (unit, integration, E2E)
- [ ] Security scan (CodeQL) completed with no critical issues
- [ ] Production deployment approval obtained from stakeholders
- [ ] Database migrations reviewed and tested
- [ ] Rollback plan prepared
- [ ] On-call engineer available for monitoring
- [ ] Communication sent to stakeholders about deployment window

## Pre-Deployment Steps

### Step 1: Verify Staging Environment

Ensure the staging environment is stable and matches production configuration:

```bash
# Check staging API health
curl https://api.staging.mystira.app/health

# Verify staging database connectivity
curl https://api.staging.mystira.app/health/ready
```

### Step 2: Create Database Backup

Create a backup of the production database before deployment:

```bash
# For Cosmos DB - enable point-in-time restore if not already enabled
# Document current timestamp for potential restore
BACKUP_TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
echo "Backup timestamp: $BACKUP_TIMESTAMP"
```

### Step 3: Review Release Notes

Review the release notes and ensure all changes are documented:
- Feature changes
- Bug fixes
- Breaking changes (should be avoided in production)
- Configuration changes required
- Database migrations

## Deployment Steps

### Step 4: Deploy API Backend

Trigger the production deployment workflow:

```bash
# Navigate to GitHub Actions
# Go to: .github/workflows/mystira-app-api-cicd-prod.yml
# Click "Run workflow" and select the release branch/tag
```

Or via GitHub CLI:

```bash
gh workflow run mystira-app-api-cicd-prod.yml \
  --ref main \
  -f environment=production
```

### Step 5: Monitor Deployment

Watch the deployment progress:

```bash
# Monitor workflow execution
gh run watch

# Check deployment logs
gh run view --log
```

### Step 6: Run Database Migrations

If there are database migrations, run them now:

```bash
# SSH or Azure CLI to production app service
# Run migrations
dotnet ef database update --connection "$COSMOS_CONNECTION_STRING"
```

### Step 7: Verify Health Checks

Confirm the new version is healthy:

```bash
# Check API health
curl https://api.mystira.app/health

# Check database connectivity
curl https://api.mystira.app/health/ready

# Verify liveness
curl https://api.mystira.app/health/live
```

### Step 8: Deploy PWA Frontend

Deploy the PWA after API is confirmed stable:

```bash
gh workflow run mystira-app-pwa-cicd-prod.yml \
  --ref main \
  -f environment=production
```

### Step 9: Smoke Testing

Run smoke tests to verify critical functionality:

```bash
# Test authentication
curl -X POST https://api.mystira.app/api/auth/signin \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'

# Test scenario retrieval
curl https://api.mystira.app/api/scenarios \
  -H "Authorization: Bearer $TEST_TOKEN"

# Test game session creation
curl -X POST https://api.mystira.app/api/gamesessions \
  -H "Authorization: Bearer $TEST_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"scenarioId":"test-scenario"}'
```

## Post-Deployment Steps

### Step 10: Monitor Application Insights

Watch metrics for the first 30 minutes:

1. Open Application Insights in Azure Portal
2. Check:
   - Error rate (should be < 1%)
   - Response time (should be < 500ms p95)
   - Request volume
   - Failed requests
   - Custom metrics (game sessions, user sign-ups)

### Step 11: Verify Logs

Check logs for errors:

```bash
# View recent logs
az webapp log tail --name mys-prod-mystira-api-san --resource-group rg-mystira-prod
```

### Step 12: Update Documentation

- [ ] Update version number in release notes
- [ ] Document any configuration changes
- [ ] Update deployment history

### Step 13: Notify Stakeholders

Send deployment completion notification:
- Deployment completed successfully
- New version deployed: [version]
- Release notes link
- Any known issues or limitations

## Verification

### Success Criteria

- [ ] All health checks return 200 OK
- [ ] Error rate < 1% in Application Insights
- [ ] Response time p95 < 500ms
- [ ] No critical errors in logs
- [ ] Smoke tests pass
- [ ] User journeys work (sign-in, create session, play scenario)

### Monitoring Checklist

Monitor for the next 24 hours:
- [ ] Application Insights alerts
- [ ] Error rates
- [ ] Performance degradation
- [ ] User feedback/support tickets

## Rollback

If deployment fails, see [Emergency Rollback](./emergency-rollback.md) runbook.

Quick rollback:

```bash
# Trigger rollback workflow
gh workflow run mystira-app-api-rollback.yml \
  --ref main \
  -f target_slot=production \
  -f reason="Deployment failed - [reason]"
```

## Troubleshooting

### Issue: Health checks failing

**Symptoms**: `/health` endpoint returns non-200 status

**Resolution**:
1. Check Application Insights for errors
2. Verify database connectivity
3. Check blob storage connectivity
4. Review recent configuration changes

### Issue: High error rate

**Symptoms**: > 5% error rate in Application Insights

**Resolution**:
1. Check Application Insights for specific error types
2. Review recent code changes
3. Consider immediate rollback if critical

### Issue: Slow response times

**Symptoms**: p95 response time > 1000ms

**Resolution**:
1. Check database query performance
2. Verify cache is working
3. Check external service dependencies
4. Scale up resources if needed

## Post-Procedure

- [ ] Update deployment log
- [ ] Create incident report if issues occurred
- [ ] Update runbook if process changed
- [ ] Schedule retrospective if deployment had issues
