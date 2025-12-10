# DevHub Security Guide

## Overview

This document outlines security best practices and guidelines for using Mystira DevHub safely.

## Credential Management

### Current Implementation

**Connection Strings**: Currently, connection strings are entered manually in the UI and stored in component state only during the session. They are **NOT persisted** to disk.

### Security Best Practices

1. **Never commit credentials to git**
   - Do not store connection strings in configuration files
   - Do not hard-code secrets in source code
   - Add `.env` files to `.gitignore`

2. **Use environment variables for local development**
   ```bash
   # Set in your shell
   export SOURCE_COSMOS_CONNECTION="AccountEndpoint=..."
   export DEST_COSMOS_CONNECTION="AccountEndpoint=..."
   export SOURCE_STORAGE_CONNECTION="DefaultEndpointsProtocol=..."
   export DEST_STORAGE_CONNECTION="DefaultEndpointsProtocol=..."
   ```

3. **Connection string security**
   - Treat connection strings as sensitive as passwords
   - Use read-only connection strings when possible
   - Rotate keys regularly
   - Use Azure Managed Identity in production when possible

### Future Enhancements

**System Keychain Integration** (Phase 7 - Future):
- macOS: Keychain Access
- Windows: Credential Manager
- Linux: libsecret/Secret Service API
- Implementation using `tauri-plugin-keytar`

## Infrastructure Operations Security

### Bicep Template Viewing

- **Read-only Monaco Editor**: Templates cannot be edited from DevHub
- **File integrity**: Future versions will display SHA-256 hash of templates
- **Edit in IDE**: All template modifications should be done in VS Code or preferred IDE

### Destructive Actions

DevHub implements multiple confirmation layers for destructive operations:

1. **Deploy Infrastructure**
   - Confirmation dialog: "Are you sure you want to deploy?"
   - Triggers GitHub Actions workflow
   - Logged for audit trail

2. **Destroy Infrastructure**
   - Type-to-confirm: User must type "DELETE"
   - Additional confirmation dialog
   - Irreversible operation warning
   - Full deletion logged

### GitHub/Azure CLI Integration

DevHub uses existing authenticated CLI sessions:

- **No token capture**: DevHub never captures or stores authentication tokens
- **User authentication**: Relies on user's authenticated `gh` and `az` CLI
- **Verification**: Commands fail gracefully if not authenticated
- **Audit**: All operations logged to deployment history

### Prerequisites

Before using infrastructure features:

```bash
# Authenticate GitHub CLI
gh auth login

# Authenticate Azure CLI
az login
az account set --subscription "your-subscription-id"
```

## Data Migration Security

### Connection Validation

- Validate connection strings before migration
- Test connectivity to both source and destination
- Verify write permissions on destination

### Data Handling

- **Upsert operations**: Existing data is overwritten (by design)
- **No data deletion**: Migrations only add/update, never delete
- **Blob storage**: Files skipped if they already exist at destination
- **Rollback**: No automatic rollback - manual recovery required

### Recommendations

1. **Always backup before migration**
   ```bash
   # Cosmos DB backup (automatic in Azure)
   # Blob Storage backup
   az storage blob copy start-batch
   ```

2. **Test in non-production first**
   - Migrate dev → staging before staging → prod
   - Verify data integrity after migration
   - Check application functionality

3. **Monitor during migration**
   - Watch error logs
   - Check success/failure counts
   - Investigate partial failures

## Network Security

### Tauri Permissions

DevHub requests minimal Tauri permissions:

```json
{
  "allowlist": {
    "fs": {
      "readFile": true,  // For reading Bicep templates
      "scope": ["infrastructure/**/*.bicep"]
    },
    "shell": {
      "execute": true,   // For spawning .NET process
      "scope": ["dotnet"]
    }
  }
}
```

### External Connections

DevHub makes connections to:

- **Cosmos DB**: Read/write access to containers
- **Azure Blob Storage**: Copy operations
- **GitHub Actions API**: Via `gh` CLI
- **Azure Resource Manager**: Via `az` CLI

All connections use HTTPS/TLS encryption.

## Audit and Logging

### Operation Logging

DevHub logs all operations:

- Timestamp
- Operation type (export, migrate, deploy, etc.)
- User (from CLI authentication)
- Status (success/failed)
- Duration
- Resources affected

### Deployment History

Infrastructure operations are tracked:

- GitHub Actions workflow links
- Azure deployment URLs
- What-if analysis results
- Resource changes

### Future: Export Audit Logs

```typescript
// Future implementation
const exportAuditLog = () => {
  const logs = getOperationHistory();
  const csv = convertToCSV(logs);
  saveFile('audit-log.csv', csv);
};
```

## Compliance Considerations

### Data Residency

- Cosmos DB data stays in configured regions
- Blob Storage respects geo-replication settings
- DevHub does not transmit data outside Azure

### PII Handling

- DevHub operates on data but does not inspect content
- Existing PII redaction (from Wave 1) is preserved
- No telemetry or analytics data collected by DevHub

### COPPA Compliance

- Migration preserves age verification data
- No additional user tracking introduced
- Parental consent data maintained

## Incident Response

### Compromised Credentials

If connection strings are compromised:

1. **Immediately rotate keys** in Azure Portal
2. **Revoke access** for compromised keys
3. **Review audit logs** for unauthorized access
4. **Update connection strings** in all environments

### Failed Deployment

If infrastructure deployment fails:

1. **Check GitHub Actions logs** for details
2. **Review Bicep validation errors**
3. **Verify Azure subscription permissions**
4. **Check resource quotas and limits**
5. **Manual rollback** if necessary using Azure Portal

### Data Migration Issues

If migration fails or corrupts data:

1. **Stop migration immediately** (if in progress)
2. **Review error logs** in DevHub
3. **Restore from backup** if needed
4. **Investigate root cause** before retry
5. **Report issue** to development team

## Security Updates

### Dependency Management

```bash
# Check for security updates
npm audit

# Update dependencies
npm audit fix

# Cargo dependencies
cargo audit
```

### Tauri Updates

Stay current with Tauri security releases:

```bash
npm install @tauri-apps/cli@latest
npm install @tauri-apps/api@latest
```

## Reporting Security Issues

**DO NOT** open public GitHub issues for security vulnerabilities.

Instead:
1. Email security issues to the development team
2. Provide detailed description and reproduction steps
3. Allow time for patching before public disclosure

## Additional Resources

- [Azure Security Best Practices](https://docs.microsoft.com/azure/security/)
- [Tauri Security Guide](https://tauri.app/v1/guides/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [GitHub Security](https://docs.github.com/en/code-security)

---

**Document Version**: 1.0
**Last Updated**: 2025-11-23
**Review Schedule**: Quarterly
