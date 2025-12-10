# Mystira.App - Consolidated Roadmap

**Last Updated**: 2025-12-10
**Status**: Active - Single source of truth for all pending improvements

This document consolidates all pending improvements, technical debt, and future work from various analysis documents throughout the codebase.

---

## Quick Navigation

- [Critical Priority (Week 1)](#critical-priority-week-1)
- [High Priority (Weeks 2-4)](#high-priority-weeks-2-4)
- [Medium Priority (Month 2-3)](#medium-priority-month-2-3)
- [Ongoing Initiatives](#ongoing-initiatives)
- [Technical Debt](#technical-debt)
- [DevHub Improvements](#devhub-improvements)

---

## Critical Priority (Week 1)

### Security & Legal Compliance

| ID | Item | Status | Notes |
|----|------|--------|-------|
| SEC-1 | Rotate all exposed secrets (Azure Cosmos DB, Storage, JWT) | Pending | See MASTER_SUMMARY_TABLE BUG-1 |
| SEC-2 | Remove hardcoded guest credentials from Admin API | Pending | BUG-NEW-2 |
| SEC-3 | Guard Swagger in production (IsDevelopment check) | Pending | BUG-NEW-1 |
| SEC-4 | Stop logging PII without redaction | Pending | BUG-4 |
| COPPA-1 | Implement COPPA compliance - Age gate | Not Started | Legal requirement |
| COPPA-2 | Implement parental consent system | Not Started | Legal requirement |

### Stability Fixes

| ID | Item | Status | Notes |
|----|------|--------|-------|
| STAB-1 | Implement health check endpoints for all APIs | Pending | |
| STAB-2 | Create public status page | Pending | |
| STAB-3 | Setup error tracking (Sentry/AppInsights) | Pending | |

---

## High Priority (Weeks 2-4)

### SDK & Dependencies

| ID | Item | Status | Notes |
|----|------|--------|-------|
| SDK-1 | Update global.json to SDK 9.0.100 | Pending | BUG-2 |
| SDK-2 | Upgrade EF Core to 9.x | Pending | BUG-8 |
| SDK-3 | Change Domain to target net9.0 | Pending | BUG-7 |

### Performance Optimization

| ID | Item | Status | Notes |
|----|------|--------|-------|
| PERF-1 | Enable Blazor AOT compilation | Pending | 50% bundle size reduction |
| PERF-2 | Enable IL linking | Pending | 30-50% size reduction |
| PERF-3 | Implement circuit breakers (Polly) | Pending | |
| PERF-4 | Setup CDN properly with cache headers | Pending | |
| PERF-5 | Implement rate limiting on auth endpoints | Partial | Needs stricter limits |

### Testing & Quality

| ID | Item | Status | Notes |
|----|------|--------|-------|
| TEST-1 | Increase test coverage to 30% minimum | Pending | Currently ~4% |
| TEST-2 | Implement test strategy (unit, integration, E2E) | Pending | |
| TEST-3 | Performance baseline & load testing | Pending | Target: 10K concurrent users |

---

## Medium Priority (Month 2-3)

### COPPA Compliance (Full Implementation)

| ID | Item | Status | Notes |
|----|------|--------|-------|
| COPPA-3 | Build Parent Dashboard | Not Started | Activity monitoring, privacy controls |
| COPPA-4 | Implement data deletion workflows | Not Started | <7 days SLA |
| COPPA-5 | Legal review and certification | Not Started | |

### Architecture Refactoring

See detailed plan in `docs/architecture/REFACTORING_PLAN.md` (1230 lines)

| Phase | Item | Effort | Status |
|-------|------|--------|--------|
| Phase 1 | Move repository interfaces to Application/Ports/Data | 2 weeks | Not Started |
| Phase 2 | Move infrastructure port interfaces to Application/Ports | 2 weeks | Not Started |
| Phase 3 | Remove infrastructure dependencies from Application layer | 3 weeks | Not Started |
| Phase 4 | Migrate 47 API services to Application/UseCases | 5 weeks | Not Started |
| Phase 5 | Migrate 41 Admin.Api services to Application/UseCases | 4 weeks | Not Started |
| Phase 6 | PWA model migration (use Contracts) | 2 weeks | Not Started |
| Phase 7 | Remove infrastructure references from API layers | 1 week | Not Started |
| Phase 8 | Testing & documentation | 2 weeks | Not Started |

**Key Files to Refactor:**
- 88 service files in API layers → Application/UseCases
- 12 repository files in API layers → Infrastructure.Data
- 138 infrastructure dependencies in Application layer

### Admin API Service Migration

From `DEPRECATED_SERVICES_ANALYSIS.md` - Admin controllers still use deprecated services:

| Service | Used By | Recommendation |
|---------|---------|----------------|
| IScenarioApiService | ScenariosController, AdminController | Migrate to CQRS |
| IAccountApiService | UserProfilesAdminController, GameSessionsController | Migrate to CQRS |
| IMediaApiService | MediaAdminController | Migrate to CQRS |
| IMediaMetadataService | MediaMetadataAdminController, AdminController | Migrate to CQRS |
| ICharacterMediaMetadataService | CharacterMediaMetadataAdminController | Create CQRS commands |
| ICharacterMapApiService | CharacterMapsAdminController | Migrate to CQRS |
| IAvatarApiService | AvatarAdminController | Migrate to CQRS |
| IBundleService | AdminController | Migrate to CQRS commands |
| IUserProfileApiService | UserProfilesAdminController | Migrate to CQRS |

### UX Improvements

| ID | Item | Status | Notes |
|----|------|--------|-------|
| UX-1 | Implement dark mode | Pending | |
| UX-2 | Add loading states/spinners | Pending | |
| UX-3 | Implement error boundaries | Pending | Prevent app crashes |
| UX-4 | WCAG 2.1 AA accessibility audit | Pending | |
| UX-5 | Add toast notification system | Pending | |

### Observability

| ID | Item | Status | Notes |
|----|------|--------|-------|
| OBS-1 | Comprehensive structured logging (Serilog) | Pending | |
| OBS-2 | Application Performance Monitoring | Pending | P99 < 2s target |
| OBS-3 | Alerting and monitoring dashboards | Pending | MTTD < 5 minutes |

---

## Ongoing Initiatives

### Security

| ID | Item | Frequency | Status |
|----|------|-----------|--------|
| SEC-O1 | Quarterly penetration testing | Quarterly | Not Started |
| SEC-O2 | Monthly vulnerability scanning | Monthly | Not Started |
| SEC-O3 | Continuous dependency scanning | Continuous | Not Started |

### Performance

| ID | Item | Frequency | Status |
|----|------|-----------|--------|
| PERF-O1 | Weekly load tests | Weekly | Not Started |
| PERF-O2 | Pre-release stress testing | Per Release | Not Started |

### Documentation

| ID | Item | Status | Notes |
|----|------|--------|-------|
| DOC-1 | Create Master PRD | Pending | |
| DOC-2 | API documentation enhancement | Pending | Swagger + examples |
| DOC-3 | Operational runbooks | Pending | |
| DOC-4 | Design system documentation | Pending | |

---

## Technical Debt

### Code Quality

| ID | Item | Priority | Notes |
|----|------|----------|-------|
| DEBT-1 | Services in API layer (architectural violation) | High | 88 files |
| DEBT-2 | Hardcoded config values in PasswordlessAuthService | Medium | Move to appsettings |
| DEBT-3 | Duplicated CORS config | Low | Extract to shared class |
| DEBT-4 | Long Program.cs files (400+ lines) | Low | Extract extension methods |
| DEBT-5 | Story Protocol stub - complete or remove | Medium | Incomplete feature |
| DEBT-6 | Character assignment not persisted | Medium | Data lost on refresh |
| DEBT-7 | Badge thresholds hardcoded | Low | Use BadgeConfigurationApiService |

### Security Headers

| ID | Item | Status | Notes |
|----|------|--------|-------|
| HDR-1 | Remove unsafe-inline from CSP | Pending | XSS vulnerability |
| HDR-2 | Fix overly permissive AllowedHosts | Pending | Host header injection |
| HDR-3 | YAML upload content validation | Pending | Injection risk |

---

## DevHub Improvements

From `tools/Mystira.DevHub/DRY_SOLID_ANALYSIS.md` and `docs/UX_IMPROVEMENTS.md`:

### High Priority

| ID | Item | Status | Notes |
|----|------|--------|-------|
| DH-1 | Split LogFilterBar.tsx (371 lines) | Pending | SRP violation |
| DH-2 | Extract shared error parsing utilities | Pending | DRY violation |
| DH-3 | Split Dashboard.tsx (340 lines) | Pending | |
| DH-4 | Log filtering and search | Pending | |
| DH-5 | Toast notifications | Pending | |
| DH-6 | Keyboard shortcuts | Pending | |
| DH-7 | Port conflict detection | Pending | |

### Medium Priority

| ID | Item | Status | Notes |
|----|------|--------|-------|
| DH-8 | Service presets/profiles | Pending | |
| DH-9 | Dark mode | Pending | |
| DH-10 | Settings panel | Pending | |
| DH-11 | Service details panel | Pending | |
| DH-12 | Activity timeline | Pending | |

### Rust Backend

| ID | Item | Status | Notes |
|----|------|--------|-------|
| DH-R1 | Split main.rs (2913 lines) into modules | Pending | azure.rs, github.rs, etc. |
| DH-R2 | Make subscription ID configurable | Pending | Currently hardcoded |
| DH-R3 | Increase health check timeout | Pending | 5s may be too short |
| DH-R4 | Case-insensitive workflow matching | Pending | |

---

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Uptime SLA | Unknown | 99.95% |
| P99 Latency | Unknown | < 2 seconds |
| Error Rate | Unknown | < 0.1% |
| Test Coverage | ~4% | 60%+ |
| COPPA Compliance | 0% | 100% |
| 7-Day User Retention | Unknown | 40% |
| Parent Consent Rate | Unknown | 95% |

---

## Document History

This roadmap consolidates items from:
- `/ANALYSIS.md` + `/SUMMARY.md` (archived)
- `/DEPRECATED_SERVICES_ANALYSIS.md` (active - migration pending)
- `docs/MASTER_SUMMARY_TABLE.md` (archived)
- `docs/POTENTIAL_ENHANCEMENTS_ROADMAP.md` (archived)
- `docs/PRODUCTION_REVIEW_REPORT.md` (archived)
- `docs/IMPROVEMENTS_ASSESSMENT.md` (archived)
- `docs/UX_IMPROVEMENTS.md` (archived)
- `docs/architecture/REFACTORING_PLAN.md` (active - detailed plan)
- `tools/Mystira.DevHub/*.md` (various analysis docs archived)

---

**Maintained By**: Development Team
**Next Review**: After completing Critical Priority items
