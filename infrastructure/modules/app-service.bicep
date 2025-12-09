// App Service Module
// Supports Key Vault references for secrets when keyVaultUri is provided

@description('Name of the App Service')
param appServiceName string

@description('Name of the App Service Plan')
param appServicePlanName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('App Service Plan SKU')
param sku string = 'B1'

@description('ASPNETCORE_ENVIRONMENT value')
@allowed([
  'Development'
  'Staging'
  'Production'
])
param aspnetEnvironment string = 'Development'

@description('Cosmos DB connection string')
@secure()
param cosmosDbConnectionString string

@description('Azure Storage connection string')
@secure()
param storageConnectionString string

@description('JWT RSA Private Key (PEM format) - used when Key Vault not configured')
@secure()
param jwtRsaPrivateKey string = ''

@description('JWT RSA Public Key (PEM format) - used when Key Vault not configured')
@secure()
param jwtRsaPublicKey string = ''

@description('JWT Issuer')
param jwtIssuer string = 'MystiraAPI'

@description('JWT Audience')
param jwtAudience string = 'MystiraPWA'

@description('Key Vault URI for secret references (e.g., https://mys-dev-mystira-kv-san.vault.azure.net/)')
param keyVaultUri string = ''

@description('Azure Communication Services connection string')
@secure()
param acsConnectionString string = ''

@description('Sender email address')
param acsSenderEmail string = 'DoNotReply@mystira.app'

@description('Allowed CORS origins')
param corsAllowedOrigins string

@description('Application Insights connection string')
param appInsightsConnectionString string = ''

@description('Log Analytics Workspace ID')
param logAnalyticsWorkspaceId string = ''

@description('Discord bot token (from Key Vault or direct)')
@secure()
param discordBotToken string = ''

@description('Bot Microsoft App ID (for Teams)')
param botMicrosoftAppId string = ''

@description('Bot Microsoft App Password')
@secure()
param botMicrosoftAppPassword string = ''

@description('WhatsApp Channel Registration ID')
param whatsAppChannelRegistrationId string = ''

@description('WhatsApp Phone Number ID')
param whatsAppPhoneNumberId string = ''

@description('WhatsApp Business Account ID')
param whatsAppBusinessAccountId string = ''

@description('WhatsApp Webhook Verify Token')
@secure()
param whatsAppWebhookVerifyToken string = ''

@description('Blob storage container name')
param blobContainerName string = 'mystira-app-media'

@description('Cosmos DB database name')
param cosmosDbDatabaseName string = 'MystiraAppDb'

@description('Tags for all resources')
param tags object = {}

// Helper: Check if Key Vault is configured
var useKeyVault = !empty(keyVaultUri)

// Helper: Build Key Vault reference
// Format: @Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/secretname/)
func kvRef(secretName string) string => '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/${secretName}/)'

// App Service Plan - uses existing if already deployed
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: sku
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service with system-assigned managed identity
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: sku != 'F1' && sku != 'D1' // AlwaysOn not available for Free/Shared tiers
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: concat(
        // Core settings
        [
          {
            name: 'ASPNETCORE_ENVIRONMENT'
            value: aspnetEnvironment
          }
          {
            name: 'ConnectionStrings__CosmosDb'
            value: cosmosDbConnectionString
          }
          {
            name: 'ConnectionStrings__AzureStorage'
            value: storageConnectionString
          }
          {
            name: 'CorsSettings__AllowedOrigins'
            value: corsAllowedOrigins
          }
          {
            name: 'Azure__BlobStorage__ContainerName'
            value: blobContainerName
          }
          {
            name: 'Azure__CosmosDb__DatabaseName'
            value: cosmosDbDatabaseName
          }
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: appInsightsConnectionString
          }
          {
            name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
            value: '~3'
          }
          {
            name: 'XDT_MicrosoftApplicationInsights_Mode'
            value: 'recommended'
          }
          {
            name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
            value: 'false'
          }
        ],
        // JWT Settings - use Key Vault references if configured
        useKeyVault ? [
          {
            name: 'JwtSettings__RsaPrivateKey'
            value: kvRef('JwtSettings--RsaPrivateKey')
          }
          {
            name: 'JwtSettings__RsaPublicKey'
            value: kvRef('JwtSettings--RsaPublicKey')
          }
          {
            name: 'JwtSettings__Issuer'
            value: kvRef('JwtSettings--Issuer')
          }
          {
            name: 'JwtSettings__Audience'
            value: kvRef('JwtSettings--Audience')
          }
        ] : [
          {
            name: 'JwtSettings__RsaPrivateKey'
            value: jwtRsaPrivateKey
          }
          {
            name: 'JwtSettings__RsaPublicKey'
            value: jwtRsaPublicKey
          }
          {
            name: 'JwtSettings__Issuer'
            value: jwtIssuer
          }
          {
            name: 'JwtSettings__Audience'
            value: jwtAudience
          }
        ],
        // ACS Settings
        [
          {
            name: 'AzureCommunicationServices__ConnectionString'
            value: acsConnectionString
          }
          {
            name: 'AzureCommunicationServices__SenderEmail'
            value: acsSenderEmail
          }
        ],
        // Discord Settings (if configured)
        !empty(discordBotToken) || useKeyVault ? [
          {
            name: 'Discord__BotToken'
            value: useKeyVault ? kvRef('Discord--BotToken') : discordBotToken
          }
        ] : [],
        // Bot Settings (if configured)
        !empty(botMicrosoftAppId) || useKeyVault ? [
          {
            name: 'Bot__MicrosoftAppId'
            value: useKeyVault ? kvRef('Bot--MicrosoftAppId') : botMicrosoftAppId
          }
          {
            name: 'Bot__MicrosoftAppPassword'
            value: useKeyVault ? kvRef('Bot--MicrosoftAppPassword') : botMicrosoftAppPassword
          }
        ] : [],
        // WhatsApp Settings (if configured)
        !empty(whatsAppChannelRegistrationId) || useKeyVault ? [
          {
            name: 'WhatsApp__ChannelRegistrationId'
            value: useKeyVault ? kvRef('WhatsApp--ChannelRegistrationId') : whatsAppChannelRegistrationId
          }
          {
            name: 'WhatsApp__PhoneNumberId'
            value: useKeyVault ? kvRef('WhatsApp--PhoneNumberId') : whatsAppPhoneNumberId
          }
          {
            name: 'WhatsApp__BusinessAccountId'
            value: useKeyVault ? kvRef('WhatsApp--BusinessAccountId') : whatsAppBusinessAccountId
          }
          {
            name: 'WhatsApp__WebhookVerifyToken'
            value: useKeyVault ? kvRef('WhatsApp--WebhookVerifyToken') : whatsAppWebhookVerifyToken
          }
          {
            name: 'WhatsApp__WebhookUrl'
            value: 'https://${appServiceName}.azurewebsites.net/api/webhooks/whatsapp'
          }
        ] : []
      )
      healthCheckPath: '/health'
    }
    httpsOnly: true
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${appServiceName}-diagnostics'
  scope: appService
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// Outputs
output appServiceName string = appService.name
output appServiceId string = appService.id
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServicePlanId string = appServicePlan.id
output appServicePrincipalId string = appService.identity.principalId
