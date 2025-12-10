// Custom Domains Module
// Configures custom domain bindings and managed SSL certificates for App Services and Static Web Apps
//
// IMPORTANT: Before deploying custom domains, you must configure DNS:
// 1. For App Services: Add CNAME record pointing to <app-name>.azurewebsites.net
// 2. For Static Web Apps: Add CNAME record pointing to the SWA default hostname
// 3. For apex domains (e.g., mystira.app): Use A record with Azure Front Door IP or alias record
//
// Domain Structure:
// Production:  mystira.app, api.mystira.app, adminapi.mystira.app
// Staging:     staging.mystira.app, api.staging.mystira.app, adminapi.staging.mystira.app
// Development: dev.mystira.app, api.dev.mystira.app, adminapi.dev.mystira.app

@description('Environment name')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string

@description('Base domain name (e.g., mystira.app)')
param baseDomain string = 'mystira.app'

@description('Name of the API App Service')
param apiAppServiceName string

@description('Name of the Admin API App Service')
param adminApiAppServiceName string

@description('Name of the Static Web App')
param staticWebAppName string

@description('Enable custom domain configuration (set to false to skip domain setup)')
param enableCustomDomains bool = false

@description('Skip SSL certificate creation (useful for initial DNS validation)')
param skipSslCertificates bool = true

@description('Tags for resources')
param tags object = {}

// Compute domain names based on environment
var domainPrefix = environment == 'prod' ? '' : '${environment}.'
var domains = {
  pwa: environment == 'prod' ? baseDomain : '${environment}.${baseDomain}'
  pwwWww: environment == 'prod' ? 'www.${baseDomain}' : ''
  api: environment == 'prod' ? 'api.${baseDomain}' : 'api.${environment}.${baseDomain}'
  adminApi: environment == 'prod' ? 'adminapi.${baseDomain}' : 'adminapi.${environment}.${baseDomain}'
}

// Reference existing App Services
resource apiAppService 'Microsoft.Web/sites@2023-01-01' existing = if (enableCustomDomains) {
  name: apiAppServiceName
}

resource adminApiAppService 'Microsoft.Web/sites@2023-01-01' existing = if (enableCustomDomains) {
  name: adminApiAppServiceName
}

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' existing = if (enableCustomDomains) {
  name: staticWebAppName
}

// ─────────────────────────────────────────────────────────────────
// App Service Custom Domain Bindings
// ─────────────────────────────────────────────────────────────────

// API App Service - Custom Domain Binding
resource apiCustomDomain 'Microsoft.Web/sites/hostNameBindings@2023-01-01' = if (enableCustomDomains) {
  parent: apiAppService
  name: domains.api
  properties: {
    siteName: apiAppServiceName
    hostNameType: 'Verified'
    sslState: skipSslCertificates ? 'Disabled' : 'SniEnabled'
  }
}

// Admin API App Service - Custom Domain Binding
resource adminApiCustomDomain 'Microsoft.Web/sites/hostNameBindings@2023-01-01' = if (enableCustomDomains) {
  parent: adminApiAppService
  name: domains.adminApi
  properties: {
    siteName: adminApiAppServiceName
    hostNameType: 'Verified'
    sslState: skipSslCertificates ? 'Disabled' : 'SniEnabled'
  }
}

// ─────────────────────────────────────────────────────────────────
// App Service Managed SSL Certificates
// Note: Requires domain to be verified first (hostname binding must exist)
// ─────────────────────────────────────────────────────────────────

// API App Service - Managed Certificate
resource apiManagedCertificate 'Microsoft.Web/certificates@2023-01-01' = if (enableCustomDomains && !skipSslCertificates) {
  name: 'cert-${replace(domains.api, '.', '-')}'
  location: apiAppService.location
  tags: tags
  properties: {
    serverFarmId: apiAppService.properties.serverFarmId
    canonicalName: domains.api
  }
  dependsOn: [apiCustomDomain]
}

// Admin API App Service - Managed Certificate
resource adminApiManagedCertificate 'Microsoft.Web/certificates@2023-01-01' = if (enableCustomDomains && !skipSslCertificates) {
  name: 'cert-${replace(domains.adminApi, '.', '-')}'
  location: adminApiAppService.location
  tags: tags
  properties: {
    serverFarmId: adminApiAppService.properties.serverFarmId
    canonicalName: domains.adminApi
  }
  dependsOn: [adminApiCustomDomain]
}

// ─────────────────────────────────────────────────────────────────
// Static Web App Custom Domains
// Note: Azure Static Web Apps automatically provision SSL certificates
// ─────────────────────────────────────────────────────────────────

// Static Web App - Primary Custom Domain (e.g., mystira.app or dev.mystira.app)
resource swaCustomDomain 'Microsoft.Web/staticSites/customDomains@2023-01-01' = if (enableCustomDomains) {
  parent: staticWebApp
  name: domains.pwa
  properties: {
    validationMethod: 'cname-delegation'
  }
}

// Static Web App - WWW Custom Domain (production only)
resource swaWwwCustomDomain 'Microsoft.Web/staticSites/customDomains@2023-01-01' = if (enableCustomDomains && environment == 'prod') {
  parent: staticWebApp
  name: domains.pwwWww
  properties: {
    validationMethod: 'cname-delegation'
  }
}

// ─────────────────────────────────────────────────────────────────
// Outputs
// ─────────────────────────────────────────────────────────────────

output customDomainsEnabled bool = enableCustomDomains
output pwaCustomDomain string = domains.pwa
output pwaWwwCustomDomain string = domains.pwwWww
output apiCustomDomain string = domains.api
output adminApiCustomDomain string = domains.adminApi

// Full URLs for convenience
output pwaUrl string = 'https://${domains.pwa}'
output apiUrl string = 'https://${domains.api}'
output adminApiUrl string = 'https://${domains.adminApi}'

// DNS Configuration Instructions (output for documentation)
output dnsInstructions object = {
  message: 'Configure the following DNS records before enabling custom domains'
  records: [
    {
      type: 'CNAME'
      name: environment == 'prod' ? '@' : environment
      value: '${staticWebAppName}.azurestaticapps.net'
      description: 'PWA (Static Web App)'
    }
    {
      type: 'CNAME'
      name: environment == 'prod' ? 'www' : ''
      value: environment == 'prod' ? '${staticWebAppName}.azurestaticapps.net' : 'N/A'
      description: 'WWW redirect (production only)'
    }
    {
      type: 'CNAME'
      name: environment == 'prod' ? 'api' : 'api.${environment}'
      value: '${apiAppServiceName}.azurewebsites.net'
      description: 'Main API'
    }
    {
      type: 'CNAME'
      name: environment == 'prod' ? 'adminapi' : 'adminapi.${environment}'
      value: '${adminApiAppServiceName}.azurewebsites.net'
      description: 'Admin API'
    }
  ]
}
