# Secret Rotation Guide

This document outlines the secret rotation strategy for Mystira.App.

## Overview

Secrets are managed in Azure Key Vault with automatic rotation where supported. The application uses `DefaultAzureCredential` which supports managed identity for secure, passwordless authentication.

## Secret Categories

### 1. JWT Signing Keys (RSA)

**Rotation Period**: 90 days
**Type**: Manual with overlap period

```bash
# Generate new RSA key pair
openssl genrsa -out private_key_new.pem 2048
openssl rsa -in private_key_new.pem -pubout -out public_key_new.pem

# Add to Key Vault with new version
az keyvault secret set \
  --vault-name mys-prod-mystira-kv-san \
  --name JwtSettings--RsaPrivateKey \
  --file private_key_new.pem

az keyvault secret set \
  --vault-name mys-prod-mystira-kv-san \
  --name JwtSettings--RsaPublicKey \
  --file public_key_new.pem
```

**Overlap Period**: Keep old keys valid for 7 days to allow existing tokens to expire.

### 2. Database Connection Strings

**Rotation Period**: 180 days
**Type**: Automatic via Managed Identity (recommended)

For Cosmos DB with managed identity, no connection string rotation is needed. The managed identity credentials are rotated automatically by Azure.

### 3. API Keys (Story Protocol, Chain Service)

**Rotation Period**: 90 days
**Type**: Manual

```bash
# Update Chain Service API key
az keyvault secret set \
  --vault-name mys-prod-mystira-kv-san \
  --name ChainService--ApiKey \
  --value "<new-api-key>"
```

### 4. Azure Communication Services

**Rotation Period**: 180 days
**Type**: Manual

```bash
# Regenerate key in Azure Portal, then update
az keyvault secret set \
  --vault-name mys-prod-mystira-kv-san \
  --name AzureCommunicationServices--ConnectionString \
  --value "<new-connection-string>"
```

## Rotation Checklist

### Pre-Rotation

1. [ ] Verify current secret expiration dates
2. [ ] Schedule rotation during low-traffic period
3. [ ] Notify team of upcoming rotation
4. [ ] Ensure monitoring is active

### During Rotation

1. [ ] Generate new secret/key
2. [ ] Add new version to Key Vault
3. [ ] Verify application can access new secret
4. [ ] Monitor for authentication errors

### Post-Rotation

1. [ ] Verify all environments are using new secrets
2. [ ] Update documentation with new expiration dates
3. [ ] Remove old secret versions after grace period
4. [ ] Update rotation calendar

## Key Vault Secret Naming Convention

Secrets use double-dash (`--`) separator for nested configuration:

| Configuration Path | Key Vault Secret Name |
|-------------------|----------------------|
| `JwtSettings:RsaPrivateKey` | `JwtSettings--RsaPrivateKey` |
| `ChainService:ApiKey` | `ChainService--ApiKey` |
| `ConnectionStrings:CosmosDb` | `ConnectionStrings--CosmosDb` |

## Monitoring Secret Usage

### Application Insights Queries

```kusto
// Track secret access failures
traces
| where message contains "Key Vault" and severityLevel >= 3
| summarize count() by bin(timestamp, 1h)
```

### Key Vault Diagnostics

Enable diagnostic logs in Key Vault for:
- `AuditEvent` - Track all secret operations
- `AllMetrics` - Monitor access patterns

## Automation (Future)

Consider implementing Azure Key Vault secret rotation using:
- Azure Functions with timer trigger
- Azure Event Grid notifications for expiring secrets
- Azure Automation runbooks

See ADR-0015 for planned automation implementation.

## Emergency Rotation

In case of suspected compromise:

1. Immediately rotate the affected secret
2. Invalidate existing tokens/sessions if applicable
3. Review access logs in Key Vault
4. Notify security team
5. Document incident

## Related Documents

- [Key Vault Configuration](../architecture/adr/ADR-0015-wolverine-migration.md)
- [Security Scanning Workflow](../../.github/workflows/security-scanning.yml)
- [Implementation Roadmap](../planning/implementation-roadmap.md)
