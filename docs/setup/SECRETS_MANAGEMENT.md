# Secrets Management Guide

## Overview

This guide explains how to securely manage secrets and sensitive configuration values for the Mystira Application Suite. Following these practices is critical for maintaining security and COPPA compliance.

> **📘 For GitHub Actions Configuration**: See [GitHub Secrets and Variables Guide](GITHUB_SECRETS_VARIABLES.md) for detailed instructions on configuring secrets for CI/CD pipelines across Development, Staging, and Production environments.

## ⚠️ CRITICAL: Never Commit Secrets to Version Control

**NEVER** commit the following to Git:
- Connection strings (Cosmos DB, Azure Storage, etc.)
- JWT signing keys
- API keys and tokens
- Email service credentials
- Any other sensitive configuration values

## Recommended Approach: Azure Key Vault (Production)

For production and staging environments, use Azure Key Vault to store all secrets.

### Setup Steps

1. **Create an Azure Key Vault** (if not already created):
   ```bash
   az keyvault create \
     --name mystira-app-keyvault \
     --resource-group mystira-app-rg \
     --location eastus
   ```

2. **Add secrets to Key Vault**:
   ```bash
   # Cosmos DB connection string
   az keyvault secret set \
     --vault-name mystira-app-keyvault \
     --name CosmosDbConnectionString \
     --value "YOUR_COSMOS_DB_CONNECTION_STRING"

   # Azure Storage connection string
   az keyvault secret set \
     --vault-name mystira-app-keyvault \
     --name AzureStorageConnectionString \
     --value "YOUR_STORAGE_CONNECTION_STRING"

   # JWT signing key
   az keyvault secret set \
     --vault-name mystira-app-keyvault \
     --name JwtSigningKey \
     --value "YOUR_JWT_KEY"
   ```

3. **Grant access to your App Service**:
   ```bash
   # Enable managed identity for your App Service
   az webapp identity assign \
     --name mystira-app-api \
     --resource-group mystira-app-rg

   # Grant the managed identity access to Key Vault
   az keyvault set-policy \
     --name mystira-app-keyvault \
     --object-id <MANAGED_IDENTITY_PRINCIPAL_ID> \
     --secret-permissions get list
   ```

4. **Update appsettings.json to reference Key Vault**:
   ```json
   {
     "ConnectionStrings": {
       "CosmosDb": "@Microsoft.KeyVault(SecretUri=https://mystira-app-keyvault.vault.azure.net/secrets/CosmosDbConnectionString/)",
       "AzureStorage": "@Microsoft.KeyVault(SecretUri=https://mystira-app-keyvault.vault.azure.net/secrets/AzureStorageConnectionString/)"
     },
     "Jwt": {
       "Key": "@Microsoft.KeyVault(SecretUri=https://mystira-app-keyvault.vault.azure.net/secrets/JwtSigningKey/)"
     }
   }
   ```

5. **Add Key Vault configuration to Program.cs**:
   ```csharp
   // In Program.cs, before builder.Build():
   if (!builder.Environment.IsDevelopment())
   {
       var keyVaultUrl = new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/");
       builder.Configuration.AddAzureKeyVault(keyVaultUrl, new DefaultAzureCredential());
   }
   ```

## Alternative: User Secrets (Local Development Only)

For local development, use .NET User Secrets to store sensitive values outside of the project directory.

### Setup Steps

1. **Initialize User Secrets** for each project:
   ```bash
   cd src/Mystira.App.Api
   dotnet user-secrets init

   cd ../Mystira.App.Admin.Api
   dotnet user-secrets init
   ```

2. **Add secrets**:
   ```bash
   # For Mystira.App.Api
   cd src/Mystira.App.Api

   dotnet user-secrets set "ConnectionStrings:CosmosDb" "YOUR_COSMOS_DB_CONNECTION_STRING"
   dotnet user-secrets set "ConnectionStrings:AzureStorage" "YOUR_STORAGE_CONNECTION_STRING"
   dotnet user-secrets set "Jwt:Key" "YOUR_JWT_SIGNING_KEY"
   dotnet user-secrets set "AzureCommunicationServices:ConnectionString" "YOUR_ACS_CONNECTION_STRING"
   dotnet user-secrets set "AzureCommunicationServices:SenderEmail" "DoNotReply@yourdomain.azurecomm.net"

   # Repeat for Mystira.App.Admin.Api
   cd ../Mystira.App.Admin.Api
   dotnet user-secrets set "ConnectionStrings:CosmosDb" "YOUR_COSMOS_DB_CONNECTION_STRING"
   dotnet user-secrets set "ConnectionStrings:AzureStorage" "YOUR_STORAGE_CONNECTION_STRING"
   dotnet user-secrets set "Jwt:Key" "YOUR_JWT_SIGNING_KEY"
   ```

3. **Verify secrets**:
   ```bash
   dotnet user-secrets list
   ```

### Where Are User Secrets Stored?

User secrets are stored in your user profile directory, completely outside the project:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

The `<user_secrets_id>` is defined in your `.csproj` file after running `dotnet user-secrets init`.

## Environment Variables (CI/CD Pipelines)

For GitHub Actions or Azure DevOps pipelines, use environment variables or pipeline secrets.

### GitHub Actions Configuration

For comprehensive GitHub Actions secrets configuration including all three environments (Development, Staging, Production), see the **[GitHub Secrets and Variables Guide](GITHUB_SECRETS_VARIABLES.md)**.

**Quick Reference:**
- Development environment: `dev` branch
- Staging environment: `staging` branch  
- Production environment: `main` branch

Each environment requires specific secrets for:
- Azure credentials and publish profiles
- JWT RSA key pairs (environment-specific)
- Azure Communication Services (Development only)
- Static Web Apps deployment tokens

**Example workflow reference**:
```yaml
- name: Run tests
  env:
    ConnectionStrings__CosmosDb: ${{ secrets.COSMOS_DB_CONNECTION_STRING }}
    ConnectionStrings__AzureStorage: ${{ secrets.AZURE_STORAGE_CONNECTION_STRING }}
    Jwt__Key: ${{ secrets.JWT_SIGNING_KEY }}
  run: dotnet test
```

### Azure DevOps Example

1. **Add secrets to Variable Groups**:
   - Go to Pipelines > Library > Variable Groups
   - Create a variable group (e.g., "Mystira-Secrets")
   - Add variables and mark them as secret

2. **Reference in pipeline**:
   ```yaml
   variables:
   - group: Mystira-Secrets

   steps:
   - task: DotNetCoreCLI@2
     inputs:
       command: 'test'
     env:
       ConnectionStrings__CosmosDb: $(CosmosDbConnectionString)
       ConnectionStrings__AzureStorage: $(AzureStorageConnectionString)
       Jwt__Key: $(JwtSigningKey)
   ```

## Secret Rotation

For production environments, implement regular secret rotation:

1. **Cosmos DB**: Regenerate keys every 90 days
2. **Storage accounts**: Rotate access keys quarterly
3. **JWT signing keys**: Implement key rotation strategy with overlapping validity periods
4. **ACS credentials**: Rotate connection strings every 90 days

### Key Rotation Script Example

```bash
#!/bin/bash
# rotate-secrets.sh

KEYVAULT_NAME="mystira-app-keyvault"

# Generate new JWT key
NEW_JWT_KEY=$(openssl rand -base64 64)

# Store in Key Vault with version
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name JwtSigningKey \
  --value "$NEW_JWT_KEY"

# Update App Service to use new key (restart required)
az webapp restart --name mystira-app-api --resource-group mystira-app-rg

echo "JWT key rotated successfully"
```

## Generating Secure Keys

### JWT Signing Key

Generate a cryptographically secure random key:

```bash
# Using OpenSSL (recommended - Linux/macOS/WSL)
openssl rand -base64 64
```

```powershell
# Using PowerShell (Windows) - cryptographically secure
$bytes = [byte[]]::new(64)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Store this in Key Vault or User Secrets, never in appsettings.json.

## Security Checklist

- [ ] All secrets removed from appsettings.json
- [ ] Production secrets stored in Azure Key Vault
- [ ] Development secrets stored in User Secrets
- [ ] Managed Identity configured for App Services
- [ ] Key Vault access policies configured
- [ ] Secret rotation schedule established
- [ ] CI/CD pipeline secrets configured
- [ ] Team members trained on secrets management
- [ ] `.gitignore` includes `**/appsettings.*.json` (except base appsettings.json)

## Troubleshooting

### "Unable to retrieve secret from Key Vault"

1. Verify managed identity is enabled:
   ```bash
   az webapp identity show --name mystira-app-api --resource-group mystira-app-rg
   ```

2. Check Key Vault access policies:
   ```bash
   az keyvault show --name mystira-app-keyvault --query properties.accessPolicies
   ```

3. Verify secret exists:
   ```bash
   az keyvault secret list --vault-name mystira-app-keyvault
   ```

### "Configuration value is empty"

1. Check User Secrets are initialized:
   ```bash
   dotnet user-secrets list
   ```

2. Verify .csproj has UserSecretsId:
   ```xml
   <PropertyGroup>
     <UserSecretsId>your-unique-id</UserSecretsId>
   </PropertyGroup>
   ```

3. Ensure environment-specific appsettings files aren't overriding values

## Additional Resources

- **[GitHub Secrets and Variables Guide](GITHUB_SECRETS_VARIABLES.md)** - Complete guide for CI/CD secrets across all environments
- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
- [Azure Managed Identity](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
- [GitHub Actions Encrypted Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

## Support

For questions or issues with secrets management:
1. Check this documentation first
2. Review Azure Key Vault logs in Azure Portal
3. Contact the DevOps team for Key Vault access issues
4. Never share secrets via email, chat, or issue trackers
