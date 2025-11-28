@description('Name of the Cosmos DB account')
param cosmosDbAccountName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Database name')
param databaseName string = 'MystiraAppDb'

// Cosmos DB Account (Azure Cloud Database)
// Using 2024-05-15 API version to improve what-if deployment preview compatibility
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
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

// Cosmos DB Database (Azure Cloud Database)
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosDbAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

// User Profiles Container (Azure Cloud Database)
resource userProfilesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'UserProfiles'
  properties: {
    resource: {
      id: 'UserProfiles'
      partitionKey: {
        paths: ['/Name']
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

// Scenarios Container (Azure Cloud Database)
resource scenariosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'Scenarios'
  properties: {
    resource: {
      id: 'Scenarios'
      partitionKey: {
        paths: ['/Id']
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

// Game Sessions Container (Azure Cloud Database)
resource gameSessionsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'GameSessions'
  properties: {
    resource: {
      id: 'GameSessions'
      partitionKey: {
        paths: ['/DmName']
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

// Compass Trackings Container (Azure Cloud Database)
resource compassTrackingsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: cosmosDatabase
  name: 'CompassTrackings'
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

// Outputs
output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbAccountId string = cosmosDbAccount.id
output cosmosDbResourceGroup string = resourceGroup().name
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output databaseName string = databaseName
#disable-next-line outputs-should-not-contain-secrets use-resource-symbol-reference
output cosmosDbConnectionString string = cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString