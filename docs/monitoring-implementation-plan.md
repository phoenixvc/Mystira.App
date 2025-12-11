# Application Monitoring Implementation Plan

**Created**: 2025-12-10
**Status**: In Progress
**Owner**: DevOps Team

---

## Overview

This document outlines the phased implementation of comprehensive application monitoring for Mystira.App across all environments (Dev, Staging, Production).

---

## Phase Summary

| Phase | Name | Priority | Estimated Files | Dependencies |
|-------|------|----------|-----------------|--------------|
| 1 | Foundation | Critical | 5 | None |
| 2 | Logging & Telemetry | High | 8 | Phase 1 |
| 3 | Alerting & Availability | High | 4 | Phase 1 |
| 4 | CI/CD Integration | Medium | 6 | Phase 1, 3 |
| 5 | Security Monitoring | High | 4 | Phase 2 |
| 6 | Dashboards & Cost | Medium | 3 | Phase 1, 3 |

---

## Phase 1: Foundation

**Goal**: Establish baseline Application Insights configuration and infrastructure.

### 1.1 Application Insights Configuration

**Files to modify:**
- `src/Mystira.App.Api/appsettings.json`
- `src/Mystira.App.Api/appsettings.Development.json` (create)
- `src/Mystira.App.Api/appsettings.Production.json` (create)
- `src/Mystira.App.Admin.Api/appsettings.json`

**Configuration to add:**
```json
{
  "ApplicationInsights": {
    "EnableAdaptiveSampling": true,
    "EnableDependencyTracking": true,
    "EnablePerformanceCounterCollectionModule": true,
    "EnableQuickPulseMetricStream": true,
    "EnableRequestTrackingTelemetryModule": true,
    "EnableEventCounterCollectionModule": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Azure.Core": "Warning",
      "Azure.Identity": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

### 1.2 NuGet Packages

**Packages to add:**
- `Microsoft.ApplicationInsights.AspNetCore` (if not present)
- `Microsoft.ApplicationInsights.DependencyCollector`

### 1.3 Program.cs Updates

**Add Application Insights service configuration:**
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableAdaptiveSampling = builder.Environment.IsProduction();
    options.EnableDependencyTrackingTelemetryModule = true;
    options.EnableQuickPulseMetricStream = true;
});
```

---

## Phase 2: Logging & Telemetry

**Goal**: Implement structured logging with Serilog and correlation ID propagation.

### 2.1 NuGet Packages

**Packages to add:**
- `Serilog.AspNetCore`
- `Serilog.Enrichers.Environment`
- `Serilog.Enrichers.Thread`
- `Serilog.Enrichers.CorrelationId`
- `Serilog.Sinks.ApplicationInsights`
- `Serilog.Settings.Configuration`

### 2.2 New Files

**`src/Mystira.App.Shared/Middleware/CorrelationIdMiddleware.cs`**
- Extracts/generates correlation ID from request headers
- Adds to HttpContext.Items and response headers
- Enriches log context

**`src/Mystira.App.Shared/Middleware/RequestLoggingMiddleware.cs`**
- Logs request start/end
- Captures request body (configurable)
- Captures response status and timing

**`src/Mystira.App.Shared/Telemetry/TelemetryInitializer.cs`**
- Adds custom dimensions (environment, version, feature flags)
- Sets cloud role name

**`src/Mystira.App.Shared/Telemetry/CustomMetrics.cs`**
- Business KPI tracking methods
- Game session metrics
- User engagement metrics

### 2.3 Configuration Updates

**appsettings.json Serilog section:**
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.ApplicationInsights"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithCorrelationId"],
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "ApplicationInsights", "Args": { "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights" }}
    ],
    "Properties": {
      "Application": "Mystira.App.Api"
    }
  }
}
```

### 2.4 Environment-Specific Log Levels

| Level | Development | Staging | Production |
|-------|-------------|---------|------------|
| Default | Debug | Information | Warning |
| Microsoft.AspNetCore | Information | Warning | Warning |
| Microsoft.EntityFrameworkCore | Information | Warning | Error |
| Application | Debug | Information | Information |

---

## Phase 3: Alerting & Availability

**Goal**: Create IaC modules for alerts, action groups, and availability tests.

### 3.1 New Bicep Modules

**`infrastructure/modules/action-group.bicep`**
- Email notification targets
- Webhook integration (optional)
- Logic App integration (optional)

**`infrastructure/modules/metric-alerts.bicep`**
- HTTP 5xx errors alert
- Response time alert (P95 > 3s)
- Availability alert (< 99%)
- Failed requests rate alert (> 10%)
- CPU/Memory alerts (> 80%)
- Health check failure alert

**`infrastructure/modules/availability-tests.bicep`**
- URL ping tests for key endpoints
- Multi-location testing
- Custom test frequency per environment

### 3.2 Alert Definitions

| Alert Name | Metric | Condition | Severity | Environments |
|------------|--------|-----------|----------|--------------|
| high-error-rate | requests/failed | > 5 in 5min | 1 | All |
| slow-response | requests/duration | P95 > 3000ms | 2 | Prod, Staging |
| low-availability | availabilityResults/availabilityPercentage | < 99% | 1 | Prod |
| high-cpu | Percentage CPU | > 80% for 5min | 2 | Prod |
| high-memory | Memory Percentage | > 80% for 5min | 2 | Prod |
| health-check-failed | Custom metric | > 3 failures | 1 | All |

### 3.3 Availability Test Endpoints

| Test Name | URL | Frequency | Locations |
|-----------|-----|-----------|-----------|
| api-health | /health | 5 min | 5 |
| api-ready | /health/ready | 5 min | 3 |
| pwa-home | / | 10 min | 5 |
| pwa-manifest | /manifest.json | 30 min | 3 |

### 3.4 Integration with main.bicep

- Add module references
- Add environment-specific parameters
- Configure action group email addresses

---

## Phase 4: CI/CD Integration

**Goal**: Add deployment annotations, smoke tests, and deployment metrics.

### 4.1 Deployment Annotations

**Add to all deployment workflows:**
- Create App Insights release annotation
- Include commit SHA, branch, deployer
- Track deployment duration

### 4.2 Smoke Tests

**Create reusable workflow: `.github/workflows/templates/smoke-tests.yml`**
- Health endpoint validation
- Key route validation
- Configurable retry logic

### 4.3 Deployment Metrics

**Track in Application Insights:**
- Deployment frequency
- Deployment duration
- Deployment success rate
- Time to deploy (commit to live)

### 4.4 Files to Modify

- `.github/workflows/mystira-app-api-cicd-dev.yml`
- `.github/workflows/mystira-app-api-cicd-staging.yml`
- `.github/workflows/mystira-app-api-cicd-prod.yml`
- `.github/workflows/mystira-app-admin-api-cicd-*.yml`
- `.github/workflows/mystira-app-pwa-cicd-*.yml`

---

## Phase 5: Security Monitoring

**Goal**: Implement security-focused monitoring and alerting.

### 5.1 Custom Security Metrics

**`src/Mystira.App.Shared/Telemetry/SecurityMetrics.cs`**
- Failed authentication attempts
- Rate limit hits
- Suspicious request patterns
- Token validation failures

### 5.2 Security Alerts

| Alert Name | Condition | Action |
|------------|-----------|--------|
| brute-force-detected | > 10 failed auth in 1min | Email + Log |
| rate-limit-sustained | > 100 rate limits in 5min | Email |
| jwt-validation-spike | > 20 failures in 5min | Email |

### 5.3 Key Vault Auditing

**Enable diagnostic settings:**
- Audit events to Log Analytics
- Alert on unusual access patterns
- Track secret access by application

### 5.4 KQL Queries for Security

**Save as workbook queries:**
```kql
// Failed authentication attempts by IP
customEvents
| where name == "AuthenticationFailed"
| summarize count() by client_IP, bin(timestamp, 1h)
| order by count_ desc

// Rate limit hits
customMetrics
| where name == "RateLimitHit"
| summarize count() by cloud_RoleInstance, bin(timestamp, 5m)
```

---

## Phase 6: Dashboards & Cost

**Goal**: Create monitoring dashboards and cost management alerts.

### 6.1 Azure Dashboard

**`infrastructure/modules/dashboard.bicep`**
- Request rate chart
- Response time chart (avg, P95, P99)
- Error rate chart
- Availability percentage
- Active users
- Top errors table

### 6.2 Workbooks

**`infrastructure/modules/workbook.bicep`**
- Performance analysis workbook
- Error analysis workbook
- User journey workbook

### 6.3 Cost Management

**`infrastructure/modules/budget.bicep`**
- Monthly budget per environment
- Alert at 50%, 80%, 100% thresholds
- Email notifications

### 6.4 Budget Configuration

| Environment | Monthly Budget | Alert Contacts |
|-------------|----------------|----------------|
| Dev | $50 | devops@mystira.app |
| Staging | $100 | devops@mystira.app |
| Prod | $500 | devops@mystira.app, finance@mystira.app |

---

## Implementation Checklist

### Phase 1: Foundation
- [ ] Update appsettings.json with ApplicationInsights config
- [ ] Create environment-specific appsettings files
- [ ] Add Application Insights service configuration to Program.cs
- [ ] Verify telemetry is flowing to App Insights

### Phase 2: Logging & Telemetry
- [ ] Add Serilog NuGet packages
- [ ] Create CorrelationIdMiddleware
- [ ] Create RequestLoggingMiddleware
- [ ] Create TelemetryInitializer
- [ ] Create CustomMetrics service
- [ ] Update Program.cs with Serilog configuration
- [ ] Add environment-specific log levels

### Phase 3: Alerting & Availability
- [ ] Create action-group.bicep
- [ ] Create metric-alerts.bicep
- [ ] Create availability-tests.bicep
- [ ] Update main.bicep to include new modules
- [ ] Add alert parameters to params.*.json
- [ ] Deploy and verify alerts

### Phase 4: CI/CD Integration
- [ ] Create smoke-tests template workflow
- [ ] Add deployment annotations to API workflows
- [ ] Add deployment annotations to Admin API workflows
- [ ] Add deployment annotations to PWA workflows
- [ ] Add smoke tests to all deployment workflows

### Phase 5: Security Monitoring
- [ ] Create SecurityMetrics service
- [ ] Add security event tracking to auth flows
- [ ] Create security-specific alerts
- [ ] Enable Key Vault diagnostic settings
- [ ] Create security KQL queries

### Phase 6: Dashboards & Cost
- [ ] Create dashboard.bicep
- [ ] Create workbook.bicep
- [ ] Create budget.bicep
- [ ] Update main.bicep
- [ ] Deploy dashboards and budgets

---

## Rollout Strategy

1. **Phase 1-2**: Deploy to Dev first, validate for 2-3 days
2. **Phase 3**: Deploy alerts to all environments simultaneously
3. **Phase 4**: Roll out CI/CD changes per workflow
4. **Phase 5**: Deploy security monitoring to Prod first
5. **Phase 6**: Deploy dashboards to all environments

---

## Success Criteria

| Metric | Target |
|--------|--------|
| Telemetry coverage | 100% of API requests tracked |
| Alert response time | < 5 minutes for critical alerts |
| Log correlation | 100% requests have correlation ID |
| Availability monitoring | All critical endpoints monitored |
| Security visibility | All auth failures tracked |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-10 | Claude | Initial plan |
