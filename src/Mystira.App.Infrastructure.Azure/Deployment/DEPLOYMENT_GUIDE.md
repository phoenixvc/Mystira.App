# Story Protocol Azure Infrastructure Deployment Guide

This guide covers deploying Azure infrastructure including Azure Key Vault for Story Protocol secrets management.

## Prerequisites

- Azure CLI installed and configured
- Azure subscription with appropriate permissions
- Service principal or managed identity for deployments
- Story Protocol private key (obtained from Story Protocol documentation)

## Deployment Steps

### 1. Set Environment Variables

```bash
# Set your environment
ENVIRONMENT="dev"  # or "prod"
LOCATION="westus"
SHORT_LOCATION="wus"
RESOURCE_GROUP="mystira-app-${ENVIRONMENT}-rg"

# Get your Azure AD object ID (for Key Vault access)
OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
```

### 2. Create Resource Group

```bash
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 3. Deploy Infrastructure with Key Vault

```bash
# Deploy using Bicep templates
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file src/Mystira.App.Infrastructure.Azure/Deployment/${ENVIRONMENT}/main.bicep \
  --parameters \
    environment=$ENVIRONMENT \
    shortLocation=$SHORT_LOCATION \
    keyVaultAdminObjectId=$OBJECT_ID
```

### 4. Store Story Protocol Private Key in Key Vault

After deployment, update the placeholder private key:

```bash
# Get Key Vault name from deployment output
KEY_VAULT_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name main \
  --query properties.outputs.keyVaultName.value -o tsv)

# Set the Story Protocol private key (REPLACE with your actual key)
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "StoryProtocol--PrivateKey" \
  --value "YOUR_ACTUAL_PRIVATE_KEY_HERE"

# Verify the secret was set (this will show the secret ID, not the value)
az keyvault secret show \
  --vault-name $KEY_VAULT_NAME \
  --name "StoryProtocol--PrivateKey" \
  --query id
```

### 5. Grant App Service Access to Key Vault

The App Service needs managed identity to access Key Vault:

```bash
# Get App Service name
APP_SERVICE_NAME="${ENVIRONMENT}-${SHORT_LOCATION}-app-mystira-api"

# Enable system-assigned managed identity
az webapp identity assign \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP

# Get the managed identity's principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Grant Key Vault access to the managed identity
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### 6. Configure Story Protocol Settings

Update the App Service configuration:

```bash
# Set Story Protocol configuration
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "StoryProtocol__Enabled=true" \
    "StoryProtocol__UseMockImplementation=false" \
    "StoryProtocol__Network=testnet" \
    "StoryProtocol__Contracts__IpAssetRegistry=YOUR_CONTRACT_ADDRESS" \
    "StoryProtocol__Contracts__RoyaltyModule=YOUR_CONTRACT_ADDRESS" \
    "StoryProtocol__Contracts__LicenseRegistry=YOUR_CONTRACT_ADDRESS"
```

## GitHub Actions Deployment

To deploy via GitHub Actions, add these secrets to your repository:

### Required Secrets

1. **AZURE_CREDENTIALS**: Service principal credentials
   ```bash
   az ad sp create-for-rbac \
     --name "mystira-github-deploy" \
     --role contributor \
     --scopes /subscriptions/{subscription-id}/resourceGroups/${RESOURCE_GROUP} \
     --sdk-auth
   ```

2. **KEYVAULT_OBJECT_ID**: Object ID for Key Vault access
   ```bash
   # Get the service principal object ID
   SP_OBJECT_ID=$(az ad sp list --display-name "mystira-github-deploy" --query "[0].id" -o tsv)
   ```

3. **STORY_PROTOCOL_PRIVATE_KEY**: Story Protocol private key (encrypted in GitHub Secrets)

### Update Workflows

Add infrastructure deployment step to your workflow:

```yaml
- name: Deploy Azure Infrastructure
  run: |
    az deployment group create \
      --resource-group ${{ env.RESOURCE_GROUP }} \
      --template-file src/Mystira.App.Infrastructure.Azure/Deployment/prod/main.bicep \
      --parameters \
        environment=prod \
        shortLocation=wus \
        keyVaultAdminObjectId=${{ secrets.KEYVAULT_OBJECT_ID }}
```

## Security Best Practices

### 1. Private Key Management

- **NEVER** commit private keys to source control
- **ALWAYS** use Azure Key Vault for production secrets
- **ROTATE** keys regularly
- **AUDIT** Key Vault access logs

### 2. Network Security

- Enable Key Vault firewall for production
- Use private endpoints for enhanced security
- Implement IP allowlisting for Key Vault access

### 3. Access Control

- Use managed identities instead of service principals when possible
- Follow principle of least privilege
- Enable soft delete and purge protection for production Key Vaults
- Monitor Key Vault access with Azure Monitor

## Verification

### Test Key Vault Access

```bash
# Test that the App Service can access Key Vault
az webapp log tail \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP
```

Look for log entries indicating successful Key Vault connections.

### Test Story Protocol Integration

Use the API endpoints to test contributor registration:

```bash
# Test setting contributors (replace with actual values)
curl -X POST https://${APP_SERVICE_NAME}.azurewebsites.net/api/admin/contributors/scenarios/test-123 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "contributors": [
      {
        "name": "Test Writer",
        "walletAddress": "0x1234567890123456789012345678901234567890",
        "role": "Writer",
        "contributionPercentage": 100.0
      }
    ]
  }'
```

## Troubleshooting

### Key Vault Access Denied

If you see "Access denied" errors:

1. Verify managed identity is enabled on App Service
2. Check Key Vault access policies include the managed identity
3. Ensure the managed identity has "get" and "list" secret permissions

### Story Protocol Connection Errors

If blockchain connection fails:

1. Verify RPC URL is correct for your network
2. Check private key is valid and has sufficient gas
3. Verify Story Protocol contract addresses are correct
4. Check network connectivity from App Service

### Deployment Failures

Common issues:

- **Key Vault name taken**: Key Vault names are globally unique; modify the naming convention
- **Permission errors**: Ensure the deploying user/SP has sufficient permissions
- **Missing parameters**: All required parameters must be provided

## Monitoring

Set up monitoring for:

- Key Vault access attempts
- Failed authentication attempts
- Blockchain transaction failures
- Gas costs and spending

## Cost Optimization

- Use Standard Key Vault tier for dev/test
- Use Premium tier for production (HSM-backed keys)
- Monitor transaction costs on Story Protocol
- Implement rate limiting for API endpoints

## Next Steps

1. Implement actual Story Protocol SDK integration (replace NotImplementedException)
2. Add transaction monitoring and alerting
3. Set up automated key rotation
4. Implement gas price optimization
5. Add comprehensive logging for blockchain operations
