# Emergency Rollback Runbook

**Severity**: Critical
**Time to Complete**: 5-15 minutes
**Prerequisites**: Azure CLI access, GitHub Actions permissions

---

## Overview

Use this runbook when a production deployment causes issues and needs to be immediately reverted. The blue-green deployment strategy allows fast rollback by swapping slots.

---

## Prerequisites

- [ ] Azure CLI installed and authenticated
- [ ] Access to GitHub repository
- [ ] Azure subscription access (Contributor role on resource group)
- [ ] Confirmation that rollback is necessary (verified by 2+ team members for production)

---

## Decision Tree

```
Is the issue affecting users?
├── YES → Proceed with immediate rollback
└── NO → Is error rate > 5%?
    ├── YES → Proceed with rollback
    └── NO → Monitor and investigate first
```

---

## Option 1: GitHub Workflow (Recommended)

### Step 1: Navigate to GitHub Actions

1. Go to https://github.com/phoenixvc/Mystira.App/actions
2. Find "API Rollback - Production" workflow
3. Click "Run workflow"

### Step 2: Confirm Rollback

1. Select `rollback_type`: `swap-slots`
2. Type `ROLLBACK` in the confirmation field
3. Click "Run workflow"

### Step 3: Monitor Progress

1. Watch the workflow execution
2. Verify health checks pass after swap
3. Check Application Insights for error rate changes

---

## Option 2: Azure CLI (Emergency)

Use this if GitHub Actions is unavailable.

### Step 1: Login to Azure

```bash
az login
az account set --subscription "Mystira Production"
```

### Step 2: Verify Current State

```bash
# Check production slot
az webapp show \
  --name mys-prod-mystira-api-san \
  --resource-group mys-prod-mystira-rg-san \
  --query "{state:state,lastModified:lastModifiedTimeUtc}"

# Check staging slot (previous version)
az webapp show \
  --name mys-prod-mystira-api-san \
  --resource-group mys-prod-mystira-rg-san \
  --slot staging \
  --query "{state:state,lastModified:lastModifiedTimeUtc}"
```

### Step 3: Perform Slot Swap

```bash
az webapp deployment slot swap \
  --name mys-prod-mystira-api-san \
  --resource-group mys-prod-mystira-rg-san \
  --slot staging \
  --target-slot production
```

### Step 4: Verify Rollback

```bash
# Check health endpoint
curl -s -o /dev/null -w "%{http_code}" \
  https://mys-prod-mystira-api-san.azurewebsites.net/health

# Check readiness
curl -s -o /dev/null -w "%{http_code}" \
  https://mys-prod-mystira-api-san.azurewebsites.net/health/ready
```

---

## Verification

After rollback, verify the following:

### Health Checks
- [ ] `/health` returns 200
- [ ] `/health/ready` returns 200

### Error Rates
- [ ] 5xx error rate < 0.5%
- [ ] No new exceptions in App Insights

### User Impact
- [ ] Users can log in
- [ ] Game sessions can be started
- [ ] No customer complaints

### Monitoring
- [ ] Check Application Insights for improvement
- [ ] Review deployment annotation in App Insights

---

## If Rollback Fails

### Slot Swap Fails

1. Check if staging slot exists and is running
2. Verify Azure credentials are valid
3. Check for deployment locks on the resource

```bash
# List locks
az lock list \
  --resource-group mys-prod-mystira-rg-san \
  --output table
```

### Health Check Still Failing After Swap

1. The staging slot may also have issues
2. Check if issue is in shared infrastructure (Cosmos DB, Key Vault)
3. Consider manual deployment of known-good version

```bash
# Deploy specific version from release tag
git checkout v1.2.3  # Known good version
dotnet publish -c Release
# Deploy via Azure CLI or portal
```

---

## Post-Rollback

### Immediate Actions
- [ ] Notify team in #incident channel
- [ ] Update status page if applicable
- [ ] Create incident ticket

### Within 1 Hour
- [ ] Root cause analysis started
- [ ] Identify what changed in failed deployment
- [ ] Plan fix for underlying issue

### Within 24 Hours
- [ ] Incident report drafted
- [ ] Fix deployed to staging for testing
- [ ] Post-mortem scheduled if major incident

---

## Escalation

| Escalation Level | Condition | Contact |
|-----------------|-----------|---------|
| L1 | Standard rollback needed | On-call engineer |
| L2 | Rollback fails or issue persists | Engineering lead |
| L3 | Extended outage > 30 min | CTO + Status page update |

---

## Related

- [Production Deployment](./production-deployment.md)
- [SLO Definitions](../slo-definitions.md)
- [Blue-Green Workflow](.github/workflows/mystira-app-api-cicd-prod.yml)
