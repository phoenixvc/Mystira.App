# Disaster Recovery Runbook

**Severity**: Critical
**Time to Complete**: Varies (30 min - 4 hours)
**Prerequisites**: Azure subscription access, GitHub access, Database backups

---

## Overview

This runbook covers recovery procedures for major infrastructure failures. It defines Recovery Time Objectives (RTO) and Recovery Point Objectives (RPO) for each component.

---

## Recovery Objectives

| Component | RTO | RPO | Backup Strategy |
|-----------|-----|-----|-----------------|
| **Cosmos DB** | 1 hour | 0 (continuous) | Continuous backup + periodic snapshots |
| **App Service** | 30 min | N/A | Redeploy from GitHub |
| **Blob Storage** | 2 hours | 24 hours | GRS replication |
| **Key Vault** | 15 min | N/A | Soft delete + purge protection |
| **DNS (Front Door)** | 15 min | N/A | Azure-managed redundancy |

---

## Disaster Scenarios

### Scenario 1: App Service Region Failure

**Symptoms**: Application unreachable, health checks failing across all instances

**Recovery Steps**:

1. **Verify Regional Outage**
   ```bash
   # Check Azure status
   curl -s https://status.azure.com/api/v2/summary.json | jq '.status'

   # Check specific region
   az resource list \
     --location "South Africa North" \
     --output table
   ```

2. **Activate DR Region** (if configured)
   ```bash
   # Update Front Door to point to DR region
   az network front-door backend-pool update \
     --front-door-name mys-prod-mystira-fd \
     --resource-group mys-prod-mystira-rg-glob \
     --name ApiBackendPool \
     --backends "[{\"address\":\"mys-prod-mystira-api-dr.azurewebsites.net\",\"weight\":100}]"
   ```

3. **If No DR Region**: Wait for Azure recovery or deploy to new region
   - Create new App Service in alternate region
   - Deploy from latest successful build
   - Update DNS/Front Door

---

### Scenario 2: Cosmos DB Data Loss

**Symptoms**: Missing data, data corruption, accidental deletion

**Recovery Steps**:

1. **Assess Damage**
   ```bash
   # Check container document count
   az cosmosdb sql container show \
     --account-name mys-prod-mystira-cosmos-san \
     --database-name MystiraAppDb \
     --name Accounts \
     --resource-group mys-prod-mystira-rg-san \
     --query documentCount
   ```

2. **Point-in-Time Restore** (Continuous Backup)
   ```bash
   # Restore to new account
   az cosmosdb restore \
     --name mys-prod-mystira-cosmos-restore \
     --resource-group mys-prod-mystira-rg-san \
     --account-name mys-prod-mystira-cosmos-san \
     --restore-timestamp "2025-12-22T10:00:00Z" \
     --location "South Africa North"
   ```

3. **Verify Restored Data**
   - Query key collections (Accounts, UserProfiles, Scenarios)
   - Compare document counts with expected values
   - Run data integrity checks

4. **Switch to Restored Database**
   - Update connection string in Key Vault
   - Restart App Service to pick up new config
   - Monitor for issues

---

### Scenario 3: Key Vault Compromise

**Symptoms**: Secrets exposed, unauthorized access detected

**Recovery Steps**:

1. **Immediate Actions**
   - Revoke compromised secrets
   - Rotate all secrets (see [Secret Rotation](../secret-rotation.md))
   - Review access logs

2. **Rotate Critical Secrets**
   ```bash
   # Rotate database credentials
   az keyvault secret set \
     --vault-name mys-prod-mystira-kv-san \
     --name CosmosDb-ConnectionString \
     --value "NEW_CONNECTION_STRING"

   # Rotate JWT keys
   # Generate new RSA keys and update
   ```

3. **Restart All Services**
   ```bash
   az webapp restart \
     --name mys-prod-mystira-api-san \
     --resource-group mys-prod-mystira-rg-san
   ```

4. **Audit and Report**
   - Review Key Vault audit logs
   - Document timeline of exposure
   - Report to security team

---

### Scenario 4: Complete Environment Loss

**Symptoms**: Resource group deleted, all resources unavailable

**Recovery Steps**:

1. **Recover from Soft Delete** (if within retention)
   ```bash
   # Check for deleted Key Vault
   az keyvault list-deleted --query "[?name=='mys-prod-mystira-kv-san']"

   # Recover Key Vault
   az keyvault recover --name mys-prod-mystira-kv-san
   ```

2. **Rebuild from Infrastructure-as-Code**
   ```bash
   # Navigate to workspace repo
   cd Mystira.workspace

   # Deploy infrastructure
   terraform init
   terraform apply -var-file="environments/prod.tfvars"
   ```

3. **Restore Data**
   - Restore Cosmos DB from continuous backup
   - Restore Blob Storage from GRS replica

4. **Redeploy Applications**
   - Trigger GitHub Actions deployment workflows
   - Configure app settings and secrets

---

## Communication Plan

### During Disaster

| Time | Action | Owner |
|------|--------|-------|
| 0 min | Acknowledge alert | On-call |
| 5 min | Assess severity | On-call |
| 15 min | Initial status update | Engineering Lead |
| 30 min | Update stakeholders | Product Manager |
| Hourly | Status updates | Engineering Lead |

### Status Page Updates

1. Go to status page admin
2. Create incident with appropriate severity
3. Update as recovery progresses
4. Resolve when fully recovered

---

## Post-Disaster

### Immediate (Within 24 hours)
- [ ] Verify all services healthy
- [ ] Check data integrity
- [ ] Review error rates in App Insights
- [ ] Document timeline of events

### Short-term (Within 1 week)
- [ ] Conduct post-mortem
- [ ] Identify improvement opportunities
- [ ] Update runbooks if needed
- [ ] Test recovery procedures

### Long-term
- [ ] Implement identified improvements
- [ ] Schedule disaster recovery drills
- [ ] Review and update RTO/RPO targets

---

## DR Testing Schedule

| Test Type | Frequency | Last Test | Next Test |
|-----------|-----------|-----------|-----------|
| Backup Verification | Monthly | - | - |
| Failover Drill | Quarterly | - | - |
| Full DR Test | Annually | - | - |

---

## Related

- [Secret Rotation](../secret-rotation.md)
- [Emergency Rollback](./emergency-rollback.md)
- [SLO Definitions](../slo-definitions.md)
