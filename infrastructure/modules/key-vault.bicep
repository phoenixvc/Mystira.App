// Key Vault Module for storing secrets including JWT RSA keys
@description('Name of the Key Vault')
param keyVaultName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Object ID of the admin user/service principal for Key Vault access')
param adminObjectId string = ''

@description('Object ID of the App Service managed identity for Key Vault access')
param appServicePrincipalId string = ''

@description('JWT RSA Private Key (PEM format)')
@secure()
param jwtRsaPrivateKey string = ''

@description('JWT RSA Public Key (PEM format)')
@secure()
param jwtRsaPublicKey string = ''

@description('JWT Issuer')
param jwtIssuer string = 'MystiraAPI'

@description('JWT Audience')
param jwtAudience string = 'MystiraPWA'

@description('Discord bot token (from Discord Developer Portal)')
@secure()
param discordBotToken string = ''

@description('Bot Microsoft App ID (for Teams)')
param botMicrosoftAppId string = ''

@description('Bot Microsoft App Password (client secret)')
@secure()
param botMicrosoftAppPassword string = ''

@description('WhatsApp Channel Registration ID (from Azure Portal → ACS → Channels → WhatsApp)')
param whatsAppChannelRegistrationId string = ''

@description('WhatsApp Business Account ID (from Meta Business Suite → Business Settings)')
param whatsAppBusinessAccountId string = ''

@description('WhatsApp Phone Number ID (from Meta Business Suite → Phone Numbers)')
param whatsAppPhoneNumberId string = ''

@description('WhatsApp webhook verification token (random string for verifying webhook requests from Meta)')
@secure()
param whatsAppWebhookVerifyToken string = ''

// Key Vault resource
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

    accessPolicies: concat(
      // Admin access policy (if provided)
      adminObjectId != '' ? [
        {
          tenantId: subscription().tenantId
          objectId: adminObjectId
          permissions: {
            keys: ['get', 'list', 'update', 'create', 'import', 'delete', 'recover', 'backup', 'restore']
            secrets: ['get', 'list', 'set', 'delete', 'recover', 'backup', 'restore']
            certificates: ['get', 'list', 'update', 'create', 'import', 'delete', 'recover', 'backup', 'restore']
          }
        }
      ] : [],
      // App Service managed identity access policy (if provided)
      appServicePrincipalId != '' ? [
        {
          tenantId: subscription().tenantId
          objectId: appServicePrincipalId
          permissions: {
            secrets: ['get', 'list']
          }
        }
      ] : []
    )

    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }

  tags: {
    Purpose: 'Mystira-App-Secrets'
  }
}

// JWT RSA Private Key secret
resource jwtPrivateKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (jwtRsaPrivateKey != '') {
  parent: keyVault
  name: 'JwtSettings--RsaPrivateKey'
  properties: {
    value: jwtRsaPrivateKey
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// JWT RSA Public Key secret
resource jwtPublicKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (jwtRsaPublicKey != '') {
  parent: keyVault
  name: 'JwtSettings--RsaPublicKey'
  properties: {
    value: jwtRsaPublicKey
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// JWT Issuer secret
resource jwtIssuerSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSettings--Issuer'
  properties: {
    value: jwtIssuer
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// JWT Audience secret
resource jwtAudienceSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'JwtSettings--Audience'
  properties: {
    value: jwtAudience
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// Discord Bot Token secret
resource discordBotTokenSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (discordBotToken != '') {
  parent: keyVault
  name: 'Discord--BotToken'
  properties: {
    value: discordBotToken
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// Bot Microsoft App ID secret
resource botAppIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (botMicrosoftAppId != '') {
  parent: keyVault
  name: 'Bot--MicrosoftAppId'
  properties: {
    value: botMicrosoftAppId
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// Bot Microsoft App Password secret
resource botAppPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (botMicrosoftAppPassword != '') {
  parent: keyVault
  name: 'Bot--MicrosoftAppPassword'
  properties: {
    value: botMicrosoftAppPassword
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// WhatsApp Channel Registration ID secret
resource whatsAppChannelRegIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (whatsAppChannelRegistrationId != '') {
  parent: keyVault
  name: 'WhatsApp--ChannelRegistrationId'
  properties: {
    value: whatsAppChannelRegistrationId
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// WhatsApp Business Account ID secret
resource whatsAppBusinessIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (whatsAppBusinessAccountId != '') {
  parent: keyVault
  name: 'WhatsApp--BusinessAccountId'
  properties: {
    value: whatsAppBusinessAccountId
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// WhatsApp Phone Number ID secret
resource whatsAppPhoneIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (whatsAppPhoneNumberId != '') {
  parent: keyVault
  name: 'WhatsApp--PhoneNumberId'
  properties: {
    value: whatsAppPhoneNumberId
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// WhatsApp Webhook Verify Token secret
resource whatsAppVerifyTokenSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (whatsAppWebhookVerifyToken != '') {
  parent: keyVault
  name: 'WhatsApp--WebhookVerifyToken'
  properties: {
    value: whatsAppWebhookVerifyToken
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

// Key Vault reference URIs for App Service configuration
output jwtPrivateKeySecretUri string = jwtRsaPrivateKey != '' ? '@Microsoft.KeyVault(SecretUri=${jwtPrivateKeySecret.properties.secretUri})' : ''
output jwtPublicKeySecretUri string = jwtRsaPublicKey != '' ? '@Microsoft.KeyVault(SecretUri=${jwtPublicKeySecret.properties.secretUri})' : ''
output jwtIssuerSecretUri string = '@Microsoft.KeyVault(SecretUri=${jwtIssuerSecret.properties.secretUri})'
output jwtAudienceSecretUri string = '@Microsoft.KeyVault(SecretUri=${jwtAudienceSecret.properties.secretUri})'
