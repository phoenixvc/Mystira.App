// Azure Communication Services Module
// Supports Email, SMS, and WhatsApp messaging channels

@description('Name of the Communication Service')
param communicationServiceName string

@description('Name of the Email Communication Service')
param emailServiceName string

@description('Domain name for email service')
param domainName string

@description('Location for the resources (Communication Services are global)')
param location string = 'global'

@description('Data location for Communication Services')
param dataLocation string = 'Europe'

@description('Enable WhatsApp channel (requires Meta Business verification)')
param enableWhatsApp bool = false

@description('WhatsApp phone number ID (from Meta Business Suite, required if enableWhatsApp is true)')
param whatsAppPhoneNumberId string = ''

@description('Tags for all resources')
param tags object = {}

// Communication Service
resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServiceName
  location: location
  tags: tags
  properties: {
    dataLocation: dataLocation
    // Note: WhatsApp channel must be configured manually through Azure Portal
    // after Meta Business verification is complete. See docs/ops/WHATSAPP_WEBHOOK_SETUP.md
  }
}

// Email Communication Service
resource emailCommunicationService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServiceName
  location: location
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

// Email Domain
resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailCommunicationService
  name: domainName
  location: location
  properties: {
    domainManagement: 'CustomerManaged'
    userEngagementTracking: 'Disabled'
  }
}

// Link Communication Service with Email Service
resource senderUsername 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-04-01' = {
  parent: emailDomain
  name: 'DoNotReply'
  properties: {
    username: 'DoNotReply'
    displayName: 'Mystira'
  }
}

// Outputs
output communicationServiceId string = communicationService.id
output communicationServiceName string = communicationService.name

// Note: Connection string is marked as secure to prevent exposure in deployment logs
#disable-next-line outputs-should-not-contain-secrets
output communicationServiceConnectionString string = communicationService.listKeys().primaryConnectionString
output emailServiceId string = emailCommunicationService.id
output emailServiceName string = emailCommunicationService.name
output emailDomainId string = emailDomain.id
output emailDomainName string = emailDomain.name
output senderEmail string = 'DoNotReply@${domainName}'
output whatsAppEnabled bool = enableWhatsApp
output whatsAppPhoneNumberId string = whatsAppPhoneNumberId
