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

@description('JWT secret key for API authentication')
@secure()
param jwtSecretKey string

@description('Azure Communication Services connection string')
@secure()
param acsConnectionString string = ''

@description('Sender email address for Azure Communication Services')
param acsSenderEmail string = 'donotreply@mystira.app'

@description('Allowed CORS origins for APIs')
param corsAllowedOrigins string = 'http://localhost:7000,https://localhost:7000,https://mystira.app,https://mango-water-04fdb1c03.3.azurestaticapps.net,https://blue-water-0eab7991e.3.azurestaticapps.net'

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

// Deploy Azure Communication Services
module communicationServices 'modules/communication-services.bicep' = {
  name: 'communication-services-deployment'
  params: {
    communicationServiceName: '${resourcePrefix}-acs-mystira'
    emailServiceName: '${resourcePrefix}-ecs-mystira'
    domainName: 'mystira.app'
    location: 'global'
  }
}

// Deploy Storage Account
module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
  params: {
    storageAccountName: 'mystiraappdevstorage'
    location: location
    corsAllowedOrigins: split(corsAllowedOrigins, ',')
  }
}

// Deploy Cosmos DB
module cosmosDb 'modules/cosmos-db.bicep' = {
  name: 'cosmosdb-deployment'
  params: {
    cosmosDbAccountName: 'mystiraappdevcosmos'
    location: location
    databaseName: 'MystiraAppDb'
  }
}

// Deploy Main API App Service
module apiAppService 'modules/app-service.bicep' = {
  name: 'api-appservice-deployment'
  params: {
    appServiceName: 'mystira-app-dev-api'
    appServicePlanName: '${resourcePrefix}-asp-mystira-api'
    location: location
    sku: 'B1'
    cosmosDbConnectionString: cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: storage.outputs.storageConnectionString
    jwtSecretKey: jwtSecretKey
    acsConnectionString: acsConnectionString
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Deploy Admin API App Service
module adminApiAppService 'modules/app-service.bicep' = {
  name: 'admin-api-appservice-deployment'
  params: {
    appServiceName: 'dev-euw-app-mystora-admin-api'
    appServicePlanName: '${resourcePrefix}-asp-mystira-admin-api'
    location: location
    sku: 'B1'
    cosmosDbConnectionString: cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: storage.outputs.storageConnectionString
    jwtSecretKey: jwtSecretKey
    acsConnectionString: acsConnectionString
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
output communicationServiceId string = communicationServices.outputs.communicationServiceId
output emailServiceId string = communicationServices.outputs.emailServiceId
output storageAccountName string = storage.outputs.storageAccountName
output storageConnectionString string = storage.outputs.storageConnectionString
output cosmosDbAccountName string = cosmosDb.outputs.cosmosDbAccountName
output cosmosDbConnectionString string = cosmosDb.outputs.cosmosDbConnectionString
output apiAppServiceUrl string = apiAppService.outputs.appServiceUrl
output adminApiAppServiceUrl string = adminApiAppService.outputs.appServiceUrl
