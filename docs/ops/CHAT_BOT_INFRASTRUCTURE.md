# Chat Bot Infrastructure Deployment Guide

## Overview

This guide covers deploying the infrastructure required for multi-platform chat bot support (Discord, Teams, WhatsApp) using Azure resources.

## Automated Resources (Bicep/IaC)

The following resources can be fully automated via Bicep templates:

| Resource | Template | Purpose |
|----------|----------|---------|
| Azure Bot | `azure-bot.bicep` | Teams/Direct Line integration |
| Communication Services | `communication-services.bicep` | WhatsApp/SMS/Email |
| App Insights | `application-insights.bicep` | Bot telemetry |
| Key Vault | `key-vault.bicep` | Secrets management |

## Semi-Automated Resources

These require initial manual setup, then can be managed via IaC:

| Resource | Manual Step | Then Automate |
|----------|-------------|---------------|
| Azure AD App Registration | Create in Azure Portal | Reference App ID in Bicep |
| WhatsApp Business Account | Verify with Meta | Configure in ACS |
| Discord Application | Create at discord.com/developers | Store token in Key Vault |

## Deployment Parameters

### Azure Bot (Teams)

```bash
# Required GitHub Secrets
BOT_MICROSOFT_APP_ID       # From Azure AD App Registration
BOT_MICROSOFT_APP_PASSWORD # Client secret from Azure AD
```

### Discord Bot

```bash
# Required GitHub Secrets
DISCORD_BOT_TOKEN          # From Discord Developer Portal
```

### WhatsApp (via ACS)

```bash
# Required GitHub Secrets
ACS_CONNECTION_STRING      # From Communication Services
WHATSAPP_PHONE_NUMBER_ID   # From Meta Business Suite (after verification)
```

## Step-by-Step Setup

### 1. Azure AD App Registration (for Teams Bot)

```bash
# Create App Registration via Azure CLI
az ad app create \
  --display-name "Mystira Bot" \
  --sign-in-audience AzureADMultipleOrgs

# Get the App ID
APP_ID=$(az ad app list --display-name "Mystira Bot" --query "[0].appId" -o tsv)

# Create client secret
az ad app credential reset --id $APP_ID --years 2
```

Save the output:
- `appId` → `BOT_MICROSOFT_APP_ID`
- `password` → `BOT_MICROSOFT_APP_PASSWORD`

### 2. Deploy Infrastructure

```bash
# Dev environment
az deployment group create \
  --resource-group dev-euw-rg-mystira-app \
  --template-file infrastructure/dev/main.bicep \
  --parameters \
    deployAzureBot=true \
    botMicrosoftAppId=$APP_ID \
    botMicrosoftAppPassword=$SECRET \
    discordBotToken=$DISCORD_TOKEN \
    enableWhatsApp=true
```

### 3. Configure GitHub Secrets

Add to repository secrets:
```
AZURE_CREDENTIALS          # Service principal JSON
BOT_MICROSOFT_APP_ID       # Azure AD App ID
BOT_MICROSOFT_APP_PASSWORD # Azure AD client secret
DISCORD_BOT_TOKEN          # Discord bot token
ACS_CONNECTION_STRING      # Communication Services connection string
```

### 4. Workflow Integration

The infrastructure deployment workflow automatically:
1. Creates/updates Azure Bot resource
2. Enables Teams and WebChat channels
3. Deploys Communication Services (if not skipping)
4. Configures App Service with bot settings

## Resource Dependencies

```
┌─────────────────────┐
│  Azure AD App Reg   │ (Manual)
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐     ┌─────────────────────┐
│    Azure Bot        │────▶│   App Service       │
│  (Teams Channel)    │     │  (Bot Endpoint)     │
└─────────────────────┘     └─────────────────────┘
           │
           ▼
┌─────────────────────┐
│   App Insights      │
│   (Telemetry)       │
└─────────────────────┘

┌─────────────────────┐     ┌─────────────────────┐
│  Meta Business      │────▶│ Communication Svc   │
│  (WhatsApp Setup)   │     │ (WhatsApp Channel)  │
└─────────────────────┘     └─────────────────────┘

┌─────────────────────┐     ┌─────────────────────┐
│  Discord Dev Portal │────▶│   Key Vault         │
│  (Bot Token)        │     │  (Secrets)          │
└─────────────────────┘     └─────────────────────┘
```

## Updating Bot Credentials

### Rotate Teams Bot Secret

```bash
# Generate new secret
az ad app credential reset --id $APP_ID --years 2

# Update Key Vault
az keyvault secret set \
  --vault-name dev-euw-kv-mystira \
  --name BOT-MICROSOFT-APP-PASSWORD \
  --value $NEW_SECRET

# Restart App Service to pick up new secret
az webapp restart --name dev-euw-app-mystira-api --resource-group dev-euw-rg-mystira-app
```

### Rotate Discord Bot Token

1. Regenerate token at https://discord.com/developers/applications
2. Update Key Vault secret
3. Restart App Service

## Monitoring

Bot health is monitored via:
- `/health` endpoint health checks
- Application Insights telemetry
- Azure Bot Service analytics

See [BOT_MONITORING.md](./BOT_MONITORING.md) for detailed monitoring setup.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Bot not responding | Check App ID/Password match |
| Teams 401 errors | Verify messaging endpoint URL |
| WhatsApp not sending | Check ACS connection string |
| Discord rate limited | Review retry configuration |

## References

- [Azure Bot Service](https://docs.microsoft.com/azure/bot-service/)
- [Bot Framework SDK](https://docs.microsoft.com/azure/bot-service/bot-service-quickstart-create-bot)
- [ACS WhatsApp](https://docs.microsoft.com/azure/communication-services/concepts/whatsapp)
