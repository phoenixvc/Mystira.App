# Incident Response: High Error Rate

**Severity**: High  
**Time to Complete**: 30-60 minutes  
**Prerequisites**: Alert triggered for elevated error rate

---

## Overview

This runbook guides you through responding to elevated error rates in the application. High error rates can indicate bugs, infrastructure issues, or external service problems.

## Alert Triggers

This runbook is triggered when:
- Error rate > 5% for 5 minutes
- Specific error types spike suddenly
- 50x errors > 1% of requests

## Initial Assessment (5 minutes)

### Step 1: Confirm the Alert

Verify the issue in Application Insights:

1. Open Azure Portal → Application Insights → Mystira.App.Api
2. Check **Failures** blade:
   - Current error rate
   - Error types
   - Affected endpoints
   - Geographic distribution

```bash
# Check health endpoints
curl https://api.mystira.app/health
curl https://api.mystira.app/health/ready
```

### Step 2: Determine Severity

Assess impact:

**Critical** (immediate action required):
- Error rate > 50%
- Authentication completely broken
- Data loss occurring
- All API endpoints failing

**High** (urgent action required):
- Error rate 10-50%
- Core functionality broken
- Significant user impact
- Payment processing failing

**Medium** (prompt action required):
- Error rate 5-10%
- Some features broken
- Moderate user impact
- Non-critical paths affected

### Step 3: Notify Team

```bash
# Post to incident channel
"🚨 High error rate detected: [X]% - Investigating"
```

## Investigation (10 minutes)

### Step 4: Identify Error Patterns

Check Application Insights for common errors:

```kql
// Top errors in last 15 minutes
exceptions
| where timestamp > ago(15m)
| summarize count() by type, outerMessage
| order by count_ desc
| take 10
```

### Step 5: Check Recent Deployments

```bash
# List recent deployments
gh run list --workflow=mystira-app-api-cicd-prod.yml --limit 5

# Check deployment time vs error spike
# If correlation found, proceed to rollback
```

### Step 6: Check Dependencies

Verify external services:

```bash
# Check Cosmos DB
curl https://api.mystira.app/health/ready

# Check Blob Storage
# Review Application Insights dependency failures

# Check Story Protocol gRPC
# Review gRPC client errors
```

### Step 7: Analyze Error Details

Look at specific error stack traces:

```kql
// Detailed error analysis
exceptions
| where timestamp > ago(15m)
| where type == "[TopErrorType]"
| project timestamp, outerMessage, innermostMessage, operation_Name, customDimensions
| take 50
```

## Resolution

### Scenario A: Recent Deployment Issue

If errors started after recent deployment:

```bash
# Immediate rollback
gh workflow run mystira-app-api-rollback.yml \
  --ref main \
  -f target_slot=production \
  -f reason="High error rate after deployment"
```

See [Emergency Rollback](./emergency-rollback.md) for full procedure.

### Scenario B: Database/External Service Issue

If database or external service is failing:

1. Check Azure Service Health
2. Scale up resources if needed:

```bash
# Scale up API instances
az appservice plan update \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --sku P2V2
```

3. Enable circuit breakers if available
4. Contact service provider support if external service issue

### Scenario C: Resource Exhaustion

If errors due to resource limits:

```bash
# Check memory/CPU usage
az monitor metrics list \
  --resource mys-prod-mystira-api-san \
  --resource-group rg-mystira-prod \
  --resource-type "Microsoft.Web/sites" \
  --metric MemoryPercentage,CpuPercentage

# Scale out if needed
az appservice plan update \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --number-of-workers 3
```

### Scenario D: Code Bug (Non-Deployment)

If error is code-related but not from recent deployment:

1. Identify the problematic code path
2. Implement workaround if possible:
   - Disable feature flag
   - Route around broken endpoint
3. Follow [Hotfix Process](./hotfix-process.md) for permanent fix

### Scenario E: Attack/Abuse

If error pattern suggests attack:

```bash
# Check for unusual traffic patterns
# Review rate limiting metrics
# Block offending IPs if identified

# Update rate limiting rules
# See RBAC and Security documentation
```

## Verification (5 minutes)

### Step 8: Confirm Resolution

```bash
# Check error rate decreased
# View Application Insights Failures blade

# Test affected endpoints
curl https://api.mystira.app/[affected-endpoint]

# Verify user reports
# Check support tickets
```

### Success Criteria

- [ ] Error rate < 5%
- [ ] Affected endpoints responding correctly
- [ ] No new errors introduced
- [ ] Root cause identified
- [ ] User impact minimized

## Post-Incident

### Step 9: Update Status

```bash
# Post resolution
"✅ Error rate resolved: [X]% → [Y]%. Root cause: [description]"
```

### Step 10: Create Incident Report

Document:
- **Timeline**: When error started, actions taken, resolution time
- **Root Cause**: What caused the errors
- **Impact**: Users affected, duration, severity
- **Resolution**: Actions taken to resolve
- **Prevention**: How to prevent recurrence

### Step 11: Implement Preventions

- [ ] Add tests to catch similar issues
- [ ] Update alerting thresholds
- [ ] Add monitoring for root cause
- [ ] Create runbook updates
- [ ] Schedule postmortem

## Troubleshooting

### Issue: Error rate not decreasing

**Resolution**:
1. Verify fix was applied correctly
2. Check if multiple issues present
3. Review dependency health
4. Consider additional rollback

### Issue: Errors intermittent

**Resolution**:
1. Check for rate limiting issues
2. Review load balancer behavior
3. Check database connection pool
4. Analyze error timing patterns

### Issue: Cannot identify root cause

**Resolution**:
1. Increase logging verbosity temporarily
2. Enable Application Insights profiler
3. Review correlation IDs for request tracing
4. Consult with team for additional context

## Post-Procedure

- [ ] Complete detailed incident report
- [ ] Update monitoring alerts
- [ ] Add preventive tests
- [ ] Update documentation
- [ ] Schedule team postmortem within 48 hours
