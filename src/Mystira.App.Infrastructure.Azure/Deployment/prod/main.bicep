// ⚠️ CONFIGURATION REQUIRED: Update parameters for your deployment

@description('Environment name (dev, staging, prod)')
param environment string = 'prod' // ⚠️ UPDATE: Set to 'dev', 'staging', or 'prod'

@description('Location for all resources') 
param location string = resourceGroup().location // ⚠️ UPDATE: Set your preferred Azure region (e.g., 'East US', 'West Europe')

@description('Location short name')
param shortLocation string = 'wus'

@description('JWT secret key')
@secure()
param jwtSecretKey string = newGuid() // ⚠️ UPDATE: Provide your own secure JWT key for production

// Variables - ⚠️ UPDATE: Change 'mystira' to your app name
var resourcePrefix = '${environment}-${shortLocation}-app-mystira' // ⚠️ UPDATE: Change 'mystira' prefix to your app name
var cosmosDbName = replace('${resourcePrefix}cosmos', '-', '')  // Remove hyphens for Cosmos DB name

// Deploy Storage Account
module storage 'storage.bicep' = {
  name: 'storage-deployment'
  params: {
    storageAccountName: '${replace(resourcePrefix, '-', '')}storage'
    location: location
    sku: environment == 'prod' ? 'Standard_GRS' : 'Standard_LRS' // ⚠️ UPDATE: Update according to data needs
  }
}

// Deploy Cosmos DB
module cosmosDb 'cosmos-db.bicep' = {
  name: 'cosmosdb-deployment'
  params: {
    cosmosDbAccountName: cosmosDbName
    location: location
    databaseName: 'MystiraAppDb'
  }
}

// Deploy App Service
module appService 'app-service.bicep' = {
  name: 'appservice-deployment'
  params: {
    appServiceName: '${resourcePrefix}-api'
    location: location
    sku: environment == 'prod' ? 'P1v3' : 'B1'
    cosmosDbConnectionString: cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: storage.outputs.storageConnectionString
    jwtSecretKey: jwtSecretKey
  }
}

// Outputs
output appServiceUrl string = appService.outputs.appServiceUrl
output storageAccountName string = storage.outputs.storageAccountName
output cosmosDbAccountName string = cosmosDb.outputs.cosmosDbAccountName
output mediaContainerUrl string = storage.outputs.mediaContainerUrl