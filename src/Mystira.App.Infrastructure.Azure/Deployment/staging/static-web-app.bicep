// Staging Static Web App Bicep Module
// This module creates and configures the Azure Static Web App for Staging environment

@description('Environment name')
param environment string = 'staging'

@description('Location for the Static Web App')
param location string = 'westeurope'

@description('Static Web App name')
param staticWebAppName string

@description('GitHub repository URL')
param repositoryUrl string = 'https://github.com/phoenixvc/Mystira.App'

@description('GitHub repository branch')
param repositoryBranch string = 'staging'

@description('GitHub token for deployment (from GitHub secret)')
@secure()
param repositoryToken string

@description('Tags to apply to resources')
param tags object = {}

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  tags: union(tags, {
    Environment: environment
    Component: 'PWA'
    ManagedBy: 'Bicep'
  })
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: repositoryBranch
    repositoryToken: repositoryToken
    buildProperties: {
      appLocation: './src/Mystira.App.PWA'
      apiLocation: ''
      outputLocation: 'wwwroot'
      skipGithubActionWorkflowGeneration: true  // We have our own workflow
    }
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
  }
}

// Application Insights for monitoring
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${staticWebAppName}-insights'
  location: location
  kind: 'web'
  tags: union(tags, {
    Environment: environment
    Component: 'Monitoring'
  })
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    RetentionInDays: 90
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Link Application Insights to Static Web App
resource swaAppSettings 'Microsoft.Web/staticSites/config@2023-01-01' = {
  name: 'appsettings'
  parent: staticWebApp
  properties: {
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
    'Blazor-Environment': 'Staging'
  }
}

// Budget alert for cost monitoring
resource budget 'Microsoft.Consumption/budgets@2023-05-01' = {
  name: '${staticWebAppName}-budget'
  properties: {
    timePeriod: {
      startDate: '2025-01-01'
      endDate: '2026-12-31'
    }
    timeGrain: 'Monthly'
    amount: 100
    category: 'Cost'
    notifications: {
      Actual_GreaterThan_80_Percent: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 80
        contactEmails: [
          'devops@mystira.app'
        ]
        thresholdType: 'Actual'
      }
    }
    filter: {
      dimensions: {
        name: 'ResourceId'
        operator: 'In'
        values: [
          staticWebApp.id
        ]
      }
    }
  }
  scope: resourceGroup()
}

output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output deploymentToken string = listSecrets(staticWebApp.id, staticWebApp.apiVersion).properties.apiKey
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
