// Dev Environment Infrastructure for Mystira App
// Resource Group: dev-euw-rg-mystira
// Subscription: 22f9eb18-6553-4b7d-9451-47d0195085fe (Phoenix Azure Sponsorship)

targetScope = 'resourceGroup'

@description('Environment name')
param environment string = 'dev'

@description('Location for all resources')
param location string = 'westeurope'

@description('Resource name prefix')
param resourcePrefix string = 'dev-euw'

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

@description('Azure Communication Services connection string')
@secure()
param acsConnectionString string = ''

@description('Sender email address for Azure Communication Services')
param acsSenderEmail string = 'donotreply@mystira.app'

@description('Allowed CORS origins for APIs')
param corsAllowedOrigins string = 'http://localhost:7000,https://localhost:7000,https://mystira.app,https://mango-water-04fdb1c03.3.azurestaticapps.net,https://blue-water-0eab7991e.3.azurestaticapps.net,https://brave-meadow-0ecd87c03.3.azurestaticapps.net'

@description('Skip storage account creation if it already exists')
param skipStorageCreation bool = false

@description('Connection string for existing storage account (if skipStorageCreation is true)')
@secure()
param existingStorageConnectionString string = ''

@description('Name of existing storage account (if skipStorageCreation is true)')
param existingStorageAccountName string = ''

@description('Resource group where existing storage account is located (if skipStorageCreation is true)')
param existingStorageResourceGroup string = ''

@description('Storage account name to create (only used if skipStorageCreation is false)')
param newStorageAccountName string = ''

@description('Skip Communication Services creation if it already exists')
param skipCommServiceCreation bool = false

@description('Resource group where existing Communication Service is located (if skipCommServiceCreation is true)')
param existingCommServiceResourceGroup string = ''

@description('Skip Cosmos DB creation if it already exists')
param skipCosmosCreation bool = false

@description('Resource group where existing Cosmos DB is located (if skipCosmosCreation is true)')
param existingCosmosResourceGroup string = ''

@description('Name of existing Cosmos DB account (if skipCosmosCreation is true)')
param existingCosmosDbAccountName string = ''

@description('Connection string for existing Cosmos DB (if skipCosmosCreation is true)')
@secure()
param existingCosmosConnectionString string = ''

@description('Skip App Service creation if they already exist')
param skipAppServiceCreation bool = false

@description('Resource group where existing App Services are located (if skipAppServiceCreation is true)')
param existingAppServiceResourceGroup string = ''

// Deploy Log Analytics Workspace
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics-deployment'
  params: {
    workspaceName: '${resourcePrefix}-log-mystira'
    location: location
  }
}

// Deploy Application Insights
module appInsights 'modules/application-insights.bicep' = {
  name: 'app-insights-deployment'
  params: {
    appInsightsName: '${resourcePrefix}-ai-mystira'
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Deploy Communication Services (conditional - skip if already exists)
module communicationServices 'modules/communication-services.bicep' = if (!skipCommServiceCreation) {
  name: 'communication-services-deployment'
  params: {
    communicationServiceName: '${resourcePrefix}-acs-mystira'
    emailServiceName: '${resourcePrefix}-email-mystira'
    domainName: 'mystira.app'
    location: 'global'
    dataLocation: 'Europe'
  }
}

// Reference existing Communication Service if skipping
resource existingCommService 'Microsoft.Communication/communicationServices@2023-04-01' existing = if (skipCommServiceCreation && existingCommServiceResourceGroup != '') {
  name: '${resourcePrefix}-acs-mystira'
  scope: resourceGroup(existingCommServiceResourceGroup)
}

// Get ACS connection string (from existing parameter or use empty - connection string must be retrieved separately)
var acsConnectionStringToUse = skipCommServiceCreation ? acsConnectionString : ''

// Storage account name calculation
var storageAccountNameToUse = skipStorageCreation ? 'placeholder123456789' : (newStorageAccountName != '' ? newStorageAccountName : replace(toLower('${resourcePrefix}-st-mystira'), '-', ''))
// Use existing storage connection string if provided, otherwise create new storage
var storageConnString = skipStorageCreation && existingStorageConnectionString != '' ? existingStorageConnectionString : ''

// Reference existing storage account (only if skipping and resource group provided)
resource existingStorage 'Microsoft.Storage/storageAccounts@2023-01-01' existing = if (skipStorageCreation && existingStorageResourceGroup != '' && existingStorageAccountName != '') {
  name: existingStorageAccountName
  scope: resourceGroup(existingStorageResourceGroup)
}

// Deploy Storage Account (conditional - skip if already exists)
module storage 'modules/storage.bicep' = if (!skipStorageCreation) {
  name: 'storage-deployment'
  params: {
    storageAccountName: storageAccountNameToUse
    location: location
    sku: 'Standard_LRS' // Dev: LRS for cost optimization
  }
}

// Deploy Cosmos DB (conditional - skip if already exists)
// Use existing connection string if provided, otherwise create new
var cosmosConnString = skipCosmosCreation && existingCosmosConnectionString != '' ? existingCosmosConnectionString : ''

module cosmosDb 'modules/cosmos-db.bicep' = if (!skipCosmosCreation) {
  name: 'cosmosdb-deployment'
  params: {
    cosmosDbAccountName: '${resourcePrefix}-cosmos-mystira'
    location: location
    databaseName: 'MystiraAppDb'
    serverless: true // Dev: Serverless for cost optimization
  }
}

// Reference existing Cosmos DB if skipping
resource existingCosmos 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' existing = if (skipCosmosCreation && existingCosmosResourceGroup != '' && existingCosmosDbAccountName != '') {
  name: existingCosmosDbAccountName
  scope: resourceGroup(existingCosmosResourceGroup)
}

// Reference existing App Services if skipping creation
resource existingApiAppService 'Microsoft.Web/sites@2023-12-01' existing = if (skipAppServiceCreation && existingAppServiceResourceGroup != '') {
  name: '${resourcePrefix}-app-mystira-api'
  scope: resourceGroup(existingAppServiceResourceGroup)
}

resource existingAdminApiAppService 'Microsoft.Web/sites@2023-12-01' existing = if (skipAppServiceCreation && existingAppServiceResourceGroup != '') {
  name: '${resourcePrefix}-app-mystira-admin-api'
  scope: resourceGroup(existingAppServiceResourceGroup)
}

// Deploy Main API App Service
module apiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'api-appservice-deployment'
  params: {
    appServiceName: '${resourcePrefix}-app-mystira-api'
    appServicePlanName: '${resourcePrefix}-asp-mystira-api'
    location: location
    sku: 'F1' // Dev: Free tier for cost optimization
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : (cosmosDb.outputs.cosmosDbConnectionString)
    storageConnectionString: skipStorageCreation ? storageConnString : (storage.outputs.storageConnectionString)
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    acsConnectionString: acsConnectionStringToUse
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Deploy Admin API App Service
module adminApiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'admin-api-appservice-deployment'
  params: {
    appServiceName: '${resourcePrefix}-app-mystira-admin-api'
    appServicePlanName: '${resourcePrefix}-asp-mystira-admin-api'
    location: location
    sku: 'F1' // Dev: Free tier for cost optimization
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : (cosmosDb.outputs.cosmosDbConnectionString)
    storageConnectionString: skipStorageCreation ? storageConnString : (storage.outputs.storageConnectionString)
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    acsConnectionString: acsConnectionStringToUse
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Outputs
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId
output appInsightsInstrumentationKey string = appInsights.outputs.instrumentationKey
output appInsightsConnectionString string = appInsights.outputs.connectionString
output communicationServiceId string = skipCommServiceCreation ? existingCommService.id : communicationServices.outputs.communicationServiceId
output emailServiceId string = skipCommServiceCreation ? '' : (communicationServices.outputs.emailServiceId)
output storageAccountName string = skipStorageCreation ? existingStorageAccountName : (storage.outputs.storageAccountName)
output storageConnectionString string = skipStorageCreation ? storageConnString : (storage.outputs.storageConnectionString)
output cosmosDbAccountName string = skipCosmosCreation ? existingCosmosDbAccountName : (cosmosDb.outputs.cosmosDbAccountName)
output cosmosDbConnectionString string = skipCosmosCreation ? cosmosConnString : (cosmosDb.outputs.cosmosDbConnectionString)
output apiAppServiceUrl string = skipAppServiceCreation ? 'https://${existingApiAppService.properties.defaultHostName}' : apiAppService.outputs.appServiceUrl
output adminApiAppServiceUrl string = skipAppServiceCreation ? 'https://${existingAdminApiAppService.properties.defaultHostName}' : adminApiAppService.outputs.appServiceUrl
