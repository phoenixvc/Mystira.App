# Staging SWA Monitoring Setup Guide

**Date**: 2025-12-08  
**Purpose**: Configure comprehensive monitoring for Staging Static Web App

---

## Overview

After migrating Staging to Azure Static Web Apps, proper monitoring ensures deployment health, performance tracking, and early issue detection.

---

## Phase 1: Application Insights Integration (15 min)

### Step 1: Create Application Insights Resource

**Azure Portal**:
1. Go to Azure Portal → Create a resource
2. Search "Application Insights" → Create
3. Configure:
   ```
   Resource Group: [Same as SWA]
   Name: mystira-app-staging-insights
   Region: Same as SWA
   Resource Mode: Workspace-based
   ```
4. Click "Review + Create" → "Create"

**Azure CLI**:
```bash
# Set variables
RESOURCE_GROUP="rg-mystira-app"
INSIGHTS_NAME="mystira-app-staging-insights"
LOCATION="southafricanorth"

# Create Application Insights
az monitor app-insights component create \
  --app $INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Get Instrumentation Key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

echo "Instrumentation Key: $INSTRUMENTATION_KEY"
```

### Step 2: Link SWA to Application Insights

**Azure Portal**:
1. Open your Static Web App resource
2. Go to Settings → Application Insights
3. Click "Enable"
4. Select the Application Insights resource created above
5. Click "Save"

**Azure CLI**:
```bash
SWA_NAME="mystira-app-staging-swa"

# Link Application Insights to SWA
az staticwebapp appsettings set \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --setting-names "APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
    --app $INSIGHTS_NAME \
    --resource-group $RESOURCE_GROUP \
    --query connectionString -o tsv)"
```

### Step 3: Verify Integration

1. Deploy to Staging (trigger workflow)
2. Wait 5 minutes for telemetry to appear
3. In Azure Portal → Application Insights → Live Metrics
4. You should see requests and dependencies

---

## Phase 2: Custom Alerts (15 min)

### Alert 1: Deployment Failures

**Purpose**: Notify when GitHub Actions deployment fails

**GitHub Actions Integration**:
```yaml
# Add to .github/workflows/azure-static-web-apps-staging.yml

- name: Notify on Failure
  if: failure()
  uses: azure/CLI@v1
  with:
    inlineScript: |
      az monitor metrics alert create \
        --name "staging-deployment-failed" \
        --resource-group ${{ env.RESOURCE_GROUP }} \
        --scopes /subscriptions/${{ secrets.AZURE_SUBSCRIPTION_ID }}/resourceGroups/${{ env.RESOURCE_GROUP }} \
        --condition "count > 0" \
        --description "Staging deployment failed"
```

**Or Create via Portal**:
1. Application Insights → Alerts → New alert rule
2. Condition: Custom log search
3. Query: `requests | where success == false | count`
4. Threshold: > 10 in 5 minutes
5. Action: Email notification

### Alert 2: High Error Rate

```bash
# Create alert for 4xx/5xx errors
az monitor metrics alert create \
  --name "staging-high-error-rate" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Insights/components/$INSIGHTS_NAME" \
  --condition "avg requests/failed > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --description "High error rate on Staging SWA"
```

### Alert 3: Slow Response Time

```bash
# Alert when response time exceeds threshold
az monitor metrics alert create \
  --name "staging-slow-response" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Insights/components/$INSIGHTS_NAME" \
  --condition "avg requests/duration > 3000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --description "Slow response times on Staging (>3s)"
```

---

## Phase 3: Azure Dashboard (20 min)

### Create Monitoring Dashboard

**Script**: `scripts/create-staging-dashboard.sh`

```bash
#!/bin/bash
# Create Azure Dashboard for Staging SWA monitoring

RESOURCE_GROUP="rg-mystira-app"
SWA_NAME="mystira-app-staging-swa"
INSIGHTS_NAME="mystira-app-staging-insights"
DASHBOARD_NAME="mystira-staging-dashboard"

# Dashboard JSON definition
cat > dashboard.json << 'EOF'
{
  "properties": {
    "lenses": {
      "0": {
        "order": 0,
        "parts": {
          "0": {
            "position": { "x": 0, "y": 0, "colSpan": 6, "rowSpan": 4 },
            "metadata": {
              "type": "Extension/AppInsightsExtension/PartType/MetricsChartPart",
              "settings": {
                "title": "Request Rate",
                "chartType": "Line"
              }
            }
          },
          "1": {
            "position": { "x": 6, "y": 0, "colSpan": 6, "rowSpan": 4 },
            "metadata": {
              "type": "Extension/AppInsightsExtension/PartType/MetricsChartPart",
              "settings": {
                "title": "Response Time",
                "chartType": "Line"
              }
            }
          },
          "2": {
            "position": { "x": 0, "y": 4, "colSpan": 6, "rowSpan": 4 },
            "metadata": {
              "type": "Extension/AppInsightsExtension/PartType/MetricsChartPart",
              "settings": {
                "title": "Failed Requests",
                "chartType": "Bar"
              }
            }
          },
          "3": {
            "position": { "x": 6, "y": 4, "colSpan": 6, "rowSpan": 4 },
            "metadata": {
              "type": "Extension/AppInsightsExtension/PartType/AvailabilityPart"
            }
          }
        }
      }
    },
    "metadata": {
      "model": {
        "timeRange": { "type": "MsPortalFx.Composition.Configuration.ValueTypes.TimeRange" }
      }
    }
  },
  "name": "$DASHBOARD_NAME",
  "type": "Microsoft.Portal/dashboards",
  "location": "eastus",
  "tags": { "hidden-title": "Mystira Staging Dashboard" }
}
EOF

# Create dashboard
az portal dashboard create \
  --name $DASHBOARD_NAME \
  --resource-group $RESOURCE_GROUP \
  --input-path dashboard.json

rm dashboard.json

echo "✅ Dashboard created: $DASHBOARD_NAME"
echo "   View at: https://portal.azure.com/#@/dashboard/private/$DASHBOARD_NAME"
```

### Key Metrics to Monitor

**Dashboard Tiles**:
1. **Request Rate** - Requests per minute
2. **Response Time** - Average/P95/P99
3. **Error Rate** - 4xx and 5xx errors
4. **Availability** - Uptime percentage
5. **Dependency Calls** - API calls to backend
6. **Browser Timing** - Client-side performance

---

## Phase 4: Cost Monitoring (10 min)

### Set Budget Alert

```bash
# Create budget for Staging SWA
az consumption budget create \
  --budget-name "staging-swa-monthly" \
  --amount 100 \
  --time-grain Monthly \
  --start-date $(date +%Y-%m-01) \
  --end-date $(date -d "+1 year" +%Y-%m-01) \
  --resource-group $RESOURCE_GROUP \
  --notifications \
    '{"Actual_GreaterThan_80_Percent":{"enabled":true,"operator":"GreaterThan","threshold":80,"contactEmails":["devops@mystira.app"]}}'
```

### Track Bandwidth Usage

SWA Free tier includes 100GB bandwidth/month. Monitor usage:

**Azure Portal**:
1. Static Web App → Monitoring → Metrics
2. Select: "Data Out" metric
3. Set: Monthly aggregation
4. Alert at: 80GB (80% of free tier)

---

## Phase 5: Synthetic Monitoring (15 min)

### Availability Tests

Create availability test in Application Insights:

**Portal**:
1. Application Insights → Availability
2. Add test:
   ```
   Test name: staging-homepage
   Test type: URL ping test
   URL: https://[your-staging-swa].azurestaticapps.net/
   Test frequency: Every 5 minutes
   Test locations: 5 locations
   Success criteria: HTTP 200, response time < 3s
   ```

**CLI**:
```bash
az monitor app-insights web-test create \
  --resource-group $RESOURCE_GROUP \
  --name "staging-availability-test" \
  --location "eastus" \
  --kind "ping" \
  --synthetic-monitor-id "staging-homepage" \
  --web-test "<WebTest>...</WebTest>" \
  --app-insights-resource-id "/subscriptions/$SUB_ID/resourceGroups/$RESOURCE_GROUP/providers/microsoft.insights/components/$INSIGHTS_NAME"
```

### Test Key Routes

Create availability tests for:
- `/` - Home page
- `/adventures` - Adventures page
- `/profile` - Profile page
- `/manifest.json` - PWA manifest
- `/service-worker.js` - Service worker

---

## Phase 6: Log Analytics (10 min)

### Custom Queries

Save useful KQL queries in Application Insights:

**Query 1: Failed Requests**
```kql
requests
| where success == false
| summarize count() by resultCode, url
| order by count_ desc
```

**Query 2: Slow Pages**
```kql
pageViews
| where duration > 3000
| summarize avg(duration), count() by name
| order by avg_duration desc
```

**Query 3: User Agents**
```kql
requests
| summarize count() by client_Browser
| order by count_ desc
```

Save these as "Saved Queries" for quick access.

---

## Validation Checklist

After setup, verify:

- [ ] Application Insights receiving telemetry
- [ ] Live Metrics showing real-time data
- [ ] Alerts configured and active
- [ ] Dashboard displaying all metrics
- [ ] Budget alert set
- [ ] Availability tests running
- [ ] Cost tracking enabled
- [ ] Team has access to monitoring resources

---

## Monitoring Dashboard

**Access**: https://portal.azure.com/#@/dashboard

**Key Metrics**:
- ✅ Request rate: < 10,000/day (free tier)
- ✅ Response time: < 3 seconds (P95)
- ✅ Error rate: < 1%
- ✅ Availability: > 99.9%
- ✅ Bandwidth: < 100GB/month (free tier)

---

## Troubleshooting

### No Telemetry Appearing

1. Check Application Insights connection string in SWA settings
2. Verify deployment was successful
3. Wait 5-10 minutes for initial telemetry
4. Check Live Metrics for immediate feedback

### Alerts Not Firing

1. Verify alert rule is enabled
2. Check threshold values are appropriate
3. Ensure action group is configured
4. Test with manual metric spike

### High Costs

1. Check bandwidth usage (free tier: 100GB/month)
2. Review Application Insights data retention (default: 90 days)
3. Consider moving to paid tier if needed

---

## Post-Migration Monitoring Plan

**Week 1**: Daily monitoring
- Check dashboard every day
- Review error logs
- Validate alert configuration
- Compare performance with old Staging

**Week 2-4**: Weekly monitoring
- Review weekly performance trends
- Adjust alert thresholds if needed
- Optimize based on patterns

**Ongoing**: Monthly reviews
- Cost analysis
- Performance trends
- Capacity planning

---

## Automated Monitoring Script

**Script**: `scripts/check-staging-health.sh`

```bash
#!/bin/bash
# Quick health check for Staging SWA

STAGING_URL="https://your-staging-swa.azurestaticapps.net"

echo "🏥 Staging Health Check"
echo "======================="
echo ""

# Check homepage
echo "📄 Testing homepage..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $STAGING_URL)
if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ Homepage: OK ($HTTP_CODE)"
else
    echo "❌ Homepage: FAILED ($HTTP_CODE)"
fi

# Check key routes
for route in "/adventures" "/profile" "/manifest.json"; do
    echo "📄 Testing $route..."
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$STAGING_URL$route")
    if [ "$HTTP_CODE" = "200" ]; then
        echo "✅ $route: OK ($HTTP_CODE)"
    else
        echo "❌ $route: FAILED ($HTTP_CODE)"
    fi
done

echo ""
echo "Health check complete!"
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-12-08  
**Next Review**: After Staging migration
