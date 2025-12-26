# Scale Resources Up/Down

**Severity**: Medium  
**Time to Complete**: 15-30 minutes  
**Prerequisites**: Performance issues or cost optimization needed

---

## Overview

This runbook guides you through scaling Azure resources up or down based on performance needs or cost optimization.

## When to Scale

### Scale Up (Increase Resources)

When you observe:
- CPU utilization consistently > 70%
- Memory utilization consistently > 80%
- Response times increasing (p95 > 1000ms)
- Request queue growing
- Database throttling (429 errors)

### Scale Down (Decrease Resources)

When you observe:
- CPU utilization consistently < 30%
- Memory utilization consistently < 40%
- Over-provisioned for current load
- Cost optimization initiatives

## App Service Scaling

### Scale Up (Vertical - Better Performance)

```bash
# Check current SKU
az appservice plan show \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --query "sku"

# Scale up to higher SKU
# Available: B1, B2, B3, S1, S2, S3, P1V2, P2V2, P3V2
az appservice plan update \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --sku P2V2
```

SKU Recommendations:
- **Development**: B1 (1 core, 1.75 GB RAM)
- **Staging**: P1V2 (2 cores, 3.5 GB RAM)
- **Production**: P2V2 (4 cores, 7 GB RAM) or higher
- **High Load**: P3V2 (8 cores, 14 GB RAM)

### Scale Out (Horizontal - More Instances)

```bash
# Check current instance count
az appservice plan show \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --query "sku.capacity"

# Scale out to more instances
az appservice plan update \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --number-of-workers 3
```

Instance Recommendations:
- **Development**: 1 instance
- **Staging**: 1-2 instances
- **Production**: 2-3 instances (minimum for HA)
- **High Traffic**: 3-5 instances

### Enable Autoscaling

```bash
# Create autoscale setting
az monitor autoscale create \
  --resource-group rg-mystira-prod \
  --resource /subscriptions/{subscription-id}/resourceGroups/rg-mystira-prod/providers/Microsoft.Web/serverfarms/asp-mystira-prod \
  --resource-type "Microsoft.Web/serverfarms" \
  --name autoscale-mystira-api \
  --min-count 2 \
  --max-count 5 \
  --count 2

# Add CPU-based scale rule
az monitor autoscale rule create \
  --resource-group rg-mystira-prod \
  --autoscale-name autoscale-mystira-api \
  --condition "CpuPercentage > 70 avg 5m" \
  --scale out 1

az monitor autoscale rule create \
  --resource-group rg-mystira-prod \
  --autoscale-name autoscale-mystira-api \
  --condition "CpuPercentage < 30 avg 10m" \
  --scale in 1
```

## Cosmos DB Scaling

### Manual Throughput Adjustment

```bash
# Check current throughput
az cosmosdb sql database throughput show \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb

# Update throughput (RU/s)
az cosmosdb sql database throughput update \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb \
  --throughput 10000
```

Throughput Recommendations:
- **Development**: 400 RU/s (minimum)
- **Staging**: 1000-2000 RU/s
- **Production**: 4000-10000 RU/s
- **High Load**: 10000+ RU/s or autoscale

### Enable Autoscale

```bash
# Enable autoscale
az cosmosdb sql database throughput migrate \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb \
  --throughput-type autoscale

# Set max throughput
az cosmosdb sql database throughput update \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb \
  --max-throughput 10000
```

## Redis Cache Scaling

```bash
# Check current cache SKU
az redis show \
  --name redis-mystira-prod \
  --resource-group rg-mystira-prod \
  --query "sku"

# Scale up cache
az redis update \
  --name redis-mystira-prod \
  --resource-group rg-mystira-prod \
  --sku Standard \
  --vm-size C2
```

Cache SKU Recommendations:
- **Development**: Basic C0 (250 MB)
- **Staging**: Standard C1 (1 GB)
- **Production**: Standard C2 (2.5 GB) or Premium P1 (6 GB)

## Verification

### After Scaling Up

Monitor for 15-30 minutes:

```kql
// Check CPU after scaling
performanceCounters
| where timestamp > ago(30m)
| where name == "% Processor Time"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart

// Check memory
performanceCounters
| where timestamp > ago(30m)
| where name == "Available Bytes"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart
```

Success Criteria:
- [ ] CPU utilization < 70%
- [ ] Memory utilization < 80%
- [ ] Response times improved (p95 < 500ms)
- [ ] No queued requests
- [ ] No errors introduced

### After Scaling Down

Monitor for 1-2 hours:

- [ ] No performance degradation
- [ ] Response times stable
- [ ] Error rate unchanged
- [ ] Resource utilization acceptable

## Rollback

If scaling causes issues:

```bash
# Revert App Service SKU
az appservice plan update \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --sku [PREVIOUS_SKU]

# Revert instance count
az appservice plan update \
  --name asp-mystira-prod \
  --resource-group rg-mystira-prod \
  --number-of-workers [PREVIOUS_COUNT]

# Revert Cosmos throughput
az cosmosdb sql database throughput update \
  --account-name cosmos-mystira-prod \
  --resource-group rg-mystira-prod \
  --name MystiraAppDb \
  --throughput [PREVIOUS_THROUGHPUT]
```

## Cost Impact

### Estimate Cost Changes

Before scaling, review pricing:
- App Service: https://azure.microsoft.com/pricing/details/app-service/
- Cosmos DB: https://azure.microsoft.com/pricing/details/cosmos-db/
- Redis Cache: https://azure.microsoft.com/pricing/details/cache/

Example cost changes:
- P1V2 → P2V2: ~$146/month → ~$292/month
- 2 instances → 3 instances: 1.5x cost
- Cosmos 4000 RU/s → 10000 RU/s: ~2.5x cost

## Best Practices

1. **Scale gradually**: Increase by one tier/instance at a time
2. **Monitor closely**: Watch metrics for 30 minutes after scaling
3. **Scale during low traffic**: Avoid peak hours when possible
4. **Document changes**: Record baseline metrics before/after
5. **Consider autoscale**: Enable for predictable patterns
6. **Review regularly**: Monthly capacity planning reviews

## Troubleshooting

### Issue: Scaling not improving performance

**Resolution**:
1. Check if bottleneck is elsewhere (database, external services)
2. Review code for inefficiencies
3. Enable Application Insights profiler
4. Check for connection pool limits

### Issue: Scaling causing errors

**Resolution**:
1. Check application logs for connection issues
2. Verify connection strings and configurations
3. Review stateful operations that may not scale horizontally
4. Check session management

## Related Documentation

- [Monitoring and Alerting](../slo-definitions.md)
- [Cost Management](../cost-optimization.md)
- [Performance Tuning](../performance-best-practices.md)

## Post-Procedure

- [ ] Update capacity planning documentation
- [ ] Document cost impact
- [ ] Update monitoring baselines
- [ ] Review autoscale policies
- [ ] Schedule regular capacity reviews
