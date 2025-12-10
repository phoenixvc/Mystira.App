// Mystira App Infrastructure
// Naming Convention: [org]-[env]-[project]-[type]-[region]
// Resource Groups:   [org]-[env]-[project]-rg-[region]
// Usage: az deployment group create --template-file main.bicep --parameters @params.<env>.json

targetScope = 'resourceGroup'

// ─────────────────────────────────────────────────────────────────
// Naming Convention Parameters
// Pattern: [org]-[env]-[project]-[type]-[region]
// ─────────────────────────────────────────────────────────────────

@description('Organisation code')
@allowed([
  'mys'   // Mystira (Eben)
  'nl'    // NeuralLiquid (Jurie)
  'pvc'   // Phoenix VC (Eben)
  'tws'   // Twines & Straps (Martyn)
])
param org string = 'mys'

@description('Environment name')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string

@description('Project name')
param project string = 'mystira'

@description('Short region code')
@allowed([
  'san'   // South Africa North (southafricanorth) - PRIMARY
  'euw'   // West Europe (westeurope)
  'eun'   // North Europe (northeurope)
  'wus'   // West US (westus)
  'eus'   // East US (eastus)
  'eus2'  // East US 2 (eastus2) - FALLBACK for SWA/global services
  'swe'   // Sweden Central (swedencentral)
  'uks'   // UK South (uksouth)
  'usw'   // US West 2 (westus2)
  'glob'  // Global / regionless
])
param region string = 'san'

@description('Azure location for resources')
param location string = 'southafricanorth'

@description('Fallback region for services not available in primary region (e.g., Static Web Apps)')
param fallbackRegion string = 'eastus2'

@description('Tags applied to all resources')
param tags object = {
  org: org
  environment: environment
  project: project
  application: 'mystira-app'
}

// ─────────────────────────────────────────────────────────────────
// Naming Helper Variables
// Pattern: [org]-[env]-[project]-[type]-[region]
// ─────────────────────────────────────────────────────────────────

var namePrefix = '${org}-${environment}-${project}'

// Fallback region code for services not available in South Africa North
var fallbackRegionCode = fallbackRegion == 'eastus2' ? 'eus2' : (fallbackRegion == 'westeurope' ? 'euw' : 'eus2')

// Short environment code for resources with strict name limits (Key Vault: 24 chars max)
var envShort = environment == 'staging' ? 'stg' : (environment == 'prod' ? 'prd' : 'dev')

// Resource names following convention
var names = {
  // Monitoring
  logAnalytics: '${namePrefix}-log-${region}'
  appInsights: '${namePrefix}-appins-${region}'
  actionGroup: '${namePrefix}-alerts-${region}'
  actionGroupShort: '${org}${envShort}alerts' // Max 12 chars for action group short name
  dashboard: '${namePrefix}-dashboard-${region}'
  budget: '${namePrefix}-budget-${region}'

  // Communication (global service)
  communicationService: '${namePrefix}-acs-glob'
  emailService: '${namePrefix}-email-glob'

  // Bot (global service)
  azureBot: '${namePrefix}-bot-glob'

  // Storage & Data
  // Note: Storage accounts have strict naming (lowercase, no dashes, 3-24 chars)
  storageAccount: replace(toLower('${org}${environment}${project}st${region}'), '-', '')
  cosmosDb: '${namePrefix}-cosmos-${region}'

  // App Services (share a single App Service Plan to reduce costs)
  appServicePlan: '${namePrefix}-plan-${region}'
  apiApp: '${namePrefix}-api-${region}'
  adminApiApp: '${namePrefix}-adminapi-${region}'

  // Static Web App (NOT available in South Africa North - uses fallback region)
  staticWebApp: '${namePrefix}-swa-${fallbackRegionCode}'

  // Security (Key Vault has 24 char limit, use shorter env code)
  // Format: [org]-[envShort]-[project]-kv-[region]
  keyVault: '${org}-${envShort}-${project}-kv-${region}'
}

// ─────────────────────────────────────────────────────────────────
// SKU Parameters (vary by environment)
// ─────────────────────────────────────────────────────────────────

@description('App Service Plan SKU')
@allowed([
  'F1'   // Free (dev only)
  'B1'   // Basic
  'B2'
  'B3'
  'S1'   // Standard
  'S2'
  'S3'
  'P1v3' // Premium v3
  'P2v3'
  'P3v3'
])
param appServiceSku string = 'F1'

@description('Storage account SKU')
@allowed([
  'Standard_LRS'  // Dev
  'Standard_GRS'  // Staging
  'Standard_RAGRS' // Prod
])
param storageSku string = 'Standard_LRS'

@description('Enable Cosmos DB serverless mode (recommended for dev)')
param cosmosServerless bool = true

@description('Log Analytics data retention in days')
param logRetentionDays int = 30

@description('Log Analytics daily quota in GB (-1 for unlimited)')
param logDailyQuotaGb int = 1

// ─────────────────────────────────────────────────────────────────
// JWT/Authentication Parameters
// ─────────────────────────────────────────────────────────────────

@description('JWT RSA Private Key (PEM format) for RS256 signing')
@secure()
param jwtRsaPrivateKey string

@description('JWT RSA Public Key (PEM format) for RS256 verification')
@secure()
param jwtRsaPublicKey string

@description('JWT Issuer')
param jwtIssuer string = 'MystiraAPI'

@description('JWT Audience')
param jwtAudience string = 'MystiraPWA'

// ─────────────────────────────────────────────────────────────────
// Communication Services Parameters
// ─────────────────────────────────────────────────────────────────

@description('Azure Communication Services connection string (for existing ACS)')
@secure()
param acsConnectionString string = ''

@description('Sender email address for Azure Communication Services')
param acsSenderEmail string = 'DoNotReply@mystira.app'

@description('Email domain name')
param emailDomainName string = 'mystira.app'

@description('Allowed CORS origins for APIs')
param corsAllowedOrigins string

// ─────────────────────────────────────────────────────────────────
// Conditional Deployment Flags (for existing resources)
// ─────────────────────────────────────────────────────────────────

@description('Skip storage account creation if it already exists')
param skipStorageCreation bool = false

@description('Connection string for existing storage account')
@secure()
param existingStorageConnectionString string = ''

@description('Name of existing storage account')
param existingStorageAccountName string = ''

@description('Skip Communication Services creation if it already exists')
param skipCommServiceCreation bool = false

@description('Skip Cosmos DB creation if it already exists')
param skipCosmosCreation bool = false

@description('Name of existing Cosmos DB account')
param existingCosmosDbAccountName string = ''

@description('Connection string for existing Cosmos DB')
@secure()
param existingCosmosConnectionString string = ''

@description('Skip App Service creation if they already exist')
param skipAppServiceCreation bool = false

// ─────────────────────────────────────────────────────────────────
// Azure Bot Parameters (optional Teams integration)
// ─────────────────────────────────────────────────────────────────

@description('Deploy Azure Bot for Teams integration')
param deployAzureBot bool = false

@description('Microsoft App ID for the bot (from Azure AD App Registration)')
param botMicrosoftAppId string = ''

@description('Microsoft App Password (client secret from Azure AD)')
@secure()
param botMicrosoftAppPassword string = ''

@description('Bot messaging endpoint URL')
param botEndpoint string = ''

@description('Bot SKU (F0 = Free, S1 = Standard)')
@allowed([
  'F0'
  'S1'
])
param botSku string = 'F0'

// ─────────────────────────────────────────────────────────────────
// WhatsApp Parameters (via ACS)
// Note: Requires Meta Business verification before channel can be used
// ─────────────────────────────────────────────────────────────────

@description('Enable WhatsApp channel in Communication Services')
param enableWhatsApp bool = false

@description('WhatsApp phone number ID (from Meta Business Suite → Phone Numbers)')
param whatsAppPhoneNumberId string = ''

@description('WhatsApp Channel Registration ID (from Azure Portal → ACS → Channels → WhatsApp)')
param whatsAppChannelRegistrationId string = ''

@description('WhatsApp Business Account ID (from Meta Business Suite → Business Settings)')
param whatsAppBusinessAccountId string = ''

@description('WhatsApp webhook verification token (random string for verifying webhook requests from Meta)')
@secure()
param whatsAppWebhookVerifyToken string = ''

// ─────────────────────────────────────────────────────────────────
// Discord Bot Parameters
// ─────────────────────────────────────────────────────────────────

@description('Discord bot token (from Discord Developer Portal → Bot → Token)')
@secure()
param discordBotToken string = ''

// ─────────────────────────────────────────────────────────────────
// Key Vault Admin Parameters
// ─────────────────────────────────────────────────────────────────

@description('Object ID of the admin user/service principal for Key Vault full access (Get from Azure Portal → Microsoft Entra ID → Users → Your User → Object ID)')
param adminObjectId string = ''

// ─────────────────────────────────────────────────────────────────
// Static Web App Parameters
// Note: SWA is NOT available in South Africa North - deploys to fallback region
// ─────────────────────────────────────────────────────────────────

@description('Deploy Static Web App (uses fallback region since SWA is not available in South Africa North)')
param deployStaticWebApp bool = false

@description('Static Web App SKU')
@allowed([
  'Free'
  'Standard'
])
param staticWebAppSku string = 'Free'

@description('GitHub repository URL for Static Web App (optional - can be linked later)')
param staticWebAppRepositoryUrl string = ''

@description('GitHub branch for Static Web App deployment')
param staticWebAppBranch string = 'dev'

// ─────────────────────────────────────────────────────────────────
// DNS / Custom Domain Parameters
// ─────────────────────────────────────────────────────────────────

@description('Enable custom domain for Static Web App')
param enableCustomDomain bool = false

@description('DNS zone name (e.g., mystira.app)')
param dnsZoneName string = 'mystira.app'

@description('Resource group where the DNS zone exists (defaults to current RG)')
param dnsZoneResourceGroup string = ''

@description('Subdomain for the environment (empty for apex/prod, "dev" for dev, "staging" for staging)')
param customDomainSubdomain string = ''

@description('Enable custom domain for API App Services')
param enableApiCustomDomain bool = false

@description('API subdomain prefix (e.g., "api" for api.mystira.app or api.dev.mystira.app)')
param apiSubdomainPrefix string = 'api'

@description('Admin API subdomain prefix (e.g., "admin" for admin.mystira.app)')
param adminApiSubdomainPrefix string = 'admin'

@description('Enable managed SSL certificates for custom domains (requires hostname binding first)')
param enableManagedCert bool = false

// ─────────────────────────────────────────────────────────────────
// Monitoring & Alerting Parameters
// ─────────────────────────────────────────────────────────────────

@description('Enable metric alerts (recommended to disable for dev to reduce noise)')
param enableAlerts bool = true

@description('Enable availability tests')
param enableAvailabilityTests bool = true

@description('Email addresses for alert notifications (comma-separated or array)')
param alertEmailReceivers array = []

@description('Webhook URLs for alert notifications')
param alertWebhookReceivers array = []

@description('Deploy Azure Dashboard for monitoring')
param deployDashboard bool = true

@description('Deploy budget alerts for cost management')
param deployBudget bool = true

@description('Monthly budget amount in USD')
param monthlyBudget int = 100

// ─────────────────────────────────────────────────────────────────
// Computed Values
// ─────────────────────────────────────────────────────────────────

var storageAccountName = skipStorageCreation ? 'placeholder' : names.storageAccount
var cosmosConnString = skipCosmosCreation && existingCosmosConnectionString != '' ? existingCosmosConnectionString : ''
var storageConnString = skipStorageCreation && existingStorageConnectionString != '' ? existingStorageConnectionString : ''

// Key Vault URI for App Service secret references
var keyVaultUriComputed = 'https://${names.keyVault}${az.environment().suffixes.keyvaultDns}/'

// DNS Zone resource group (defaults to current RG if not specified)
var dnsZoneRg = dnsZoneResourceGroup == '' ? resourceGroup().name : dnsZoneResourceGroup

// Custom domain name (e.g., "dev.mystira.app" or "mystira.app" for apex)
var customDomainFull = customDomainSubdomain == '' ? dnsZoneName : '${customDomainSubdomain}.${dnsZoneName}'

// API custom domains (e.g., "api.dev.mystira.app" or "api.mystira.app" for prod)
var apiCustomDomain = customDomainSubdomain == '' ? '${apiSubdomainPrefix}.${dnsZoneName}' : '${apiSubdomainPrefix}.${customDomainSubdomain}.${dnsZoneName}'
var adminApiCustomDomain = customDomainSubdomain == '' ? '${adminApiSubdomainPrefix}.${dnsZoneName}' : '${adminApiSubdomainPrefix}.${customDomainSubdomain}.${dnsZoneName}'

// ─────────────────────────────────────────────────────────────────
// Module Deployments
// ─────────────────────────────────────────────────────────────────

// Log Analytics Workspace
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'deploy-log-analytics'
  params: {
    workspaceName: names.logAnalytics
    location: location
    retentionInDays: logRetentionDays
    dailyQuotaGb: logDailyQuotaGb
  }
}

// Application Insights
module appInsights 'modules/application-insights.bicep' = {
  name: 'deploy-app-insights'
  params: {
    appInsightsName: names.appInsights
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// Action Group for Alert Notifications
module actionGroup 'modules/action-group.bicep' = if (enableAlerts && length(alertEmailReceivers) > 0) {
  name: 'deploy-action-group'
  params: {
    actionGroupName: names.actionGroup
    actionGroupShortName: names.actionGroupShort
    emailReceivers: alertEmailReceivers
    webhookReceivers: alertWebhookReceivers
    tags: tags
    enabled: true
  }
}

// Metric Alerts (HTTP errors, slow response, CPU/Memory, etc.)
module metricAlerts 'modules/metric-alerts.bicep' = if (enableAlerts && length(alertEmailReceivers) > 0 && !skipAppServiceCreation) {
  name: 'deploy-metric-alerts'
  dependsOn: [actionGroup, apiAppService]
  params: {
    environment: environment
    project: project
    appInsightsId: appInsights.outputs.appInsightsId
    appServiceId: apiAppService.outputs.appServicePlanId
    actionGroupId: enableAlerts && length(alertEmailReceivers) > 0 ? actionGroup.outputs.actionGroupId : ''
    tags: tags
    enableAlerts: enableAlerts
  }
}

// Availability Tests (synthetic monitoring)
module availabilityTests 'modules/availability-tests.bicep' = if (enableAvailabilityTests && length(alertEmailReceivers) > 0 && !skipAppServiceCreation) {
  name: 'deploy-availability-tests'
  dependsOn: [actionGroup, apiAppService]
  params: {
    environment: environment
    project: project
    location: location
    appInsightsId: appInsights.outputs.appInsightsId
    actionGroupId: enableAlerts && length(alertEmailReceivers) > 0 ? actionGroup.outputs.actionGroupId : ''
    apiBaseUrl: 'https://${apiAppService.outputs.appServiceDefaultHostname}'
    pwaBaseUrl: deployStaticWebApp ? 'https://${staticWebApp.outputs.staticWebAppDefaultHostname}' : ''
    tags: tags
    enableAvailabilityTests: enableAvailabilityTests
    testFrequencySeconds: environment == 'prod' ? 300 : 600 // 5 min for prod, 10 min for others
  }
}

// Security Alerts (brute force detection, rate limit monitoring, etc.)
module securityAlerts 'modules/security-alerts.bicep' = if (enableAlerts && length(alertEmailReceivers) > 0) {
  name: 'deploy-security-alerts'
  dependsOn: [actionGroup]
  params: {
    environment: environment
    project: project
    appInsightsId: appInsights.outputs.appInsightsId
    actionGroupId: enableAlerts && length(alertEmailReceivers) > 0 ? actionGroup.outputs.actionGroupId : ''
    tags: tags
    enableSecurityAlerts: enableAlerts
  }
}

// Cosmos DB Alerts (RU consumption, throttling, latency, errors)
module cosmosAlerts 'modules/cosmos-alerts.bicep' = if (enableAlerts && length(alertEmailReceivers) > 0 && !skipCosmosCreation) {
  name: 'deploy-cosmos-alerts'
  dependsOn: [actionGroup, cosmosDb]
  params: {
    environment: environment
    project: project
    cosmosDbId: cosmosDb.outputs.cosmosDbAccountId
    actionGroupId: enableAlerts && length(alertEmailReceivers) > 0 ? actionGroup.outputs.actionGroupId : ''
    tags: tags
    enableCosmosAlerts: enableAlerts
  }
}

// Monitoring Dashboard
module monitoringDashboard 'modules/dashboard.bicep' = if (deployDashboard) {
  name: 'deploy-monitoring-dashboard'
  params: {
    dashboardName: names.dashboard
    location: location
    appInsightsId: appInsights.outputs.appInsightsId
    appInsightsName: appInsights.outputs.appInsightsName
    tags: tags
    environment: environment
    project: project
  }
}

// Budget Alerts for Cost Management
module budgetAlerts 'modules/budget.bicep' = if (deployBudget && length(alertEmailReceivers) > 0) {
  name: 'deploy-budget-alerts'
  params: {
    budgetName: names.budget
    monthlyBudget: monthlyBudget
    alertEmailReceivers: alertEmailReceivers
    tags: tags
    enableBudget: deployBudget
  }
}

// Key Vault (stores JWT secrets, Discord token, Bot credentials, and WhatsApp config)
module keyVault 'modules/key-vault.bicep' = {
  name: 'deploy-key-vault'
  params: {
    keyVaultName: names.keyVault
    location: location
    adminObjectId: adminObjectId
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    discordBotToken: discordBotToken
    botMicrosoftAppId: botMicrosoftAppId
    botMicrosoftAppPassword: botMicrosoftAppPassword
    whatsAppChannelRegistrationId: whatsAppChannelRegistrationId
    whatsAppBusinessAccountId: whatsAppBusinessAccountId
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    whatsAppWebhookVerifyToken: whatsAppWebhookVerifyToken
  }
}

// Communication Services (conditional)
module communicationServices 'modules/communication-services.bicep' = if (!skipCommServiceCreation) {
  name: 'deploy-communication-services'
  params: {
    communicationServiceName: names.communicationService
    emailServiceName: names.emailService
    domainName: emailDomainName
    location: 'global'
    dataLocation: 'Europe'
    enableWhatsApp: enableWhatsApp
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    tags: tags
  }
}

// Azure Bot (conditional)
module azureBot 'modules/azure-bot.bicep' = if (deployAzureBot && botMicrosoftAppId != '') {
  name: 'deploy-azure-bot'
  params: {
    botName: names.azureBot
    botDisplayName: 'Mystira Bot'
    microsoftAppId: botMicrosoftAppId
    botEndpoint: botEndpoint != '' ? botEndpoint : 'https://${names.apiApp}.azurewebsites.net/api/messages/teams'
    location: 'global'
    sku: botSku
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    enableTeamsChannel: true
    enableWebChatChannel: true
    tags: tags
  }
}

// Storage Account (conditional)
module storage 'modules/storage.bicep' = if (!skipStorageCreation) {
  name: 'deploy-storage'
  params: {
    storageAccountName: storageAccountName
    location: location
    sku: storageSku
  }
}

// Cosmos DB (conditional)
module cosmosDb 'modules/cosmos-db.bicep' = if (!skipCosmosCreation) {
  name: 'deploy-cosmos-db'
  params: {
    cosmosDbAccountName: names.cosmosDb
    location: location
    databaseName: 'MystiraAppDb'
    serverless: cosmosServerless
  }
}

// Main API App Service (conditional)
module apiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'deploy-api-app-service'
  dependsOn: [keyVault]  // Ensure Key Vault is created first
  params: {
    appServiceName: names.apiApp
    appServicePlanName: names.appServicePlan
    location: location
    sku: appServiceSku
    aspnetEnvironment: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    keyVaultUri: keyVaultUriComputed
    acsConnectionString: skipCommServiceCreation ? acsConnectionString : communicationServices.outputs.communicationServiceConnectionString
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    discordBotToken: discordBotToken
    botMicrosoftAppId: botMicrosoftAppId
    botMicrosoftAppPassword: botMicrosoftAppPassword
    whatsAppChannelRegistrationId: whatsAppChannelRegistrationId
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    whatsAppBusinessAccountId: whatsAppBusinessAccountId
    whatsAppWebhookVerifyToken: whatsAppWebhookVerifyToken
    tags: tags
  }
}

// Admin API App Service (conditional) - shares the same App Service Plan
module adminApiAppService 'modules/app-service.bicep' = if (!skipAppServiceCreation) {
  name: 'deploy-admin-api-app-service'
  dependsOn: [apiAppService, keyVault]  // Ensure plan and Key Vault are created first
  params: {
    appServiceName: names.adminApiApp
    appServicePlanName: names.appServicePlan
    location: location
    sku: appServiceSku
    aspnetEnvironment: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    keyVaultUri: keyVaultUriComputed
    acsConnectionString: skipCommServiceCreation ? acsConnectionString : communicationServices.outputs.communicationServiceConnectionString
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    discordBotToken: discordBotToken
    botMicrosoftAppId: botMicrosoftAppId
    botMicrosoftAppPassword: botMicrosoftAppPassword
    whatsAppChannelRegistrationId: whatsAppChannelRegistrationId
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    whatsAppBusinessAccountId: whatsAppBusinessAccountId
    whatsAppWebhookVerifyToken: whatsAppWebhookVerifyToken
    tags: tags
  }
}

// Static Web App (conditional - deploys to fallback region since not available in South Africa North)
module staticWebApp 'modules/static-web-app.bicep' = if (deployStaticWebApp) {
  name: 'deploy-static-web-app'
  params: {
    staticWebAppName: names.staticWebApp
    location: fallbackRegion
    sku: staticWebAppSku
    repositoryUrl: staticWebAppRepositoryUrl
    branch: staticWebAppBranch
    tags: tags
    // Custom domain will be bound after DNS record is created
    customDomain: ''
    enableCustomDomain: false
  }
}

// DNS Records for Static Web App Custom Domain
// Creates CNAME/TXT record in existing DNS zone pointing to SWA
module swaDnsRecord 'modules/dns-zone.bicep' = if (deployStaticWebApp && enableCustomDomain) {
  name: 'deploy-swa-dns-record'
  scope: resourceGroup(dnsZoneRg)
  params: {
    dnsZoneName: dnsZoneName
    subdomain: customDomainSubdomain
    targetHostname: staticWebApp.outputs.staticWebAppDefaultHostname
    recordType: customDomainSubdomain == '' ? 'TXT' : 'CNAME'
    enableCustomDomain: true
    tags: tags
  }
}

// DNS Records for Main API Custom Domain
// Creates CNAME record: api.mystira.app or api.dev.mystira.app -> App Service hostname
module apiDnsRecord 'modules/dns-zone.bicep' = if (!skipAppServiceCreation && enableApiCustomDomain) {
  name: 'deploy-api-dns-record'
  scope: resourceGroup(dnsZoneRg)
  params: {
    dnsZoneName: dnsZoneName
    subdomain: customDomainSubdomain == '' ? apiSubdomainPrefix : '${apiSubdomainPrefix}.${customDomainSubdomain}'
    targetHostname: apiAppService.outputs.appServiceDefaultHostname
    recordType: 'CNAME'
    enableCustomDomain: true
    tags: tags
  }
}

// DNS Records for Admin API Custom Domain
// Creates CNAME record: admin.mystira.app or admin.dev.mystira.app -> App Service hostname
module adminApiDnsRecord 'modules/dns-zone.bicep' = if (!skipAppServiceCreation && enableApiCustomDomain) {
  name: 'deploy-admin-api-dns-record'
  scope: resourceGroup(dnsZoneRg)
  params: {
    dnsZoneName: dnsZoneName
    subdomain: customDomainSubdomain == '' ? adminApiSubdomainPrefix : '${adminApiSubdomainPrefix}.${customDomainSubdomain}'
    targetHostname: adminApiAppService.outputs.appServiceDefaultHostname
    recordType: 'CNAME'
    enableCustomDomain: true
    tags: tags
  }
}

// Static Web App Custom Domain Binding (after DNS record exists)
// This is a separate deployment to ensure DNS propagates first
module staticWebAppCustomDomain 'modules/static-web-app.bicep' = if (deployStaticWebApp && enableCustomDomain) {
  name: 'deploy-static-web-app-custom-domain'
  dependsOn: [swaDnsRecord]
  params: {
    staticWebAppName: names.staticWebApp
    location: fallbackRegion
    sku: staticWebAppSku
    repositoryUrl: staticWebAppRepositoryUrl
    branch: staticWebAppBranch
    tags: tags
    customDomain: customDomainFull
    enableCustomDomain: true
  }
}

// API Custom Domain Binding (after DNS record exists)
module apiCustomDomainBinding 'modules/app-service.bicep' = if (!skipAppServiceCreation && enableApiCustomDomain) {
  name: 'deploy-api-custom-domain-binding'
  dependsOn: [apiDnsRecord]
  params: {
    appServiceName: names.apiApp
    appServicePlanName: names.appServicePlan
    location: location
    sku: appServiceSku
    aspnetEnvironment: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    keyVaultUri: keyVaultUriComputed
    acsConnectionString: skipCommServiceCreation ? acsConnectionString : communicationServices.outputs.communicationServiceConnectionString
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    discordBotToken: discordBotToken
    botMicrosoftAppId: botMicrosoftAppId
    botMicrosoftAppPassword: botMicrosoftAppPassword
    whatsAppChannelRegistrationId: whatsAppChannelRegistrationId
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    whatsAppBusinessAccountId: whatsAppBusinessAccountId
    whatsAppWebhookVerifyToken: whatsAppWebhookVerifyToken
    tags: tags
    customDomain: apiCustomDomain
    enableCustomDomain: true
    enableManagedCert: enableManagedCert
  }
}

// Admin API Custom Domain Binding (after DNS record exists)
module adminApiCustomDomainBinding 'modules/app-service.bicep' = if (!skipAppServiceCreation && enableApiCustomDomain) {
  name: 'deploy-admin-api-custom-domain-binding'
  dependsOn: [adminApiDnsRecord]
  params: {
    appServiceName: names.adminApiApp
    appServicePlanName: names.appServicePlan
    location: location
    sku: appServiceSku
    aspnetEnvironment: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
    cosmosDbConnectionString: skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString
    storageConnectionString: skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString
    jwtRsaPrivateKey: jwtRsaPrivateKey
    jwtRsaPublicKey: jwtRsaPublicKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    keyVaultUri: keyVaultUriComputed
    acsConnectionString: skipCommServiceCreation ? acsConnectionString : communicationServices.outputs.communicationServiceConnectionString
    acsSenderEmail: acsSenderEmail
    corsAllowedOrigins: corsAllowedOrigins
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    discordBotToken: discordBotToken
    botMicrosoftAppId: botMicrosoftAppId
    botMicrosoftAppPassword: botMicrosoftAppPassword
    whatsAppChannelRegistrationId: whatsAppChannelRegistrationId
    whatsAppPhoneNumberId: whatsAppPhoneNumberId
    whatsAppBusinessAccountId: whatsAppBusinessAccountId
    whatsAppWebhookVerifyToken: whatsAppWebhookVerifyToken
    tags: tags
    customDomain: adminApiCustomDomain
    enableCustomDomain: true
    enableManagedCert: enableManagedCert
  }
}

// ─────────────────────────────────────────────────────────────────
// Key Vault Access Policies for App Services
// Added after App Services are deployed to avoid circular dependency
// ─────────────────────────────────────────────────────────────────

// Reference the Key Vault that was created by the module
resource keyVaultRef 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: names.keyVault
}

// Grant Main API App Service access to Key Vault secrets
resource apiAppKeyVaultAccess 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = if (!skipAppServiceCreation) {
  name: 'add'
  parent: keyVaultRef
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: apiAppService.outputs.appServicePrincipalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
      {
        tenantId: subscription().tenantId
        objectId: adminApiAppService.outputs.appServicePrincipalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// ─────────────────────────────────────────────────────────────────
// Outputs
// ─────────────────────────────────────────────────────────────────

// Naming info
output namingPattern string = '[org]-[env]-[project]-[type]-[region]'
output org string = org
output environment string = environment
output project string = project
output region string = region

// Monitoring
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId
output appInsightsInstrumentationKey string = appInsights.outputs.instrumentationKey
output appInsightsConnectionString string = appInsights.outputs.connectionString

// Communication Services
output communicationServiceConnectionString string = skipCommServiceCreation ? acsConnectionString : communicationServices.outputs.communicationServiceConnectionString
output emailServiceId string = skipCommServiceCreation ? '' : communicationServices.outputs.emailServiceId

// Storage
output storageAccountName string = skipStorageCreation ? existingStorageAccountName : storage.outputs.storageAccountName
output storageConnectionString string = skipStorageCreation ? storageConnString : storage.outputs.storageConnectionString

// Cosmos DB
output cosmosDbAccountName string = skipCosmosCreation ? existingCosmosDbAccountName : cosmosDb.outputs.cosmosDbAccountName
output cosmosDbConnectionString string = skipCosmosCreation ? cosmosConnString : cosmosDb.outputs.cosmosDbConnectionString

// App Services
output apiAppServiceUrl string = skipAppServiceCreation ? '' : apiAppService.outputs.appServiceUrl
output adminApiAppServiceUrl string = skipAppServiceCreation ? '' : adminApiAppService.outputs.appServiceUrl
output apiCustomDomainUrl string = !skipAppServiceCreation && enableApiCustomDomain ? 'https://${apiCustomDomain}' : ''
output adminApiCustomDomainUrl string = !skipAppServiceCreation && enableApiCustomDomain ? 'https://${adminApiCustomDomain}' : ''

// Azure Bot
output azureBotId string = deployAzureBot && botMicrosoftAppId != '' ? azureBot.outputs.botId : ''
output azureBotName string = deployAzureBot && botMicrosoftAppId != '' ? azureBot.outputs.botName : ''
output azureBotEndpoint string = deployAzureBot && botMicrosoftAppId != '' ? azureBot.outputs.botEndpoint : ''

// WhatsApp
output whatsAppEnabled bool = enableWhatsApp

// Discord
output discordBotConfigured bool = discordBotToken != ''

// Static Web App
output staticWebAppUrl string = deployStaticWebApp ? staticWebApp.outputs.staticWebAppUrl : ''
output staticWebAppName string = deployStaticWebApp ? staticWebApp.outputs.staticWebAppName : ''
output staticWebAppFallbackRegion string = fallbackRegion
output staticWebAppCustomDomain string = deployStaticWebApp && enableCustomDomain ? customDomainFull : ''
output staticWebAppCustomDomainUrl string = deployStaticWebApp && enableCustomDomain ? 'https://${customDomainFull}' : ''

// Key Vault
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri

// Region information
output primaryRegion string = location
output primaryRegionCode string = region
output fallbackRegionForSWA string = fallbackRegion

// Monitoring & Alerting
output actionGroupId string = enableAlerts && length(alertEmailReceivers) > 0 ? actionGroup.outputs.actionGroupId : ''
output alertsEnabled bool = enableAlerts
output availabilityTestsEnabled bool = enableAvailabilityTests
output dashboardId string = deployDashboard ? monitoringDashboard.outputs.dashboardId : ''
output budgetConfigured bool = deployBudget && length(alertEmailReceivers) > 0
