# Monitoring Implementation Plan

**Status**: In Progress  
**Owner**: Platform Team  
**Last Updated**: 2025-12-22

---

## Overview

This document outlines the implementation plan for comprehensive monitoring and observability across the Mystira platform, aligned with SLO definitions.

## Current State

### Implemented
- ✅ Application Insights integration
- ✅ Health check endpoints (`/health`, `/health/ready`, `/health/live`)
- ✅ Custom metrics tracking (business KPIs)
- ✅ Security metrics tracking
- ✅ User journey analytics
- ✅ Distributed tracing foundation
- ✅ Serilog structured logging

### In Progress
- 🔄 SLO-based alerting rules
- 🔄 Operational dashboards
- 🔄 Log Analytics workbook definitions
- 🔄 Availability tests via Terraform

### Not Started
- ⏳ Cost monitoring alerts
- ⏳ Application Map optimization
- ⏳ Profiling for performance bottlenecks
- ⏳ Custom availability tests for critical user journeys

---

## Phase 1: Foundation (Complete)

### Application Insights Setup
- [x] Configure Application Insights for all services
- [x] Enable adaptive sampling in production
- [x] Configure cloud role names for Application Map
- [x] Set up custom dimensions for filtering

### Health Checks
- [x] Implement health check endpoints
- [x] Add database health checks
- [x] Add blob storage health checks
- [x] Add Redis cache health checks
- [x] Add Discord bot health checks (optional)

### Structured Logging
- [x] Configure Serilog
- [x] Add correlation IDs
- [x] Implement request logging middleware
- [x] Add security event logging

---

## Phase 2: Metrics & Alerting (In Progress)

### Custom Metrics Implementation

**Business Metrics:**
```csharp
// Track via ICustomMetrics
- Game sessions started/completed
- User sign-ups/sign-ins
- Content plays
- Badge awards
- Scenario views
```

**Technical Metrics:**
```csharp
// Track via ICustomMetrics
- Cache hit/miss rates
- Database query performance
- Blob storage operations
- Token refresh success/failure
- Dual-write failures (polyglot)
```

### Alert Rules (KQL Queries)

#### 1. High Error Rate Alert
```kql
// Alert when error rate > 5% for 5 minutes
requests
| where timestamp > ago(5m)
| summarize 
    TotalRequests = count(),
    FailedRequests = countif(success == false)
| extend ErrorRate = 100.0 * FailedRequests / TotalRequests
| where ErrorRate > 5
```

#### 2. Slow Response Time Alert
```kql
// Alert when p95 response time > 1000ms
requests
| where timestamp > ago(5m)
| summarize P95 = percentile(duration, 95)
| where P95 > 1000
```

#### 3. Database Throttling Alert
```kql
// Alert on Cosmos DB 429 errors
dependencies
| where timestamp > ago(5m)
| where type == "Azure DocumentDB"
| where resultCode == "429"
| summarize Count = count()
| where Count > 10
```

#### 4. Authentication Failures Alert
```kql
// Alert on auth endpoint failures
customEvents
| where timestamp > ago(5m)
| where name == "Auth.SignIn.Failed" or name == "Auth.TokenValidation.Failed"
| summarize Count = count()
| where Count > 50
```

#### 5. Dual-Write Failures Alert
```kql
// Alert on polyglot persistence failures
customEvents
| where timestamp > ago(10m)
| where name == "Polyglot.DualWrite.Failed"
| summarize Count = count() by tostring(customDimensions.EntityType)
| where Count > 5
```

### Implementing Alerts via Terraform

```hcl
# Example: High error rate alert
resource "azurerm_monitor_metric_alert" "high_error_rate" {
  name                = "high-error-rate"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights.main.id]
  description         = "Alert when error rate exceeds 5%"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT5M"

  criteria {
    metric_namespace = "Microsoft.Insights/components"
    metric_name      = "requests/failed"
    aggregation      = "Count"
    operator         = "GreaterThan"
    threshold        = 5
  }

  action {
    action_group_id = azurerm_monitor_action_group.platform_team.id
  }
}
```

---

## Phase 3: Dashboards (Planned)

### 1. Executive Dashboard
**Purpose**: High-level business metrics  
**Widgets**:
- Active users (daily/monthly)
- Game sessions created/completed
- Content engagement rates
- User sign-ups trend
- Platform availability (SLO)

### 2. Operations Dashboard
**Purpose**: Technical health monitoring  
**Widgets**:
- Request volume and latency
- Error rate by endpoint
- Database performance (RU/s, latency)
- Cache hit rates
- API instance health

### 3. Security Dashboard
**Purpose**: Security event monitoring  
**Widgets**:
- Failed authentication attempts
- Rate limiting violations
- Token validation failures
- Suspicious activity patterns
- COPPA compliance events

### 4. Cost Management Dashboard
**Purpose**: Resource cost tracking  
**Widgets**:
- Cost by service (daily/monthly)
- Cosmos DB RU consumption
- Blob storage usage
- App Service resource utilization
- Cost trend and forecast

### Dashboard Creation via Bicep

```bicep
resource dashboard 'Microsoft.Portal/dashboards@2020-09-01-preview' = {
  name: 'mystira-ops-dashboard'
  location: resourceGroup().location
  properties: {
    lenses: [
      {
        order: 0
        parts: [
          // Error rate widget
          // Response time widget
          // etc.
        ]
      }
    ]
  }
}
```

---

## Phase 4: Availability Tests (Planned)

### Synthetic Monitoring

Implement availability tests for critical user journeys:

1. **User Sign-In Flow**
   - Test passwordless sign-in
   - Verify token issuance
   - Check token refresh

2. **Game Session Creation**
   - Create new session
   - Select character
   - Make first choice

3. **Content Delivery**
   - Retrieve scenario list
   - Load scenario details
   - Access media assets

### Implementation via Terraform

```hcl
resource "azurerm_application_insights_web_test" "user_signin" {
  name                    = "user-signin-test"
  location                = var.location
  resource_group_name     = var.resource_group_name
  application_insights_id = azurerm_application_insights.main.id
  kind                    = "ping"
  frequency               = 300
  timeout                 = 30
  enabled                 = true
  geo_locations           = ["emea-nl-ams-azr", "us-ca-sjc-azr", "apac-sg-sin-azr"]

  configuration = <<XML
<WebTest Name="UserSignIn" Enabled="True" Timeout="30">
  <Items>
    <Request Method="POST" Url="https://api.mystira.app/api/auth/signin"
             Headers="Content-Type: application/json" 
             Body='{"email":"test@example.com","method":"passwordless"}' />
  </Items>
  <ValidationRules>
    <ValidationRule Classname="Microsoft.VisualStudio.TestTools.WebTesting.Rules.ValidationRuleFindText">
      <RuleParameters>
        <RuleParameter Name="FindText" Value="success" />
      </RuleParameters>
    </ValidationRule>
  </ValidationRules>
</WebTest>
XML
}
```

---

## Phase 5: Cost Monitoring (Planned)

### Cost Alerts

1. **Daily Cost Threshold**
   - Alert when daily cost exceeds $50
   - Notification to platform team

2. **Monthly Budget Alert**
   - Alert at 80% and 100% of monthly budget
   - Escalation to finance team at 100%

3. **Anomaly Detection**
   - Alert on unexpected cost spikes
   - Automatic investigation triggers

### Cost Optimization Queries

```kql
// Identify expensive queries
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.DOCUMENTDB"
| where Category == "QueryRuntimeStatistics"
| extend RequestCharge = todouble(requestCharge_s)
| summarize TotalRU = sum(RequestCharge) by QueryText = querytext_s
| order by TotalRU desc
| take 20
```

---

## Phase 6: Advanced Observability (Future)

### Distributed Tracing Enhancement
- Full OpenTelemetry integration
- Cross-service trace correlation
- Performance profiling integration

### Predictive Analytics
- ML-based anomaly detection
- Capacity forecasting
- User behavior prediction

### Chaos Engineering
- Automated resilience testing
- Failure injection experiments
- Recovery time measurement

---

## Implementation Timeline

| Phase | Start Date | Target Completion | Status |
|-------|-----------|-------------------|---------|
| Phase 1: Foundation | 2025-Q3 | 2025-Q4 | ✅ Complete |
| Phase 2: Metrics & Alerting | 2025-Q4 | 2026-Q1 | 🔄 In Progress |
| Phase 3: Dashboards | 2026-Q1 | 2026-Q1 | ⏳ Planned |
| Phase 4: Availability Tests | 2026-Q1 | 2026-Q2 | ⏳ Planned |
| Phase 5: Cost Monitoring | 2026-Q2 | 2026-Q2 | ⏳ Planned |
| Phase 6: Advanced Observability | 2026-Q3 | 2026-Q4 | ⏳ Future |

---

## Success Criteria

- [ ] All SLO metrics tracked and alerted
- [ ] Mean Time To Detection (MTTD) < 5 minutes
- [ ] Mean Time To Resolution (MTTR) < 30 minutes
- [ ] 99.9% availability achieved
- [ ] Cost visibility for all services
- [ ] Automated incident response for common scenarios

---

## References

- [SLO Definitions](./slo-definitions.md)
- [Operational Runbooks](./runbooks/README.md)
- [Azure Monitoring Best Practices](https://learn.microsoft.com/azure/azure-monitor/best-practices)
- [Application Insights Documentation](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
