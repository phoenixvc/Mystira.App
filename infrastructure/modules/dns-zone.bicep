// DNS Zone Module for Custom Domain Configuration
// References an existing DNS zone and manages CNAME/TXT records for SWA and App Services

@description('Name of the existing DNS zone (e.g., mystira.app)')
param dnsZoneName string

@description('Subdomain to create (e.g., "api", "admin", "dev", "api.dev", or "" for apex)')
param subdomain string = ''

@description('Target hostname to point the CNAME to (e.g., SWA or App Service default hostname)')
param targetHostname string

@description('Enable custom domain record creation')
param enableCustomDomain bool = true

@description('Record type: "CNAME" for subdomains, "TXT" for apex domain validation')
@allowed([
  'CNAME'
  'TXT'
])
param recordType string = 'CNAME'

@description('Tags for resources')
param tags object = {}

// Reference existing DNS zone in current resource group
// Note: This module must be deployed to the DNS zone's resource group via scope: resourceGroup(dnsZoneRg)
resource dnsZone 'Microsoft.Network/dnsZones@2023-07-01-preview' existing = {
  name: dnsZoneName
}

// Full custom domain name
var customDomainName = subdomain == '' ? dnsZoneName : '${subdomain}.${dnsZoneName}'

// CNAME record for subdomain (api, admin, api.dev, etc.)
resource cnameRecord 'Microsoft.Network/dnsZones/CNAME@2023-07-01-preview' = if (enableCustomDomain && subdomain != '' && recordType == 'CNAME') {
  name: subdomain
  parent: dnsZone
  properties: {
    TTL: 3600
    CNAMERecord: {
      cname: targetHostname
    }
    metadata: tags
  }
}

// TXT record for apex domain validation (SWA requirement)
resource apexTxtRecord 'Microsoft.Network/dnsZones/TXT@2023-07-01-preview' = if (enableCustomDomain && subdomain == '' && recordType == 'TXT') {
  name: '@'
  parent: dnsZone
  properties: {
    TTL: 3600
    TXTRecords: [
      {
        value: [targetHostname]
      }
    ]
    metadata: tags
  }
}

// Outputs
output dnsZoneId string = dnsZone.id
output dnsZoneName string = dnsZone.name
output customDomainName string = customDomainName
output recordType string = recordType
output targetHostname string = targetHostname
