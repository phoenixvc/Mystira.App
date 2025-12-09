// Mystira App Infrastructure
// Naming Convention: [org]-[env]-[project]-[type]-[region]
// Resource Groups:   [org]-[env]-[project]-rg-[region]
// Usage: az deployment group create --template-file main.bicep --parameters @params.<env>.json

targetScope = 'resourceGroup'

// ─────────────────────────────────────────────────────────────────
// Naming Convention Parameters
// Pattern: [org]-[env]-[project]-[type]-[region]
// ─────────────────────────────────────────────────────────────────

@description('Organisation code')
@allowed([
  'mys'   // Mystira (Eben)
  'nl'    // NeuralLiquid (Jurie)
  'pvc'   // Phoenix VC (Eben)
  'tws'   // Twines & Straps (Martyn)
])
param org string = 'mys'

@description('Environment name')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string

@description('Project name')
param project string = 'mystira'

@description('Short region code')
@allowed([
  'san'   // South Africa North (southafricanorth) - PRIMARY
  'euw'   // West Europe (westeurope)
  'eun'   // North Europe (northeurope)
  'wus'   // West US (westus)
  'eus'   // East US (eastus)
  'eus2'  // East US 2 (eastus2) - FALLBACK for SWA/global services
  'swe'   // Sweden Central (swedencentral)
  'uks'   // UK South (uksouth)
  'usw'   // US West 2 (westus2)
  'glob'  // Global / regionless
])
param region string = 'san'

@description('Azure location for resources')
param location string = 'southafricanorth'

@description('Fallback region for services not available in primary region (e.g., Static Web Apps)')
param fallbackRegion string = 'eastus2'

@description('Tags applied to all resources')
param tags object = {
  org: org
  environment: environment
  project: project
  application: 'mystira-app'
}

// ─────────────────────────────────────────────────────────────────
// Naming Helper Variables
// Pattern: [org]-[env]-[project]-[type]-[region]
// ─────────────────────────────────────────────────────────────────

var namePrefix = '${org}-${environment}-${project}'

// Fallback region code for services not available in South Africa North
var fallbackRegionCode = fallbackRegion == 'eastus2' ? 'eus2' : (fallbackRegion == 'westeurope' ? 'euw' : 'eus2')

// Resource names following convention
var names = {
  // Monitoring
  logAnalytics: '${namePrefix}-log-${region}'
  appInsights: '${namePrefix}-appins-${region}'

  // Communication (global service)
  communicationService: '${namePrefix}-acs-glob'
  emailService: '${namePrefix}-email-glob'

  // Bot (global service)
  azureBot: '${namePrefix}-bot-glob'

  // Storage & Data
  // Note: Storage accounts have strict naming (lowercase, no dashes, 3-24 chars)
  storageAccount: replace(toLower('${org}${environment}${project}st${region}'), '-', '')
  cosmosDb: '${namePrefix}-cosmos-${region}'

  // App Services (share a single App Service Plan to reduce costs)
  appServicePlan: '${namePrefix}-plan-${region}'
  apiApp: '${namePrefix}-api-${region}'
  adminApiApp: '${namePrefix}-adminapi-${region}'

  // Static Web App (NOT available in South Africa North - uses fallback region)
  staticWebApp: '${namePrefix}-swa-${fallbackRegionCode}'

  // Security
  keyVault: '${namePrefix}-kv-${region}'
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
// Note: Requires Meta Business verification before channel can be used
// ─────────────────────────────────────────────────────────────────

@description('Enable WhatsApp channel in Communication Services')
param enableWhatsApp bool = false

@description('WhatsApp phone number ID (from Meta Business Suite → Phone Numbers)')
param whatsAppPhoneNumberId string = ''

@description('WhatsApp Channel Registration ID (from Azure Portal → ACS → Channels → WhatsApp)')
param whatsAppChannelRegistrationId string = ''

@description('WhatsApp Business Account ID (from Meta Business Suite → Business Settings)')
param whatsAppBusinessAccountId string = ''

@description('WhatsApp webhook verification token (random string for verifying webhook requests from Meta)')
@secure()
param whatsAppWebhookVerifyToken string = ''

// ─────────────────────────────────────────────────────────────────
// Discord Bot Parameters
// ─────────────────────────────────────────────────────────────────

@description('Discord bot token (from Discord Developer Portal → Bot → Token)')
@secure()
param discordBotToken string = ''

// ─────────────────────────────────────────────────────────────────
// Static Web App Parameters
// Note: SWA is NOT available in South Africa North - deploys to fallback region
// ─────────────────────────────────────────────────────────────────

@description('Deploy Static Web App (uses fallback region since SWA is not available in South Africa North)')
param deployStaticWebApp bool = false

@description('Static Web App SKU')
@allowed([
  'Free'
  'Standard'
])
param staticWebAppSku string = 'Free'

@description('GitHub repository URL for Static Web App (optional - can be linked later)')
param staticWebAppRepositoryUrl string = ''

@description('GitHub branch for Static Web App deployment')
param staticWebAppBranch string = 'dev'

// ─────────────────────────────────────────────────────────────────
// Computed Values
// ─────────────────────────────────────────────────────────────────

var storageAccountName = skipStorageCreation ? 'placeholder' : names.storageAccount
var cosmosConnString = skipCosmosCreation && existingCosmosConnectionString != '' ? existingCosmosConnectionString : ''
var storageConnString = skipStorageCreation && existingStorageConnectionString != '' ? existingStorageConnectionString : ''
var acsConnStringToUse = skipCommServiceCreation ? acsConnectionString : ''

// ─────────────────────────────────────────────────────────────────
// Module Deployments
// ─────────────────────────────────────────────────────────────────

// Log Analytics Workspace
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'deploy-log-analytics'
  params: {
    workspaceName: names.logAnalytics
    location: location
    retentionInDays: logRetentionDays
    dailyQuotaGb: logDailyQuotaGb
  }
}

// Application Insights
module appInsights 'modules/application-insights.bicep' = {
  name: 'deploy-app-insights'
  params: {
    appInsightsName: names.appInsights
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Key Vault (stores JWT secrets, Discord token, Bot credentials, and WhatsApp config)
module keyVault 'modules/key-vault.bicep' = {
  name: 'deploy-key-vault'
  params: {
    keyVaultName: names.keyVault
    location: location
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    discordBotToken: discordBotToken
    botMicrosoftAppId: botMicrosoftAppId
    botMicrosoftAppPassword: botMicrosoftAppPassword
    whatsAppChannelRegistrationId: whatsAppChannelRegistrationId
    whatsAppBusinessAccountId: whatsAppBusinessAccountId
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    whatsAppWebhookVerifyToken: whatsAppWebhookVerifyToken
  }
}

// Communication Services (conditional)
module communicationServices 'modules/communication-services.bicep' = if (!skipCommServiceCreation) {
  name: 'deploy-communication-services'
  params: {
    communicationServiceName: names.communicationService
    emailServiceName: names.emailService
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
  name: 'deploy-azure-bot'
  params: {
    botName: names.azureBot
    botDisplayName: 'Mystira Bot'
    microsoftAppId: botMicrosoftAppId
    microsoftAppPassword: botMicrosoftAppPassword
    botEndpoint: botEndpoint != '' ? botEndpoint : 'https://${names.apiApp}.azurewebsites.net/api/messages/teams'
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
  name: 'deploy-storage'
  params: {
    storageAccountName: storageAccountName
    location: location
    sku: storageSku
  }
}

// Cosmos DB (conditional)
module cosmosDb 'modules/cosmos-db.bicep' = if (!skipCosmosCreation) {
  name: 'deploy-cosmos-db'
  params: {
    cosmosDbAccountName: names.cosmosDb
    location: location
    databaseName: 'MystiraAppDb'
    serverless: cosmosServerless
  }
}

// Main API App Service (conditional)
module apiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'deploy-api-app-service'
  params: {
    appServiceName: names.apiApp
    appServicePlanName: names.appServicePlan
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

// Admin API App Service (conditional) - shares the same App Service Plan
module adminApiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'deploy-admin-api-app-service'
  dependsOn: [apiAppService]  // Ensure plan is created first
  params: {
    appServiceName: names.adminApiApp
    appServicePlanName: names.appServicePlan
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

// Static Web App (conditional - deploys to fallback region since not available in South Africa North)
module staticWebApp 'modules/static-web-app.bicep' = if (deployStaticWebApp) {
  name: 'deploy-static-web-app'
  params: {
    staticWebAppName: names.staticWebApp
    location: fallbackRegion
    sku: staticWebAppSku
    repositoryUrl: staticWebAppRepositoryUrl
    branch: staticWebAppBranch
    tags: tags
  }
}

// ─────────────────────────────────────────────────────────────────
// Outputs
// ─────────────────────────────────────────────────────────────────

// Naming info
output namingPattern string = '[org]-[env]-[project]-[type]-[region]'
output org string = org
output environment string = environment
output project string = project
output region string = region

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

// Discord
output discordBotConfigured bool = discordBotToken != ''

// Static Web App
output staticWebAppUrl string = deployStaticWebApp ? staticWebApp.outputs.staticWebAppUrl : ''
output staticWebAppName string = deployStaticWebApp ? staticWebApp.outputs.staticWebAppName : ''
output staticWebAppFallbackRegion string = fallbackRegion

// Key Vault
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri

// Region information
output primaryRegion string = location
output primaryRegionCode string = region
output fallbackRegionForSWA string = fallbackRegion
