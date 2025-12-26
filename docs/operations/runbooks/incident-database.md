# Incident Response: Database Issues

**Severity**: Critical  
**Time to Complete**: 20-45 minutes  
**Prerequisites**: Database connectivity or performance issues detected

---

## Overview

This runbook guides you through responding to Azure Cosmos DB issues affecting the Mystira application.

## Alert Triggers

- Database health check failing
- Connection timeouts > 5% of requests
- Request Unit (RU) throttling detected
- Query latency > 1000ms p95

## Initial Assessment (5 minutes)

### Step 1: Verify Database Status

```bash
# Check API health endpoint
curl https://api.mystira.app/health/ready

# Check Application Insights
# Navigate to Dependencies → Cosmos DB
```

### Step 2: Check Azure Service Health

1. Open Azure Portal → Service Health
2. Check for Cosmos DB outages in your region
3. Review active incidents

### Step 3: Assess Impact

```kql
// Database operation failures
dependencies
| where timestamp > ago(15m)
| where type == "Azure DocumentDB"
| where success == false
| summarize count() by resultCode, name
```

## Common Scenarios

### Scenario A: Connection Failures

**Symptoms**: "Unable to connect to database" errors

**Resolution**:

```bash
# 1. Check firewall rules
az cosmosdb network-rule list \
  --resource-group rg-mystira-prod \
  --name cosmos-mystira-prod

# 2. Verify connection string
# Check Key Vault for connection string
az keyvault secret show \
  --vault-name kv-mystira-prod \
  --name CosmosDb-ConnectionString

# 3. Test connectivity from App Service
# Use Kudu console or Azure CLI
```

### Scenario B: RU Throttling (429 Errors)

**Symptoms**: High request unit consumption, 429 status codes

**Resolution**:

```bash
# 1. Check current RU usage
az cosmosdb show \
  --resource-group rg-mystira-prod \
  --name cosmos-mystira-prod \
  --query "capabilities[?name=='EnableServerless'].name"

# 2. Scale up throughput temporarily
az cosmosdb sql database throughput update \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb \
  --throughput 10000

# 3. Review query patterns
# Check for missing indexes or inefficient queries
```

Query to identify expensive operations:

```kql
dependencies
| where timestamp > ago(15m)
| where type == "Azure DocumentDB"
| where customDimensions has "RequestCharge"
| extend RequestCharge = toreal(customDimensions.RequestCharge)
| summarize avg(RequestCharge), max(RequestCharge) by name
| order by max_RequestCharge desc
```

### Scenario C: High Latency

**Symptoms**: Slow database queries, timeouts

**Resolution**:

```bash
# 1. Check indexing policy
az cosmosdb sql container show \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --database-name MystiraAppDb \
  --name GameSessions \
  --query "resource.indexingPolicy"

# 2. Review query patterns
# Look for cross-partition queries
```

Enable diagnostic logging:

```bash
az monitor diagnostic-settings create \
  --resource /subscriptions/{subscription-id}/resourceGroups/rg-mystira-prod/providers/Microsoft.DocumentDB/databaseAccounts/cosmos-mystira-prod \
  --name cosmos-diagnostics \
  --workspace /subscriptions/{subscription-id}/resourcegroups/rg-mystira-prod/providers/microsoft.operationalinsights/workspaces/law-mystira-prod \
  --logs '[{"category": "QueryRuntimeStatistics", "enabled": true}]'
```

### Scenario D: Data Corruption

**Symptoms**: Unexpected data values, missing fields

**Resolution**:

```bash
# 1. Identify affected documents
# Use Data Explorer in Azure Portal

# 2. Check recent deployments
# Possible migration script issue

# 3. Restore from backup if needed
# Use point-in-time restore feature

az cosmosdb sql database restore \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb \
  --restore-timestamp "2025-12-22T10:00:00Z"
```

### Scenario E: Partition Hot Spots

**Symptoms**: Some partition keys showing high RU usage

**Resolution**:

```kql
// Identify hot partitions
dependencies
| where timestamp > ago(15m)
| where type == "Azure DocumentDB"
| extend PartitionKey = tostring(customDimensions.PartitionKey)
| summarize count(), avg(duration) by PartitionKey
| order by count_ desc
```

**Mitigation**:
1. Review partition key strategy
2. Consider synthetic partition keys
3. Redistribute data if needed

## Emergency Actions

### Failover to Secondary Region

If primary region is unavailable:

```bash
# Initiate manual failover (if configured)
az cosmosdb failover-priority-change \
  --resource-group rg-mystira-prod \
  --name cosmos-mystira-prod \
  --failover-policies "South Africa North=0" "West Europe=1"
```

### Enable Read-Only Mode

If write operations are problematic:

```bash
# Update application configuration
# Set READ_ONLY_MODE=true
# This disables write operations temporarily
```

## Verification

### Success Criteria

- [ ] Database health check passing
- [ ] Connection success rate > 99%
- [ ] Query latency < 500ms p95
- [ ] No 429 (throttling) errors
- [ ] RU consumption within budget

### Monitoring Queries

```kql
// Check database health
dependencies
| where timestamp > ago(5m)
| where type == "Azure DocumentDB"
| summarize 
    SuccessRate = 100.0 * countif(success == true) / count(),
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95)
```

## Post-Incident

### Step 1: Document Root Cause

Create detailed incident report:
- What caused the issue
- When it started
- Impact assessment
- Resolution steps
- Prevention measures

### Step 2: Optimize Queries

Review and optimize problematic queries:
- Add missing indexes
- Optimize cross-partition queries
- Implement caching where appropriate

### Step 3: Update Monitoring

- [ ] Add alerts for similar issues
- [ ] Update RU budget if needed
- [ ] Adjust scaling policies

### Step 4: Review Capacity

```bash
# Analyze capacity needs
az cosmosdb sql database throughput show \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb
```

Consider:
- Switching to serverless vs provisioned throughput
- Enabling autoscale
- Partitioning strategy improvements

## Troubleshooting

### Issue: Cannot connect via Azure Portal

**Resolution**:
1. Check network connectivity
2. Verify firewall rules include Azure Portal IPs
3. Use private endpoint if configured

### Issue: Consistent high RU usage

**Resolution**:
1. Review query execution plans
2. Check for missing indexes
3. Implement query result caching
4. Consider serverless pricing model

### Issue: Slow writes

**Resolution**:
1. Check indexing policy (too many indexes slow writes)
2. Review document size (large documents take longer)
3. Check for conflicts in multi-region setup

## Related Documentation

- [SLO Definitions](../slo-definitions.md)
- [Disaster Recovery](./disaster-recovery.md)
- [Secret Rotation](../secret-rotation.md)

## Post-Procedure

- [ ] Complete incident report
- [ ] Update capacity planning
- [ ] Review and optimize queries
- [ ] Update monitoring alerts
- [ ] Schedule team postmortem
