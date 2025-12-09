// Cosmos DB Module
@description('Name of the Cosmos DB account')
param cosmosDbAccountName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Database name')
param databaseName string = 'MystiraAppDb'

@description('Enable serverless mode')
param serverless bool = true

// Cosmos DB Account
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    capabilities: serverless ? [
      {
        name: 'EnableServerless'
      }
    ] : []
  }
}

// Cosmos DB Database
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosDbAccount
  name: databaseName
  dependsOn: [
    cosmosDbAccount
  ]
  properties: {
    resource: {
      id: databaseName
    }
  }
}

// User Profiles Container
resource userProfilesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'UserProfiles'
  dependsOn: [
    cosmosDatabase
  ]
  properties: {
    resource: {
      id: 'UserProfiles'
      partitionKey: {
        paths: ['/accountId']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

// Accounts Container
resource accountsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'Accounts'
  dependsOn: [
    cosmosDatabase
  ]
  properties: {
    resource: {
      id: 'Accounts'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

// Scenarios Container
resource scenariosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'Scenarios'
  dependsOn: [
    cosmosDatabase
  ]
  properties: {
    resource: {
      id: 'Scenarios'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

// Game Sessions Container
resource gameSessionsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'GameSessions'
  dependsOn: [
    cosmosDatabase
  ]
  properties: {
    resource: {
      id: 'GameSessions'
      partitionKey: {
        paths: ['/accountId']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

// Content Bundles Container
resource contentBundlesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'ContentBundles'
  dependsOn: [
    cosmosDatabase
  ]
  properties: {
    resource: {
      id: 'ContentBundles'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

// Pending Signups Container
resource pendingSignupsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'PendingSignups'
  dependsOn: [
    cosmosDatabase
  ]
  properties: {
    resource: {
      id: 'PendingSignups'
      partitionKey: {
        paths: ['/email']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

// Compass Trackings Container
resource compassTrackingsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'CompassTrackings'
  dependsOn: [
    cosmosDatabase
  ]
  properties: {
    resource: {
      id: 'CompassTrackings'
      partitionKey: {
        paths: ['/Axis']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbAccountId string = cosmosDbAccount.id
output cosmosDbConnectionString string = listConnectionStrings(cosmosDbAccount.id, cosmosDbAccount.apiVersion).connectionStrings[0].connectionString
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output databaseName string = cosmosDatabase.name
