// Mystira App Infrastructure
// Single main.bicep with environment-specific parameter files
// Usage: az deployment group create --template-file main.bicep --parameters @params.<env>.json

targetScope = 'resourceGroup'

// ─────────────────────────────────────────────────────────────────
// Core Environment Parameters
// ─────────────────────────────────────────────────────────────────

@description('Environment name')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string

@description('Location for all resources')
param location string = 'westeurope'

@description('Resource name prefix (e.g., dev-euw, staging-euw, prod-euw)')
param resourcePrefix string

@description('Tags applied to all resources')
param tags object = {
  environment: environment
  application: 'mystira-app'
}

// ─────────────────────────────────────────────────────────────────
// SKU Parameters (vary by environment)
// ─────────────────────────────────────────────────────────────────

@description('App Service Plan SKU')
@allowed([
  'F1'   // Free (dev only)
  'B1'   // Basic
  'B2'
  'B3'
  'S1'   // Standard
  'S2'
  'S3'
  'P1v3' // Premium v3
  'P2v3'
  'P3v3'
])
param appServiceSku string = 'F1'

@description('Storage account SKU')
@allowed([
  'Standard_LRS'  // Dev
  'Standard_GRS'  // Staging
  'Standard_RAGRS' // Prod
])
param storageSku string = 'Standard_LRS'

@description('Enable Cosmos DB serverless mode (recommended for dev)')
param cosmosServerless bool = true

@description('Log Analytics data retention in days')
param logRetentionDays int = 30

@description('Log Analytics daily quota in GB (-1 for unlimited)')
param logDailyQuotaGb int = 1

// ─────────────────────────────────────────────────────────────────
// JWT/Authentication Parameters
// ─────────────────────────────────────────────────────────────────

@description('JWT RSA Private Key (PEM format) for RS256 signing')
@secure()
param jwtRsaPrivateKey string

@description('JWT RSA Public Key (PEM format) for RS256 verification')
@secure()
param jwtRsaPublicKey string

@description('JWT Issuer')
param jwtIssuer string = 'MystiraAPI'

@description('JWT Audience')
param jwtAudience string = 'MystiraPWA'

// ─────────────────────────────────────────────────────────────────
// Communication Services Parameters
// ─────────────────────────────────────────────────────────────────

@description('Azure Communication Services connection string (for existing ACS)')
@secure()
param acsConnectionString string = ''

@description('Sender email address for Azure Communication Services')
param acsSenderEmail string = 'DoNotReply@mystira.app'

@description('Email domain name')
param emailDomainName string = 'mystira.app'

@description('Allowed CORS origins for APIs')
param corsAllowedOrigins string

// ─────────────────────────────────────────────────────────────────
// Conditional Deployment Flags (for existing resources)
// ─────────────────────────────────────────────────────────────────

@description('Skip storage account creation if it already exists')
param skipStorageCreation bool = false

@description('Connection string for existing storage account')
@secure()
param existingStorageConnectionString string = ''

@description('Name of existing storage account')
param existingStorageAccountName string = ''

@description('Skip Communication Services creation if it already exists')
param skipCommServiceCreation bool = false

@description('Skip Cosmos DB creation if it already exists')
param skipCosmosCreation bool = false

@description('Name of existing Cosmos DB account')
param existingCosmosDbAccountName string = ''

@description('Connection string for existing Cosmos DB')
@secure()
param existingCosmosConnectionString string = ''

@description('Skip App Service creation if they already exist')
param skipAppServiceCreation bool = false

// ─────────────────────────────────────────────────────────────────
// Azure Bot Parameters (optional Teams integration)
// ─────────────────────────────────────────────────────────────────

@description('Deploy Azure Bot for Teams integration')
param deployAzureBot bool = false

@description('Microsoft App ID for the bot (from Azure AD App Registration)')
param botMicrosoftAppId string = ''

@description('Microsoft App Password (client secret from Azure AD)')
@secure()
param botMicrosoftAppPassword string = ''

@description('Bot messaging endpoint URL')
param botEndpoint string = ''

@description('Bot SKU (F0 = Free, S1 = Standard)')
@allowed([
  'F0'
  'S1'
])
param botSku string = 'F0'

// ─────────────────────────────────────────────────────────────────
// WhatsApp Parameters (via ACS)
// ─────────────────────────────────────────────────────────────────

@description('Enable WhatsApp channel in Communication Services')
param enableWhatsApp bool = false

@description('WhatsApp phone number ID (from Meta Business Suite)')
param whatsAppPhoneNumberId string = ''

// ─────────────────────────────────────────────────────────────────
// Module Deployments
// ─────────────────────────────────────────────────────────────────

// Computed values
var storageAccountName = skipStorageCreation ? 'placeholder' : replace(toLower('${resourcePrefix}stmystira'), '-', '')
var cosmosConnString = skipCosmosCreation && existingCosmosConnectionString != '' ? existingCosmosConnectionString : ''
var storageConnString = skipStorageCreation && existingStorageConnectionString != '' ? existingStorageConnectionString : ''
var acsConnStringToUse = skipCommServiceCreation ? acsConnectionString : ''

// Log Analytics Workspace
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics-${environment}'
  params: {
    workspaceName: '${resourcePrefix}-log-mystira'
    location: location
    retentionInDays: logRetentionDays
    dailyQuotaGb: logDailyQuotaGb
  }
}

// Application Insights
module appInsights 'modules/application-insights.bicep' = {
  name: 'app-insights-${environment}'
  params: {
    appInsightsName: '${resourcePrefix}-ai-mystira'
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Communication Services (conditional)
module communicationServices 'modules/communication-services.bicep' = if (!skipCommServiceCreation) {
  name: 'communication-services-${environment}'
  params: {
    communicationServiceName: '${resourcePrefix}-acs-mystira'
    emailServiceName: '${resourcePrefix}-email-mystira'
    domainName: emailDomainName
    location: 'global'
    dataLocation: 'Europe'
    enableWhatsApp: enableWhatsApp
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    tags: tags
  }
}

// Azure Bot (conditional)
module azureBot 'modules/azure-bot.bicep' = if (deployAzureBot && botMicrosoftAppId != '') {
  name: 'azure-bot-${environment}'
  params: {
    botName: '${resourcePrefix}-bot-mystira'
    botDisplayName: 'Mystira Bot'
    microsoftAppId: botMicrosoftAppId
    microsoftAppPassword: botMicrosoftAppPassword
    botEndpoint: botEndpoint != '' ? botEndpoint : 'https://${resourcePrefix}-app-mystira-api.azurewebsites.net/api/messages/teams'
    location: 'global'
    sku: botSku
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    enableTeamsChannel: true
    enableWebChatChannel: true
    tags: tags
  }
}

// Storage Account (conditional)
module storage 'modules/storage.bicep' = if (!skipStorageCreation) {
  name: 'storage-${environment}'
  params: {
    storageAccountName: storageAccountName
    location: location
    sku: storageSku
  }
}

// Cosmos DB (conditional)
module cosmosDb 'modules/cosmos-db.bicep' = if (!skipCosmosCreation) {
  name: 'cosmos-${environment}'
  params: {
    cosmosDbAccountName: '${resourcePrefix}-cosmos-mystira'
    location: location
    databaseName: 'MystiraAppDb'
    serverless: cosmosServerless
  }
}

// Main API App Service (conditional)
module apiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'api-appservice-${environment}'
  params: {
    appServiceName: '${resourcePrefix}-app-mystira-api'
    appServicePlanName: '${resourcePrefix}-asp-mystira-api'
    location: location
    sku: appServiceSku
    aspnetEnvironment: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    acsConnectionString: acsConnStringToUse
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Admin API App Service (conditional)
module adminApiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'admin-api-appservice-${environment}'
  params: {
    appServiceName: '${resourcePrefix}-app-mystira-admin-api'
    appServicePlanName: '${resourcePrefix}-asp-mystira-admin-api'
    location: location
    sku: appServiceSku
    aspnetEnvironment: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    acsConnectionString: acsConnStringToUse
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// ─────────────────────────────────────────────────────────────────
// Outputs
// ─────────────────────────────────────────────────────────────────

output environment string = environment
output resourcePrefix string = resourcePrefix

// Monitoring
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId
output appInsightsInstrumentationKey string = appInsights.outputs.instrumentationKey
output appInsightsConnectionString string = appInsights.outputs.connectionString

// Communication Services
output communicationServiceConnectionString string = skipCommServiceCreation ? acsConnectionString : communicationServices.outputs.communicationServiceConnectionString
output emailServiceId string = skipCommServiceCreation ? '' : communicationServices.outputs.emailServiceId

// Storage
output storageAccountName string = skipStorageCreation ? existingStorageAccountName : storage.outputs.storageAccountName
output storageConnectionString string = skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString

// Cosmos DB
output cosmosDbAccountName string = skipCosmosCreation ? existingCosmosDbAccountName : cosmosDb.outputs.cosmosDbAccountName
output cosmosDbConnectionString string = skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString

// App Services
output apiAppServiceUrl string = skipAppServiceCreation ? '' : apiAppService.outputs.appServiceUrl
output adminApiAppServiceUrl string = skipAppServiceCreation ? '' : adminApiAppService.outputs.appServiceUrl

// Azure Bot
output azureBotId string = deployAzureBot && botMicrosoftAppId != '' ? azureBot.outputs.botId : ''
output azureBotName string = deployAzureBot && botMicrosoftAppId != '' ? azureBot.outputs.botName : ''
output azureBotEndpoint string = deployAzureBot && botMicrosoftAppId != '' ? azureBot.outputs.botEndpoint : ''

// WhatsApp
output whatsAppEnabled bool = enableWhatsApp
