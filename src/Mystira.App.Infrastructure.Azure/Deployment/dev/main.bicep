// ⚠️ CONFIGURATION REQUIRED: Update parameters for your deployment

@description('Environment name (dev, staging, prod)')
param environment string = 'dev' // ⚠️ UPDATE: Set to 'dev', 'staging', or 'prod'

@description('Location for all resources') 
param location string = resourceGroup().location // ⚠️ UPDATE: Set your preferred Azure region (e.g., 'East US', 'West Europe')

@description('JWT secret key')
@secure()
param jwtSecretKey string = newGuid() // ⚠️ UPDATE: Provide your own secure JWT key for production

@description('Resources to deploy (empty array = deploy all)')
param deployStorage bool = true
@description('Deploy Cosmos DB')
param deployCosmos bool = true
@description('Deploy App Service')
param deployAppService bool = true

// Variables - Standardized naming: {env}-{location}-app-{name}
var resourcePrefix = '${environment}-euw-app-mystira' // Standardized format: {env}-{location}-app-{name}
var cosmosDbName = replace('${resourcePrefix}cosmos', '-', '')  // Remove hyphens for Cosmos DB name
var appServiceName = '${resourcePrefix}-api' // App Service name with -api suffix

// Deploy Storage Account (conditional)
module storage 'storage.bicep' = if (deployStorage) {
  name: 'storage-deployment'
  params: {
    storageAccountName: '${replace(resourcePrefix, '-', '')}storage'
    location: location
    sku: environment == 'prod' ? 'Standard_GRS' : 'Standard_LRS'
  }
}

// Deploy Cosmos DB (conditional)
module cosmosDb 'cosmos-db.bicep' = if (deployCosmos) {
  name: 'cosmosdb-deployment'
  params: {
    cosmosDbAccountName: cosmosDbName
    location: location
    databaseName: 'MystiraAppDb'
  }
}

// Deploy App Service (conditional)
module appService 'app-service.bicep' = if (deployAppService) {
  name: 'appservice-deployment'
  params: {
    appServiceName: appServiceName
    location: location
    sku: environment == 'prod' ? 'P1v3' : 'B1'
    cosmosDbConnectionString: deployCosmos ? cosmosDb.outputs.cosmosDbConnectionString : ''
    storageConnectionString: deployStorage ? storage.outputs.storageConnectionString : ''
    jwtSecretKey: jwtSecretKey
  }
}

// Outputs (conditional based on what was deployed)
output appServiceUrl string = deployAppService ? appService.outputs.appServiceUrl : ''
output storageAccountName string = deployStorage ? storage.outputs.storageAccountName : ''
output cosmosDbAccountName string = deployCosmos ? cosmosDb.outputs.cosmosDbAccountName : ''
output mediaContainerUrl string = deployStorage ? storage.outputs.mediaContainerUrl : ''