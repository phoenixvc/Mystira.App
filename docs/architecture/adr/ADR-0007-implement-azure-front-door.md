# ADR-0007: Implement Azure Front Door for Edge Security

**Status**: Proposed

**Date**: 2025-12-07

**Deciders**: Architecture Team

**Tags**: infrastructure, security, azure, front-door, waf, networking

---

## Context

The Mystira.App application currently has its APIs directly exposed to the public internet without centralized security or traffic management. A production review identified several critical security gaps that need to be addressed.

### Current Architecture

```
┌─────────────┐
│   Internet  │
└──────┬──────┘
       │ Direct Access (No WAF)
       │
       ├──────────────────┬──────────────────┐
       │                  │                  │
       ▼                  ▼                  ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│  Static Web  │   │  Main API    │   │  Admin API   │
│  App (PWA)   │   │  :5260       │   │  :5261       │
└──────────────┘   └──────────────┘   └──────────────┘
```

### Problems with Current Approach

1. **No WAF Protection**
   - APIs vulnerable to OWASP Top 10 attacks
   - No SQL injection prevention at edge
   - No XSS protection before reaching application
   - No bot protection

2. **No DDoS Protection**
   - Basic Azure DDoS only (network layer)
   - No application-layer (L7) DDoS protection
   - Single point of failure

3. **Exposed API Endpoints**
   - APIs directly accessible on public internet
   - Each service has its own public IP
   - Multiple attack surfaces to manage

4. **Scattered CORS Configuration**
   - 6+ allowed origins across multiple config files
   - Inconsistent configuration between environments
   - Maintenance complexity

5. **No Centralized Traffic Analytics**
   - Cannot detect abuse patterns globally
   - No unified view of traffic
   - Difficult to identify attack trends

6. **Secrets in Configuration Files**
   - Connection strings in appsettings.json
   - Exposed in source control
   - Environment detection issues causing localhost connections

### Security Risk Assessment

| Risk | Current State | Likelihood | Impact | Severity |
|------|--------------|------------|--------|----------|
| DDoS Attack | No L7 protection | Medium | High | Critical |
| SQL Injection | No WAF | Medium | Critical | Critical |
| XSS Attacks | No WAF | Medium | High | Critical |
| Data Breach | Exposed APIs | Low | Critical | High |
| Service Unavailability | No edge protection | Medium | High | Critical |

### Considered Alternatives

1. **Application Gateway Only**
   - ✅ Regional WAF protection
   - ✅ Lower cost than Front Door
   - ❌ No global edge presence
   - ❌ Single region latency for global users
   - ❌ No CDN capabilities

2. **Cloudflare or External CDN**
   - ✅ Strong WAF and DDoS protection
   - ✅ Global edge network
   - ❌ Data leaves Azure ecosystem
   - ❌ Additional vendor management
   - ❌ Potential compliance issues

3. **Azure Front Door Standard** ⭐ **CHOSEN**
   - ✅ Global edge network with 118+ PoPs
   - ✅ Integrated WAF with OWASP rulesets
   - ✅ Native Azure integration
   - ✅ Simplified CORS management
   - ✅ Built-in SSL certificate management
   - ✅ Traffic analytics and monitoring
   - ✅ Caching for static assets
   - ❌ Additional cost (~R890/month)
   - ❌ Slight latency overhead (~20ms)

4. **Azure Front Door Premium + API Management**
   - ✅ All Front Door benefits
   - ✅ Full API gateway capabilities
   - ✅ Per-consumer rate limiting
   - ✅ API versioning and documentation
   - ❌ Significantly higher cost (~R3,400/month additional)
   - ❌ Over-engineered for current needs
   - ❌ No external API consumers yet

---

## Decision

We will implement **Azure Front Door Standard** as the centralized entry point for all Mystira.App traffic. This provides edge security, WAF protection, and traffic management without the complexity of a full API gateway.

### Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         INTERNET                                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    AZURE FRONT DOOR                              │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  SECURITY: WAF + DDoS + Rate Limiting + Bot Protection   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  ROUTING:                                                │   │
│  │    mystira.app/*       → Static Web App                  │   │
│  │    mystira.app/api/*   → Main API                        │   │
│  │    mystira.app/admin/* → Admin API                       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  Endpoint: mystira.app (single domain)                           │
└───────────────┬─────────────────┬─────────────────┬─────────────┘
                │                 │                 │
                ▼                 ▼                 ▼
         ┌──────────┐      ┌──────────┐      ┌──────────┐
         │  Static  │      │  Main    │      │  Admin   │
         │  Web App │      │  API     │      │  API     │
         └──────────┘      └──────────┘      └──────────┘
```

### Implementation Components

1. **Azure Front Door Profile** (Standard tier)
   - Single entry point for all traffic
   - Global edge network presence
   - Managed SSL certificates

2. **WAF Policy** (OWASP 3.2 ruleset)
   - SQL injection protection
   - XSS prevention
   - Protocol anomaly detection
   - Custom rules for app-specific threats

3. **Rate Limiting Rules**
   - Global: 1000 requests/minute per IP
   - Auth endpoints: 5 requests/minute per IP
   - API endpoints: 100 requests/minute per IP

4. **Origin Groups**
   - PWA Origin: Azure Static Web App
   - API Origin: Main API App Service
   - Admin Origin: Admin API App Service

5. **Routing Rules**
   - `/*` → PWA (default)
   - `/api/*` → Main API
   - `/adminapi/*` → Admin API (with additional IP restrictions)

6. **Caching Rules**
   - Static assets: 7 days
   - Media files: 30 days
   - API responses: No cache

### Phase 2 Considerations (Future)

When any of these triggers occur, implement API Management:
- First external partner integration request
- Mobile app development begins
- API monetization strategy approved
- Need for per-consumer rate limiting

---

## Consequences

### Positive Consequences ✅

1. **Enhanced Security**
   - WAF protection against OWASP Top 10
   - DDoS protection at L3-L7
   - Centralized rate limiting
   - Bot protection capabilities
   - Reduced attack surface (single entry point)

2. **Simplified Architecture**
   - Single domain for all services (mystira.app)
   - Centralized CORS configuration
   - Unified SSL certificate management
   - Simplified DNS management

3. **Improved Performance**
   - Global edge caching for static assets
   - Reduced latency for global users
   - Optimized routing through Azure backbone

4. **Better Observability**
   - Centralized traffic analytics
   - Attack pattern detection
   - Real-time monitoring dashboards
   - Unified logging

5. **Operational Benefits**
   - Backend services not directly exposed
   - Can update origins without DNS changes
   - Blue-green deployment support
   - Health probing and automatic failover

### Negative Consequences ❌

1. **Additional Cost**
   - ~R890/month increase (49% of current spend)
   - Mitigated by: Essential security investment

2. **Slight Latency Overhead**
   - ~20ms additional for API calls
   - Mitigated by: Offset by edge caching benefits for static content

3. **Configuration Complexity**
   - New infrastructure to manage
   - Learning curve for team
   - Mitigated by: Infrastructure as Code (Bicep templates)

4. **Vendor Lock-in**
   - Deeper Azure dependency
   - Mitigated by: Standard HTTP routing, can migrate if needed

---

## Implementation Plan

### Week 1: Foundation
- [ ] Create Azure Key Vault for secrets
- [ ] Migrate connection strings from appsettings
- [ ] Fix environment detection issues
- [ ] Create Bicep templates for Front Door

### Week 2: Staging Deployment
- [ ] Deploy Front Door to staging
- [ ] Configure WAF policy with OWASP rules
- [ ] Set up routing rules
- [ ] Test all endpoints through Front Door

### Week 3: Production Migration
- [ ] Deploy Front Door to production
- [ ] Update DNS records
- [ ] Configure monitoring and alerts
- [ ] Update PWA configuration

### Week 4: Optimization
- [ ] Tune WAF rules based on traffic
- [ ] Optimize caching policies
- [ ] Document incident response procedures
- [ ] Establish performance baselines

---

## Cost Analysis

| Current State | Monthly Cost (ZAR) |
|--------------|-------------------|
| Static Web Apps (2x) | R0 |
| App Service Plan (B1) | R700 |
| Cosmos DB (Serverless) | R800 |
| Storage Account | R100 |
| Application Insights | R200 |
| **Total** | **R1,800** |

| With Front Door | Monthly Cost (ZAR) |
|----------------|-------------------|
| Current services | R1,800 |
| Azure Front Door (Standard) | R800 |
| Azure Key Vault | R50 |
| DNS Zone | R40 |
| **Total** | **R2,690** |

**Increase**: +R890/month (+49%)

---

## Success Metrics

### Security
- [ ] Zero direct API access (all through Front Door)
- [ ] WAF blocking >95% of malicious requests
- [ ] No secrets in source control
- [ ] SSL/TLS A+ rating

### Performance
- [ ] API response time <500ms (p95)
- [ ] Front Door adds <50ms latency
- [ ] 99.9% uptime SLA met
- [ ] Cache hit ratio >80% for static assets

### Operational
- [ ] Centralized logging operational
- [ ] Alerts configured for anomalies
- [ ] Incident response procedures documented
- [ ] Team trained on Front Door management

---

## Related Decisions

- **ADR-0005**: Separate API and Admin API (both APIs as Front Door origins)
- **ADR-0008**: Separate Staging Environment (staging will have its own Front Door)

---

## References

- [Azure Front Door Documentation](https://learn.microsoft.com/azure/frontdoor/)
- [Azure WAF Documentation](https://learn.microsoft.com/azure/web-application-firewall/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Azure Well-Architected Framework - Security](https://learn.microsoft.com/azure/architecture/framework/security/)
- [Mystira Architecture Review Report](../../PRODUCTION_REVIEW_REPORT.md)

---

## Notes

- Start with Standard tier; upgrade to Premium only if API Management needed
- Monitor WAF false positives during first month
- Consider geo-filtering if user base is geographically limited
- Admin API should have additional IP restrictions via custom rules

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
