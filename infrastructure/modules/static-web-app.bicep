// Azure Static Web App Module
// Note: Static Web Apps are NOT available in South Africa North
// This module deploys to a fallback region (e.g., eastus2, westeurope)

@description('Name of the Static Web App')
param staticWebAppName string

@description('Location for the Static Web App (must be a supported region)')
@allowed([
  'centralus'
  'eastus2'
  'eastasia'
  'westeurope'
  'westus2'
])
param location string = 'eastus2'

@description('SKU for the Static Web App')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Free'

@description('GitHub repository URL (optional - can be linked later via Azure Portal)')
param repositoryUrl string = ''

@description('GitHub branch to deploy from')
param branch string = 'dev'

@description('App location within the repository (e.g., "/" or "/src/pwa")')
param appLocation string = '/src/Mystira.App.PWA'

@description('API location within the repository (leave empty if no API)')
param apiLocation string = ''

@description('Output location for built app')
param outputLocation string = 'wwwroot'

@description('Tags for all resources')
param tags object = {}

// Static Web App resource
resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    repositoryUrl: repositoryUrl != '' ? repositoryUrl : null
    branch: repositoryUrl != '' ? branch : null
    buildProperties: repositoryUrl != '' ? {
      appLocation: appLocation
      apiLocation: apiLocation
      outputLocation: outputLocation
    } : null
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

// Outputs
output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
// Note: Deployment token must be retrieved separately via Azure CLI or Portal
// Use: az staticwebapp secrets list --name <name> --query "properties.apiKey" -o tsv
