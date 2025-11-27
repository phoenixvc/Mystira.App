@description('Name of the storage account')
param storageAccountName string = 'mystiraappstorage'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Storage account SKU')
param sku string = 'Standard_LRS'

// Storage Account (Azure Cloud Storage)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: sku
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Blob Service (Azure Cloud Storage)
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: ['https://mystiraapp.azurewebsites.net', 'https://localhost:7000']
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders: ['*']
          exposedHeaders: ['*']
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

// Media Container (Azure Cloud Storage)
resource mediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'mystira-app-media'
  properties: {
    publicAccess: 'Blob'
  }
}

// Outputs
output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output storageAccountResourceGroup string = resourceGroup().name
output mediaContainerUrl string = '${storageAccount.properties.primaryEndpoints.blob}mystira-app-media/'
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
#disable-next-line outputs-should-not-contain-secrets
output storageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
