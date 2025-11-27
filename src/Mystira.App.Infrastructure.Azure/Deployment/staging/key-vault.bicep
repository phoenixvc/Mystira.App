// Azure Key Vault deployment for Story Protocol private keys and secrets
@description('Environment name (dev, staging, prod)')
param environment string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Location short name')
param shortLocation string

@description('Object ID of the user or service principal that will have access to Key Vault')
param keyVaultAdminObjectId string

// Variables
var resourcePrefix = '${environment}-${shortLocation}-app-mystira'
var keyVaultName = '${replace(resourcePrefix, '-', '')}kv'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    enabledForDeployment: true
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: keyVaultAdminObjectId
        permissions: {
          keys: [
            'get'
            'list'
            'update'
            'create'
            'import'
            'delete'
            'recover'
            'backup'
            'restore'
          ]
          secrets: [
            'get'
            'list'
            'set'
            'delete'
            'recover'
            'backup'
            'restore'
          ]
          certificates: [
            'get'
            'list'
            'update'
            'create'
            'import'
            'delete'
            'recover'
            'backup'
            'restore'
          ]
        }
      }
    ]
    
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
  
  tags: {
    Environment: environment
    Purpose: 'StoryProtocol-Secrets'
  }
}

// Store placeholder for Story Protocol private key
resource storyProtocolPrivateKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'StoryProtocol--PrivateKey'
  properties: {
    value: 'PLACEHOLDER-UPDATE-WITH-ACTUAL-PRIVATE-KEY'
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// Store RPC URL secret
resource storyProtocolRpcUrl 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'StoryProtocol--RpcUrl'
  properties: {
    value: 'https://rpc.testnet.story.foundation'
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// Outputs
output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
