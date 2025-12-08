// ⚠️ CONFIGURATION REQUIRED: Update parameters for your deployment

@description('Environment name (dev, staging, prod)')
param environment string = 'staging' // ⚠️ UPDATE: Set to 'dev', 'staging', or 'prod'

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
@description('Deploy Static Web App (PWA)')
param deployStaticWebApp bool = true

@description('GitHub repository token for SWA deployment')
@secure()
param githubToken string = ''

@description('Object ID for Key Vault admin access (optional)')
param keyVaultAdminObjectId string = ''

@description('Location short name')
param shortLocation string = 'san'

// Variables - Standardized naming: {env}-{location}-app-{name}
var resourcePrefix = '${environment}-san-app-mystira' // Standardized format: {env}-{location}-app-{name}
var cosmosDbName = replace('${resourcePrefix}cosmos', '-', '')  // Remove hyphens for Cosmos DB name
var appServiceName = '${resourcePrefix}-api' // App Service name with -api suffix
var staticWebAppName = '${resourcePrefix}-pwa' // Static Web App name with -pwa suffix

// Deploy Key Vault for Story Protocol secrets (conditional - only if keyVaultAdminObjectId is provided)
module keyVault 'key-vault.bicep' = if (keyVaultAdminObjectId != '') {
  name: 'keyvault-deployment'
  params: {
    environment: environment
    location: location
    shortLocation: shortLocation
    keyVaultAdminObjectId: keyVaultAdminObjectId
  }
}

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
    cosmosDbConnectionString: deployCosmos ? cosmosDb!.outputs.cosmosDbConnectionString : ''
    storageConnectionString: deployStorage ? storage!.outputs.storageConnectionString : ''
    jwtSecretKey: jwtSecretKey
    keyVaultName: (keyVaultAdminObjectId != '') ? keyVault!.outputs.keyVaultName : ''
  }
}

// Deploy Static Web App (PWA) for Staging (conditional)
module staticWebApp 'static-web-app.bicep' = if (deployStaticWebApp && githubToken != '') {
  name: 'staticwebapp-deployment'
  params: {
    staticWebAppName: staticWebAppName
    environment: environment
    location: location
    repositoryUrl: 'https://github.com/phoenixvc/Mystira.App'
    repositoryBranch: 'staging'
    repositoryToken: githubToken
    tags: {
      Environment: environment
      Project: 'Mystira'
    }
  }
}

// Outputs (conditional based on what was deployed)
// Use null-forgiving operator (!) since we check the condition before accessing
output appServiceUrl string = deployAppService ? appService!.outputs.appServiceUrl : ''
output storageAccountName string = deployStorage ? storage!.outputs.storageAccountName : ''
output cosmosDbAccountName string = deployCosmos ? cosmosDb!.outputs.cosmosDbAccountName : ''
output mediaContainerUrl string = deployStorage ? storage!.outputs.mediaContainerUrl : ''
output keyVaultName string = (keyVaultAdminObjectId != '') ? keyVault!.outputs.keyVaultName : ''
output keyVaultUri string = (keyVaultAdminObjectId != '') ? keyVault!.outputs.keyVaultUri : ''
output staticWebAppUrl string = (deployStaticWebApp && githubToken != '') ? staticWebApp!.outputs.staticWebAppUrl : ''
output staticWebAppHostname string = (deployStaticWebApp && githubToken != '') ? staticWebApp!.outputs.staticWebAppDefaultHostname : ''
output staticWebAppDeploymentToken string = (deployStaticWebApp && githubToken != '') ? staticWebApp!.outputs.deploymentToken : ''
output appInsightsConnectionString string = (deployStaticWebApp && githubToken != '') ? staticWebApp!.outputs.appInsightsConnectionString : ''
