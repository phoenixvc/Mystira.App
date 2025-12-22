# Operational Runbooks

**Status**: Active
**Last Updated**: 2025-12-22
**Owner**: Platform Team

---

## Overview

This directory contains runbooks for common operational procedures. Each runbook provides step-by-step instructions for specific scenarios.

---

## Runbook Index

### Deployment & Releases

| Runbook | Description |
|---------|-------------|
| [Production Deployment](./production-deployment.md) | Deploy to production environment |
| [Emergency Rollback](./emergency-rollback.md) | Rollback a failed deployment |
| [Hotfix Process](./hotfix-process.md) | Deploy an urgent fix |

### Incident Response

| Runbook | Description |
|---------|-------------|
| [High Error Rate](./incident-high-error-rate.md) | Respond to elevated error rates |
| [Database Issues](./incident-database.md) | Troubleshoot database problems |
| [Authentication Outage](./incident-auth-outage.md) | Respond to auth service issues |

### Maintenance

| Runbook | Description |
|---------|-------------|
| [Secret Rotation](../secret-rotation.md) | Rotate secrets and keys |
| [Scale Up/Down](./scale-resources.md) | Adjust resource capacity |
| [Certificate Renewal](./certificate-renewal.md) | Renew SSL certificates |

---

## Runbook Template

All runbooks should follow this structure:

```markdown
# [Runbook Title]

**Severity**: [Critical/High/Medium/Low]
**Time to Complete**: [Estimated time]
**Prerequisites**: [What you need before starting]

---

## Overview
[Brief description of when to use this runbook]

## Prerequisites
- [ ] Item 1
- [ ] Item 2

## Steps

### Step 1: [Action]
[Detailed instructions]

### Step 2: [Action]
[Detailed instructions]

## Verification
[How to confirm the procedure was successful]

## Rollback
[How to undo if something goes wrong]

## Post-Procedure
- [ ] Update documentation if needed
- [ ] Notify stakeholders
- [ ] Create incident report if applicable
```

---

## Related Documents

- [SLO Definitions](../slo-definitions.md)
- [RBAC and Security](../rbac-and-managed-identity.md)
- [Disaster Recovery](./disaster-recovery.md)
