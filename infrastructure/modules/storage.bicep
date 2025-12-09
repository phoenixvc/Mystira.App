// Storage Account Module
@description('Name of the storage account')
param storageAccountName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Storage account SKU')
param sku string = 'Standard_LRS'

@description('Allowed CORS origins for blob storage')
param corsAllowedOrigins array = []

// Storage Account
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

// Blob Service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: length(corsAllowedOrigins) > 0 ? [
        {
          allowedOrigins: corsAllowedOrigins
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'HEAD']
          allowedHeaders: ['*']
          exposedHeaders: ['*']
          maxAgeInSeconds: 3600
        }
      ] : []
    }
  }
}

// Media Container
resource mediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'mystira-app-media'
  properties: {
    publicAccess: 'Blob'
  }
}

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output storageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value};EndpointSuffix=core.windows.net'
output primaryEndpoints object = storageAccount.properties.primaryEndpoints
output mediaContainerUrl string = '${storageAccount.properties.primaryEndpoints.blob}mystira-app-media/'
