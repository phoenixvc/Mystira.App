# RBAC and Managed Identity Guide

**Status**: Active
**Last Updated**: 2025-12-22

---

## Overview

This document outlines the Role-Based Access Control (RBAC) and Managed Identity patterns used in Mystira.App for secure Azure resource access.

---

## Managed Identity Strategy

### DefaultAzureCredential Chain

Mystira.App uses `DefaultAzureCredential` from the Azure SDK, which provides a unified authentication experience across environments:

```
┌─────────────────────────────────────────────────────────────────┐
│                   DefaultAzureCredential Flow                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Production (Azure App Service):                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  1. System-assigned Managed Identity                        ││
│  │  2. User-assigned Managed Identity (if configured)          ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                  │
│  Development (Local):                                            │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  1. Environment Variables (AZURE_CLIENT_ID, etc.)           ││
│  │  2. Azure CLI credentials (az login)                        ││
│  │  3. Visual Studio credentials                               ││
│  │  4. VS Code Azure extension                                 ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                  │
│  CI/CD (GitHub Actions):                                         │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  1. Workload Identity Federation (preferred)                ││
│  │  2. Service Principal via secrets                           ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Implementation

Key Vault configuration (`KeyVaultConfigurationExtensions.cs`):

```csharp
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    // Exclude interactive credentials in production for security
    ExcludeInteractiveBrowserCredential = !environment.IsDevelopment(),
    ExcludeVisualStudioCodeCredential = !environment.IsDevelopment(),

    // Fail fast in production if managed identity unavailable
    Retry =
    {
        MaxRetries = environment.IsDevelopment() ? 3 : 1,
        NetworkTimeout = TimeSpan.FromSeconds(environment.IsDevelopment() ? 10 : 3)
    }
});
```

---

## RBAC Role Assignments

### Least Privilege Principle

Each service identity receives only the minimum permissions required:

| Resource | Role | Principal | Purpose |
|----------|------|-----------|---------|
| **Key Vault** | Key Vault Secrets User | App Service MI | Read secrets at runtime |
| **Key Vault** | Key Vault Administrator | Admin User | Manage secrets (admin only) |
| **Cosmos DB** | Cosmos DB Account Reader | App Service MI | Read/write data |
| **Blob Storage** | Storage Blob Data Contributor | App Service MI | Upload/download media |
| **App Insights** | Monitoring Metrics Publisher | App Service MI | Write telemetry |
| **DNS Zone** | DNS Zone Contributor | GitHub Actions SP | Manage DNS records |

### Service Principal Roles (CI/CD)

```bash
# Create service principal with Contributor role on resource group
az ad sp create-for-rbac \
  --name "github-actions-mystira-app" \
  --role Contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/mys-{env}-mystira-rg-san \
  --sdk-auth

# Add DNS Zone Contributor for custom domains
az role assignment create \
  --assignee {sp-object-id} \
  --role "DNS Zone Contributor" \
  --scope /subscriptions/{subscription-id}/resourceGroups/mys-prod-mystira-rg-glob/providers/Microsoft.Network/dnszones/mystira.app
```

---

## Key Vault Access Policies

### Access Policy Model (Legacy)

For environments using access policies:

| Principal | Secrets | Keys | Certificates |
|-----------|---------|------|--------------|
| App Service MI | Get, List | - | - |
| Admin User | Get, List, Set, Delete | - | - |
| GitHub Actions SP | Get, List | - | - |

### RBAC Model (Recommended)

For environments using Azure RBAC:

```bash
# App Service - read secrets only
az role assignment create \
  --assignee {app-service-mi-object-id} \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/{subscription-id}/resourceGroups/mys-{env}-mystira-rg-san/providers/Microsoft.KeyVault/vaults/mys-{env}-mystira-kv-san

# Admin - full secret management
az role assignment create \
  --assignee {admin-object-id} \
  --role "Key Vault Administrator" \
  --scope /subscriptions/{subscription-id}/resourceGroups/mys-{env}-mystira-rg-san/providers/Microsoft.KeyVault/vaults/mys-{env}-mystira-kv-san
```

---

## Environment-Specific Configuration

### Development

```json
{
  "KeyVault": {
    "Name": ""  // Empty - secrets from local config or user-secrets
  }
}
```

Developers use:
- `dotnet user-secrets` for local development
- Azure CLI login (`az login`) if Key Vault is needed

### Staging

```json
{
  "KeyVault": {
    "Name": "mys-staging-mystira-kv-san"
  }
}
```

### Production

```json
{
  "KeyVault": {
    "Name": "mys-prod-mystira-kv-san"
  }
}
```

---

## Security Best Practices

### 1. Never Store Secrets in Code

```csharp
// BAD - hardcoded secret
var connectionString = "AccountEndpoint=https://...;AccountKey=SECRET";

// GOOD - from Key Vault via configuration
var connectionString = configuration.GetConnectionString("CosmosDb");
```

### 2. Use Managed Identity for Azure Resources

```csharp
// BAD - connection string with key
services.AddSingleton(new BlobServiceClient(connectionString));

// GOOD - managed identity
services.AddSingleton(new BlobServiceClient(
    new Uri($"https://{accountName}.blob.core.windows.net"),
    new DefaultAzureCredential()));
```

### 3. Minimize Token Lifetime

Configure short-lived tokens where possible:
- Access tokens: 1 hour (Azure default)
- Refresh tokens: As short as practical
- Key rotation: Every 90 days (see secret-rotation.md)

### 4. Network Security

- Enable private endpoints for sensitive resources
- Use VNet integration where available
- Configure IP restrictions on App Service

---

## Troubleshooting

### Common Issues

**"ManagedIdentityCredential authentication failed"**

1. Verify App Service has system-assigned managed identity enabled
2. Check role assignments are correct
3. Ensure resource allows access from App Service

**"Access denied to Key Vault"**

1. Verify Key Vault firewall allows App Service
2. Check RBAC role or access policy exists
3. Confirm managed identity object ID matches

**"DefaultAzureCredential timeout in production"**

1. Ensure only managed identity is tried (exclude interactive)
2. Check network connectivity to Azure AD
3. Review credential options configuration

### Diagnostic Commands

```bash
# Check App Service managed identity
az webapp identity show \
  --name mys-{env}-mystira-api-san \
  --resource-group mys-{env}-mystira-rg-san

# List Key Vault role assignments
az role assignment list \
  --scope /subscriptions/{sub}/resourceGroups/mys-{env}-mystira-rg-san/providers/Microsoft.KeyVault/vaults/mys-{env}-mystira-kv-san \
  --output table

# Test Key Vault access
az keyvault secret list \
  --vault-name mys-{env}-mystira-kv-san \
  --output table
```

---

## Migration to RBAC

If migrating from access policies to RBAC:

1. **Enable RBAC on Key Vault**:
   ```bash
   az keyvault update \
     --name mys-{env}-mystira-kv-san \
     --enable-rbac-authorization true
   ```

2. **Create Role Assignments** (before disabling access policies)

3. **Test Access** in staging first

4. **Remove Access Policies** (RBAC takes precedence when enabled)

---

## Related Documents

- [Secret Rotation Guide](../operations/secret-rotation.md)
- [Infrastructure README](../../infrastructure/README.md)
- [ADR-0012: Infrastructure as Code](../architecture/adr/ADR-0012-infrastructure-as-code.md)
