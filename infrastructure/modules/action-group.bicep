// Action Group Module
// Defines notification channels for alerts (email, webhook, etc.)

@description('Name of the action group')
param actionGroupName string

@description('Short name for the action group (max 12 chars)')
@maxLength(12)
param actionGroupShortName string

@description('Email addresses to receive alert notifications')
param emailReceivers array = []

@description('Webhook URLs to receive alert notifications')
param webhookReceivers array = []

@description('Tags for all resources')
param tags object = {}

@description('Enable the action group')
param enabled bool = true

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'Global' // Action groups are always global
  tags: tags
  properties: {
    groupShortName: actionGroupShortName
    enabled: enabled
    emailReceivers: [for (email, i) in emailReceivers: {
      name: 'email-${i}'
      emailAddress: email
      useCommonAlertSchema: true
    }]
    webhookReceivers: [for (webhook, i) in webhookReceivers: {
      name: 'webhook-${i}'
      serviceUri: webhook
      useCommonAlertSchema: true
    }]
    // Azure App push notifications (optional, for mobile alerts)
    azureAppPushReceivers: []
    // SMS receivers (optional, requires additional setup)
    smsReceivers: []
    // Voice call receivers (optional, requires additional setup)
    voiceReceivers: []
    // Logic App receivers (optional, for advanced automation)
    logicAppReceivers: []
    // Azure Function receivers (optional, for custom processing)
    azureFunctionReceivers: []
    // Event Hub receivers (optional, for streaming alerts)
    eventHubReceivers: []
    // ITSM receivers (optional, for ServiceNow, etc.)
    itsmReceivers: []
    // ARM role receivers (optional, for role-based notifications)
    armRoleReceivers: []
    // Automation runbook receivers (optional)
    automationRunbookReceivers: []
  }
}

output actionGroupId string = actionGroup.id
output actionGroupName string = actionGroup.name
