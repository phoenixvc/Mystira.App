// DNS Zone Module for Custom Domain Configuration
// References an existing DNS zone and manages records for Static Web Apps

@description('Name of the existing DNS zone (e.g., mystira.app)')
param dnsZoneName string

@description('Resource group where the DNS zone exists')
param dnsZoneResourceGroup string = resourceGroup().name

@description('Subdomain for the SWA (e.g., "www", "dev", "staging", or "" for apex)')
param subdomain string = ''

@description('Static Web App default hostname to point to')
param swaDefaultHostname string

@description('Enable custom domain record creation')
param enableCustomDomain bool = true

@description('Tags for resources')
param tags object = {}

// Reference existing DNS zone (may be in different resource group)
resource dnsZone 'Microsoft.Network/dnsZones@2023-07-01-preview' existing = {
  name: dnsZoneName
  scope: resourceGroup(dnsZoneResourceGroup)
}

// Full custom domain name
var customDomainName = subdomain == '' ? dnsZoneName : '${subdomain}.${dnsZoneName}'

// CNAME record for subdomain (www, dev, staging, etc.)
resource cnameRecord 'Microsoft.Network/dnsZones/CNAME@2023-07-01-preview' = if (enableCustomDomain && subdomain != '') {
  name: subdomain
  parent: dnsZone
  properties: {
    TTL: 3600
    CNAMERecord: {
      cname: swaDefaultHostname
    }
    metadata: tags
  }
}

// For apex domain, we need an ALIAS record (Azure DNS specific)
// SWA requires a TXT record for validation first, then the alias
resource apexTxtRecord 'Microsoft.Network/dnsZones/TXT@2023-07-01-preview' = if (enableCustomDomain && subdomain == '') {
  name: '@'
  parent: dnsZone
  properties: {
    TTL: 3600
    TXTRecords: [
      {
        value: [swaDefaultHostname]
      }
    ]
    metadata: tags
  }
}

// Outputs
output dnsZoneId string = dnsZone.id
output dnsZoneName string = dnsZone.name
output customDomainName string = customDomainName
output recordType string = subdomain == '' ? 'TXT (apex)' : 'CNAME'
output targetHostname string = swaDefaultHostname
