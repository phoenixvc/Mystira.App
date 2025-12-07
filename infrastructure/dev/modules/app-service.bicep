// App Service Module
@description('Name of the App Service')
param appServiceName string

@description('Name of the App Service Plan')
param appServicePlanName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('App Service Plan SKU')
param sku string = 'B1'

@description('Cosmos DB connection string')
@secure()
param cosmosDbConnectionString string

@description('Azure Storage connection string')
@secure()
param storageConnectionString string

@description('JWT RSA Private Key (PEM format) - use Key Vault reference in production')
@secure()
param jwtRsaPrivateKey string = ''

@description('JWT RSA Public Key (PEM format) - use Key Vault reference in production')
@secure()
param jwtRsaPublicKey string = ''

@description('JWT Issuer')
param jwtIssuer string = 'MystiraAPI'

@description('JWT Audience')
param jwtAudience string = 'MystiraPWA'

@description('Key Vault name for secret references (optional)')
param keyVaultName string = ''

@description('Azure Communication Services connection string')
@secure()
param acsConnectionString string = ''

@description('Sender email address')
param acsSenderEmail string = 'donotreply@mystira.app'

@description('Allowed CORS origins')
param corsAllowedOrigins string

@description('Application Insights connection string')
param appInsightsConnectionString string = ''

@description('Log Analytics Workspace ID')
param logAnalyticsWorkspaceId string = ''

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: sku
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: sku != 'F1' && sku != 'D1' // AlwaysOn not available for Free/Shared tiers
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Development'
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
        {
          name: 'AzureCommunicationServices__ConnectionString'
          value: acsConnectionString
        }
        {
          name: 'AzureCommunicationServices__SenderEmail'
          value: acsSenderEmail
        }
        {
          name: 'CorsSettings__AllowedOrigins'
          value: corsAllowedOrigins
        }
        {
          name: 'Azure__BlobStorage__ContainerName'
          value: 'mystira-app-media'
        }
        {
          name: 'Azure__CosmosDb__DatabaseName'
          value: 'MystiraAppDb'
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
      ]
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

output appServiceName string = appService.name
output appServiceId string = appService.id
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServicePlanId string = appServicePlan.id
