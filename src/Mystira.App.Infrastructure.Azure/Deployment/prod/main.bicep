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

@description('Object ID for Key Vault admin access')
param keyVaultAdminObjectId string // ⚠️ REQUIRED: Provide the Object ID of the user/service principal for Key Vault access

// Variables - ⚠️ UPDATE: Change 'mystira' to your app name
var resourcePrefix = '${environment}-${shortLocation}-app-mystira' // ⚠️ UPDATE: Change 'mystira' prefix to your app name
var cosmosDbName = replace('${resourcePrefix}cosmos', '-', '')  // Remove hyphens for Cosmos DB name

// Deploy Key Vault for Story Protocol secrets
module keyVault 'key-vault.bicep' = {
  name: 'keyvault-deployment'
  params: {
    environment: environment
    location: location
    shortLocation: shortLocation
    keyVaultAdminObjectId: keyVaultAdminObjectId
  }
}

// Deploy Storage Account
module storage 'storage.bicep' = {
  name: 'storage-deployment'
  params: {
    storageAccountName: '${replace(resourcePrefix, '-', '')}storage'
    location: location
    sku: 'Standard_LRS' // Always use cheapest option (LRS)
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
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

// Outputs
output appServiceUrl string = appService.outputs.appServiceUrl
output storageAccountName string = storage.outputs.storageAccountName
output cosmosDbAccountName string = cosmosDb.outputs.cosmosDbAccountName
output mediaContainerUrl string = storage.outputs.mediaContainerUrloutput keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
