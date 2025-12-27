# Service Level Objectives (SLOs) and Indicators (SLIs)

**Status**: Active
**Last Updated**: 2025-12-22
**Owner**: Platform Team

---

## Overview

This document defines the Service Level Objectives (SLOs) and Service Level Indicators (SLIs) for Mystira.App services. These metrics guide operational decisions and alert thresholds.

---

## Core Services

### Mystira.App.Api (Backend API)

#### Availability SLO

| Metric | SLI Definition | SLO Target | Window |
|--------|---------------|------------|--------|
| **Availability** | `(successful_requests / total_requests) * 100` | ≥ 99.5% | 30 days |
| **Error Rate** | `(5xx_errors / total_requests) * 100` | ≤ 0.5% | 30 days |

**KQL Query (App Insights):**
```kql
requests
| where timestamp > ago(30d)
| summarize
    total = count(),
    successful = countif(success == true),
    availability = round(todouble(countif(success == true)) / count() * 100, 2)
```

#### Latency SLO

| Metric | SLI Definition | SLO Target | Window |
|--------|---------------|------------|--------|
| **P50 Latency** | 50th percentile response time | ≤ 100ms | 30 days |
| **P95 Latency** | 95th percentile response time | ≤ 500ms | 30 days |
| **P99 Latency** | 99th percentile response time | ≤ 1500ms | 30 days |

**KQL Query:**
```kql
requests
| where timestamp > ago(30d)
| summarize
    p50 = percentile(duration, 50),
    p95 = percentile(duration, 95),
    p99 = percentile(duration, 99)
```

#### Throughput SLO

| Metric | SLI Definition | SLO Target | Window |
|--------|---------------|------------|--------|
| **Capacity** | Peak requests per second | Handle ≥ 100 RPS | N/A |
| **Saturation** | CPU utilization under load | ≤ 80% at peak | Rolling |

---

### Game Session Service

#### Session Reliability

| Metric | SLI Definition | SLO Target | Window |
|--------|---------------|------------|--------|
| **Session Start Success** | `(sessions_started / session_start_attempts) * 100` | ≥ 99% | 7 days |
| **Session Completion** | `(sessions_completed / sessions_started) * 100` | ≥ 85% | 7 days |
| **Choice Response Time** | Time to process game choice | ≤ 200ms (P95) | 7 days |

**Custom Metric:**
```csharp
_customMetrics.TrackGameSessionStarted(scenarioId, profileId);
_customMetrics.TrackGameSessionCompleted(sessionId, scenarioId, duration, choicesMade);
```

---

### Authentication Service

#### Auth Reliability

| Metric | SLI Definition | SLO Target | Window |
|--------|---------------|------------|--------|
| **Login Success Rate** | `(successful_logins / total_login_attempts) * 100` | ≥ 99% | 7 days |
| **Token Refresh Success** | `(successful_refreshes / refresh_attempts) * 100` | ≥ 99.9% | 7 days |
| **Auth Latency** | Token validation time | ≤ 50ms (P95) | 7 days |

---

### Data Services (Cosmos DB)

#### Database Reliability

| Metric | SLI Definition | SLO Target | Window |
|--------|---------------|------------|--------|
| **Query Success Rate** | `(successful_queries / total_queries) * 100` | ≥ 99.9% | 30 days |
| **Query Latency** | Query response time | ≤ 100ms (P95) | 30 days |
| **RU Efficiency** | RU consumption vs provisioned | ≤ 70% average | Rolling |

---

### Blob Storage

#### Storage Reliability

| Metric | SLI Definition | SLO Target | Window |
|--------|---------------|------------|--------|
| **Upload Success Rate** | `(successful_uploads / total_uploads) * 100` | ≥ 99.9% | 30 days |
| **Download Latency** | First byte time | ≤ 200ms (P95) | 30 days |

---

## Error Budgets

### Calculation

```
Error Budget = 100% - SLO Target
Monthly Error Budget (minutes) = Total Minutes * (100% - SLO Target)
```

### Current Budgets

| Service | SLO | Error Budget (30 days) |
|---------|-----|------------------------|
| API Availability | 99.5% | 216 minutes (~3.6 hours) |
| Session Start | 99% | 432 minutes (~7.2 hours) |
| Auth Success | 99% | 432 minutes (~7.2 hours) |
| Database Queries | 99.9% | 43 minutes |

### Budget Consumption Alerts

| Threshold | Action |
|-----------|--------|
| 50% consumed | Notify engineering team |
| 75% consumed | Pause non-critical deployments |
| 90% consumed | Incident review required |
| 100% consumed | Freeze all changes, incident response |

---

## Alert Thresholds

Based on SLO definitions, configure the following alerts:

### Critical (Severity 1)

| Alert | Condition | Action |
|-------|-----------|--------|
| Availability Drop | < 99% over 5 min | Page on-call |
| Error Spike | > 5% 5xx rate over 5 min | Page on-call |
| Database Unavailable | Connection failures | Page on-call + DBA |

### Warning (Severity 2)

| Alert | Condition | Action |
|-------|-----------|--------|
| Latency Degradation | P95 > 1000ms over 10 min | Email team |
| Error Budget 50% | 50% monthly budget consumed | Email team |
| Session Failure Rate | > 5% failures over 15 min | Email team |

### Informational (Severity 3)

| Alert | Condition | Action |
|-------|-----------|--------|
| Traffic Spike | > 2x normal traffic | Log for analysis |
| Slow Queries | > 10 queries > 500ms | Dashboard |
| Cache Miss Rate | > 30% misses | Dashboard |

---

## Monitoring Dashboard Metrics

### Primary Panel: Health Overview
- Availability (current + trend)
- Error rate (current + trend)
- Active users
- Request rate

### Secondary Panel: Latency
- P50/P95/P99 latency charts
- Latency by endpoint
- Slow request table

### Tertiary Panel: Dependencies
- Cosmos DB latency and RU consumption
- Blob Storage operations
- External API health (Story Protocol, Entra External ID)

### Business Panel: User Engagement
- Active game sessions
- Session completions per hour
- User sign-ups/sign-ins
- Content plays

---

## Review Cadence

| Review Type | Frequency | Participants |
|-------------|-----------|--------------|
| SLO Status Check | Weekly | Engineering Lead |
| Error Budget Review | Monthly | Engineering + Product |
| SLO Revision | Quarterly | Engineering + Product + Leadership |

---

## Related Documents

- [Monitoring Implementation Plan](../monitoring-implementation-plan.md)
- [RBAC and Security](./rbac-and-managed-identity.md)
- [Secret Rotation](./secret-rotation.md)

---

## Revision History

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2025-12-22 | 1.0 | Platform Team | Initial SLO definitions |
