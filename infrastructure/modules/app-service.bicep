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

@description('Custom domain to bind (e.g., "api.mystira.app")')
param customDomain string = ''

@description('Enable custom domain binding (requires DNS CNAME to be configured first)')
param enableCustomDomain bool = false

// Helper: Check if Key Vault is configured
var useKeyVault = !empty(keyVaultUri)

// Helper: Key Vault reference prefix for building secret URIs
// Format: @Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/secretname/)
var kvRefPrefix = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/'
var kvRefSuffix = '/)'

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
            value: '${kvRefPrefix}JwtSettings--RsaPrivateKey${kvRefSuffix}'
          }
          {
            name: 'JwtSettings__RsaPublicKey'
            value: '${kvRefPrefix}JwtSettings--RsaPublicKey${kvRefSuffix}'
          }
          {
            name: 'JwtSettings__Issuer'
            value: '${kvRefPrefix}JwtSettings--Issuer${kvRefSuffix}'
          }
          {
            name: 'JwtSettings__Audience'
            value: '${kvRefPrefix}JwtSettings--Audience${kvRefSuffix}'
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
            value: useKeyVault ? '${kvRefPrefix}Discord--BotToken${kvRefSuffix}' : discordBotToken
          }
        ] : [],
        // Bot Settings (if configured)
        !empty(botMicrosoftAppId) || useKeyVault ? [
          {
            name: 'Bot__MicrosoftAppId'
            value: useKeyVault ? '${kvRefPrefix}Bot--MicrosoftAppId${kvRefSuffix}' : botMicrosoftAppId
          }
          {
            name: 'Bot__MicrosoftAppPassword'
            value: useKeyVault ? '${kvRefPrefix}Bot--MicrosoftAppPassword${kvRefSuffix}' : botMicrosoftAppPassword
          }
        ] : [],
        // WhatsApp Settings (if configured)
        !empty(whatsAppChannelRegistrationId) || useKeyVault ? [
          {
            name: 'WhatsApp__ChannelRegistrationId'
            value: useKeyVault ? '${kvRefPrefix}WhatsApp--ChannelRegistrationId${kvRefSuffix}' : whatsAppChannelRegistrationId
          }
          {
            name: 'WhatsApp__PhoneNumberId'
            value: useKeyVault ? '${kvRefPrefix}WhatsApp--PhoneNumberId${kvRefSuffix}' : whatsAppPhoneNumberId
          }
          {
            name: 'WhatsApp__BusinessAccountId'
            value: useKeyVault ? '${kvRefPrefix}WhatsApp--BusinessAccountId${kvRefSuffix}' : whatsAppBusinessAccountId
          }
          {
            name: 'WhatsApp__WebhookVerifyToken'
            value: useKeyVault ? '${kvRefPrefix}WhatsApp--WebhookVerifyToken${kvRefSuffix}' : whatsAppWebhookVerifyToken
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

// Custom domain binding with managed SSL certificate
// CNAME should point: customDomain -> appServiceName.azurewebsites.net
//
// NOTE: For managed certificates, deploy in two phases:
//   Phase 1: Deploy with enableManagedCert=false (creates hostname binding only)
//   Phase 2: Deploy with enableManagedCert=true (creates certificate after DNS propagates)
//
// This avoids circular dependency issues with certificate validation

@description('Enable managed SSL certificate (requires hostname binding to exist first)')
param enableManagedCert bool = false

resource customDomainBinding 'Microsoft.Web/sites/hostNameBindings@2023-01-01' = if (enableCustomDomain && customDomain != '') {
  name: customDomain
  parent: appService
  properties: {
    siteName: appService.name
    hostNameType: 'Verified'
    sslState: enableManagedCert ? 'SniEnabled' : 'Disabled'
    thumbprint: enableManagedCert ? managedCertificate.properties.thumbprint : ''
  }
}

// App Service Managed Certificate for custom domain
// Azure automatically provisions and renews this certificate (free)
// Only create after hostname binding exists and DNS has propagated
resource managedCertificate 'Microsoft.Web/certificates@2023-01-01' = if (enableCustomDomain && customDomain != '' && enableManagedCert) {
  name: '${appServiceName}-${replace(customDomain, '.', '-')}-cert'
  location: location
  tags: tags
  properties: {
    serverFarmId: appServicePlan.id
    canonicalName: customDomain
  }
}

// Outputs
output appServiceName string = appService.name
output appServiceId string = appService.id
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceDefaultHostname string = appService.properties.defaultHostName
output appServicePlanId string = appServicePlan.id
output appServicePrincipalId string = appService.identity.principalId
output customDomainUrl string = enableCustomDomain && customDomain != '' ? 'https://${customDomain}' : ''
