@description('Name of the App Service')
param appServiceName string = 'dev-euw-app-mystira-api'

@description('Location for all resources')
param location string = resourceGroup().location

@description('App Service Plan SKU')
param sku string = 'B1'

@description('Cosmos DB connection string')
@secure()
param cosmosDbConnectionString string = ''

@description('Azure Storage connection string')
@secure()
param storageConnectionString string = ''

@description('JWT secret key')
@secure()
param jwtSecretKey string

// App Service Plan (Azure Cloud Hosting)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appServiceName}-plan'
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

// App Service (Azure Cloud Hosting)
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
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
          name: 'Jwt__Key'
          value: jwtSecretKey
        }
        {
          name: 'Jwt__Issuer'
          value: 'mystira-api'
        }
        {
          name: 'Jwt__Audience'
          value: 'mystira-app'
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

// Output the App Service URL
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceName string = appService.name