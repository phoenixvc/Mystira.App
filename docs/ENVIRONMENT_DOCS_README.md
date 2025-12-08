# Environment Strategy & Migration Documentation

**Last Updated**: 2025-12-08  
**Status**: Active Documentation

---

## Quick Navigation

### 📋 **Start Here**
- **[CORRECTED_ENVIRONMENT_STRATEGY.md](CORRECTED_ENVIRONMENT_STRATEGY.md)** - Current environment architecture and strategy (All-SWA Standardization)

### 🚀 **Migration Guide**
- **[STAGING_MIGRATION_EXECUTION.md](STAGING_MIGRATION_EXECUTION.md)** - Step-by-step execution checklist (recommended)
- **[STAGING_MIGRATION_GUIDE.md](STAGING_MIGRATION_GUIDE.md)** - Detailed migration reference

### 🔍 **Analysis & Issues**
- **[PR_ANALYSIS.md](PR_ANALYSIS.md)** - Complete PR analysis with 10 issues identified and resolution status

---

## Current Environment State

| Environment | Platform | Status | Workflow |
|-------------|----------|--------|----------|
| **Dev** | Azure Static Web Apps | ✅ Current | `azure-static-web-apps-dev-san-swa-mystira-app.yml` |
| **Staging** | Azure Static Web Apps | 📋 Ready for migration | `azure-static-web-apps-staging.yml` |
| **Production** | Azure Static Web Apps | ✅ Current | `azure-static-web-apps-blue-water-0eab7991e.yml` |

**Migration Needed**: Only Staging (Dev and Prod already on SWA)

---

## What This PR Fixes

1. ✅ **Email Service Consistency** - Admin API now registers email service
2. ✅ **Environment Strategy** - Corrected documentation (Dev already uses SWA)
3. ✅ **Migration Tooling** - Complete workflow and guides for Staging migration
4. ✅ **PR Preview Tests** - Automated SWA validation on every PR

---

## Documentation Consolidation

**Active Documents** (Use these):
- `CORRECTED_ENVIRONMENT_STRATEGY.md` - Single source of truth for strategy
- `STAGING_MIGRATION_EXECUTION.md` - Practical execution guide
- `STAGING_MIGRATION_GUIDE.md` - Detailed reference
- `PR_ANALYSIS.md` - Issues and improvements

**Archived Documents** (Historical reference):
- `archive/ENVIRONMENT_PARITY_ANALYSIS.md` - Initial analysis (superseded)
- `archive/FINALIZED_ENVIRONMENT_DECISION.md` - Original strategy (contained error)
- `archive/TASK_COMPLETION_SUMMARY.md` - Task summary (info now in PR_ANALYSIS)
- `archive/EXECUTIVE_SUMMARY.md` - Executive summary (info now in CORRECTED_ENVIRONMENT_STRATEGY)

---

## Cost Impact

**Net Savings**: R350/month (~R4,200/year)
- Dev: Already free (SWA Free tier)
- Staging: Save R350/month (App Service → SWA Free tier)
- Production: No change (SWA Standard)

---

## Quick Links

**Workflows**:
- [Staging SWA Workflow](../.github/workflows/azure-static-web-apps-staging.yml)
- [PR Preview Tests](../.github/workflows/swa-preview-tests.yml)
- [Old Staging Workflow (Disabled)](../.github/workflows/mystira-app-pwa-cicd-staging.yml.disabled)

**Key Decisions**:
- Strategy: All-SWA Standardization (simpler, better parity)
- Migration: Only Staging needs action (~2 hours)
- Cost: R350/mo savings (not cost-neutral)

---

## Need Help?

1. **For migration**: Start with `STAGING_MIGRATION_EXECUTION.md`
2. **For strategy questions**: See `CORRECTED_ENVIRONMENT_STRATEGY.md`
3. **For troubleshooting**: Check `STAGING_MIGRATION_GUIDE.md` troubleshooting section
4. **For analysis details**: See `PR_ANALYSIS.md`

---

**Maintained By**: DevOps Team  
**Next Review**: After Staging migration completion
