// Azure Bot Service Module for Teams/Discord/Multi-platform chat bots
// Supports Multi-tenant Azure Bot with Microsoft App registration

@description('Name of the Azure Bot resource')
param botName string

@description('Display name of the bot')
param botDisplayName string = 'Mystira Bot'

@description('Microsoft App ID for the bot (from Azure AD App Registration)')
param microsoftAppId string

@description('Bot endpoint URL (messaging endpoint)')
param botEndpoint string

@description('Location for the resources')
param location string = 'global'

@description('SKU for the bot (F0 = Free, S1 = Standard)')
@allowed([
  'F0'
  'S1'
])
param sku string = 'F0'

@description('Application Insights Instrumentation Key for bot telemetry')
param appInsightsInstrumentationKey string = ''

@description('Enable Teams channel')
param enableTeamsChannel bool = true

@description('Enable Web Chat channel')
param enableWebChatChannel bool = true

@description('Tags for all resources')
param tags object = {}

// Azure Bot resource
resource azureBot 'Microsoft.BotService/botServices@2022-09-15' = {
  name: botName
  location: location
  kind: 'azurebot'
  sku: {
    name: sku
  }
  tags: tags
  properties: {
    displayName: botDisplayName
    endpoint: botEndpoint
    msaAppId: microsoftAppId
    msaAppType: 'MultiTenant'
    msaAppTenantId: ''
    developerAppInsightKey: appInsightsInstrumentationKey != '' ? appInsightsInstrumentationKey : null
    luisAppIds: []
    isCmekEnabled: false
    isStreamingSupported: false
    schemaTransformationVersion: '1.3'
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

// Teams Channel
resource teamsChannel 'Microsoft.BotService/botServices/channels@2022-09-15' = if (enableTeamsChannel) {
  parent: azureBot
  name: 'MsTeamsChannel'
  location: location
  properties: {
    channelName: 'MsTeamsChannel'
    properties: {
      enableCalling: false
      isEnabled: true
    }
  }
}

// Web Chat Channel (for testing and embedding)
resource webChatChannel 'Microsoft.BotService/botServices/channels@2022-09-15' = if (enableWebChatChannel) {
  parent: azureBot
  name: 'WebChatChannel'
  location: location
  properties: {
    channelName: 'WebChatChannel'
    properties: {
      sites: [
        {
          siteName: 'Default'
          isEnabled: true
          isWebchatPreviewEnabled: true
        }
      ]
    }
  }
}

// Direct Line Channel (for custom clients)
resource directLineChannel 'Microsoft.BotService/botServices/channels@2022-09-15' = {
  parent: azureBot
  name: 'DirectLineChannel'
  location: location
  properties: {
    channelName: 'DirectLineChannel'
    properties: {
      sites: [
        {
          siteName: 'Default'
          isEnabled: true
          isV1Enabled: false
          isV3Enabled: true
        }
      ]
    }
  }
}

// Outputs
output botId string = azureBot.id
output botName string = azureBot.name
output botEndpoint string = azureBot.properties.endpoint
output teamsChannelEnabled bool = enableTeamsChannel
output webChatChannelEnabled bool = enableWebChatChannel
