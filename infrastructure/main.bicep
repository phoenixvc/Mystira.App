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
  'euw'   // West Europe (westeurope)
  'eun'   // North Europe (northeurope)
  'wus'   // West US (westus)
  'eus'   // East US (eastus)
  'san'   // South Africa North (southafricanorth)
  'swe'   // Sweden Central (swedencentral)
  'uks'   // UK South (uksouth)
  'usw'   // US West 2 (westus2)
  'glob'  // Global / regionless
])
param region string = 'euw'

@description('Azure location for resources')
param location string = 'westeurope'

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

// Resource names following convention
var names = {
  // Monitoring
  logAnalytics: '${namePrefix}-log-${region}'
  appInsights: '${namePrefix}-appins-${region}'

  // Communication
  communicationService: '${namePrefix}-acs-${region}'
  emailService: '${namePrefix}-email-${region}'

  // Bot
  azureBot: '${namePrefix}-bot-${region}'

  // Storage & Data
  // Note: Storage accounts have strict naming (lowercase, no dashes, 3-24 chars)
  storageAccount: replace(toLower('${org}${environment}${project}st${region}'), '-', '')
  cosmosDb: '${namePrefix}-cosmos-${region}'

  // App Services (share a single App Service Plan to reduce costs)
  appServicePlan: '${namePrefix}-plan-${region}'
  apiApp: '${namePrefix}-api-${region}'
  adminApiApp: '${namePrefix}-adminapi-${region}'

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
// ─────────────────────────────────────────────────────────────────

@description('Enable WhatsApp channel in Communication Services')
param enableWhatsApp bool = false

@description('WhatsApp phone number ID (from Meta Business Suite)')
param whatsAppPhoneNumberId string = ''

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

// Key Vault (stores JWT secrets securely)
module keyVault 'modules/key-vault.bicep' = {
  name: 'deploy-key-vault'
  params: {
    keyVaultName: names.keyVault
    location: location
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
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

// Key Vault
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
