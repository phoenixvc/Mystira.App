// Availability Tests Module
// Defines synthetic monitoring tests for uptime monitoring

@description('Environment name for naming convention')
param environment string

@description('Project name for naming convention')
param project string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Application Insights resource ID')
param appInsightsId string

@description('Action Group ID for alert notifications')
param actionGroupId string

@description('Base URL for the API (e.g., https://api.mystira.app)')
param apiBaseUrl string

@description('Base URL for the Admin API (e.g., https://admin-api.mystira.app)')
param adminApiBaseUrl string = ''

@description('Base URL for the PWA (e.g., https://mystira.app)')
param pwaBaseUrl string = ''

@description('Tags for all resources')
param tags object = {}

@description('Enable availability tests')
param enableAvailabilityTests bool = true

@description('Test frequency in seconds')
@allowed([300, 600, 900]) // 5, 10, or 15 minutes
param testFrequencySeconds int = 300

// Naming convention helper
var testPrefix = '${environment}-${project}'

// Generate unique GUIDs for test elements
var apiHealthTestGuid = guid('${testPrefix}-api-health-test')
var apiHealthRequestGuid = guid('${testPrefix}-api-health-request')
var apiReadyTestGuid = guid('${testPrefix}-api-ready-test')
var apiReadyRequestGuid = guid('${testPrefix}-api-ready-request')
var adminApiHealthTestGuid = guid('${testPrefix}-admin-api-health-test')
var adminApiHealthRequestGuid = guid('${testPrefix}-admin-api-health-request')
var pwaHomeTestGuid = guid('${testPrefix}-pwa-home-test')
var pwaHomeRequestGuid = guid('${testPrefix}-pwa-home-request')

// Build WebTest XML strings with proper interpolation
// Note: Bicep raw strings ('''...''') do NOT interpolate variables, so we use regular string concatenation
var apiHealthWebTest = '<WebTest Name="${testPrefix}-api-health-test" Id="${apiHealthTestGuid}" Enabled="True" Timeout="30" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Items><Request Method="GET" Guid="${apiHealthRequestGuid}" Version="1.1" Url="${apiBaseUrl}/health" ThinkTime="0" Timeout="30" ParseDependentRequests="False" FollowRedirects="True" RecordResult="True" Cache="False" ResponseTimeGoal="0" Encoding="utf-8" ExpectedHttpStatusCode="200"/></Items></WebTest>'

var apiReadyWebTest = '<WebTest Name="${testPrefix}-api-ready-test" Id="${apiReadyTestGuid}" Enabled="True" Timeout="30" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Items><Request Method="GET" Guid="${apiReadyRequestGuid}" Version="1.1" Url="${apiBaseUrl}/health/ready" ThinkTime="0" Timeout="30" ParseDependentRequests="False" FollowRedirects="True" RecordResult="True" Cache="False" ResponseTimeGoal="0" Encoding="utf-8" ExpectedHttpStatusCode="200"/></Items></WebTest>'

var pwaHomeWebTest = '<WebTest Name="${testPrefix}-pwa-home-test" Id="${pwaHomeTestGuid}" Enabled="True" Timeout="60" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Items><Request Method="GET" Guid="${pwaHomeRequestGuid}" Version="1.1" Url="${pwaBaseUrl}/" ThinkTime="0" Timeout="60" ParseDependentRequests="False" FollowRedirects="True" RecordResult="True" Cache="False" ResponseTimeGoal="0" Encoding="utf-8" ExpectedHttpStatusCode="200"/></Items></WebTest>'

var adminApiHealthWebTest = '<WebTest Name="${testPrefix}-admin-api-health-test" Id="${adminApiHealthTestGuid}" Enabled="True" Timeout="30" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Items><Request Method="GET" Guid="${adminApiHealthRequestGuid}" Version="1.1" Url="${adminApiBaseUrl}/health" ThinkTime="0" Timeout="30" ParseDependentRequests="False" FollowRedirects="True" RecordResult="True" Cache="False" ResponseTimeGoal="0" Encoding="utf-8" ExpectedHttpStatusCode="200"/></Items></WebTest>'

// Test locations (use multiple geographic locations for reliability)
// See: https://docs.microsoft.com/en-us/azure/azure-monitor/app/monitor-web-app-availability#location
var testLocations = [
  {
    Id: 'emea-nl-ams-azr' // Amsterdam, Netherlands
  }
  {
    Id: 'emea-gb-db3-azr' // Dublin, Ireland
  }
  {
    Id: 'emea-fr-pra-edge' // Paris, France
  }
  {
    Id: 'us-va-ash-azr' // Virginia, USA
  }
  {
    Id: 'apac-sg-sin-azr' // Singapore
  }
]

// ═══════════════════════════════════════════════════════════════════════════════
// API HEALTH ENDPOINT TEST
// Primary availability test for API health endpoint
// ═══════════════════════════════════════════════════════════════════════════════
resource apiHealthTest 'Microsoft.Insights/webtests@2022-06-15' = if (enableAvailabilityTests && !empty(apiBaseUrl)) {
  name: '${testPrefix}-api-health-test'
  location: location
  tags: union(tags, {
    'hidden-link:${appInsightsId}': 'Resource'
  })
  kind: 'ping'
  properties: {
    SyntheticMonitorId: '${testPrefix}-api-health-test'
    Name: '${testPrefix} API Health Check'
    Description: 'Tests the /health endpoint for API availability'
    Enabled: true
    Frequency: testFrequencySeconds
    Timeout: 30
    Kind: 'ping'
    RetryEnabled: true
    Locations: testLocations
    Configuration: {
      WebTest: apiHealthWebTest
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// API READINESS ENDPOINT TEST
// Tests the readiness probe endpoint
// ═══════════════════════════════════════════════════════════════════════════════
resource apiReadyTest 'Microsoft.Insights/webtests@2022-06-15' = if (enableAvailabilityTests && !empty(apiBaseUrl)) {
  name: '${testPrefix}-api-ready-test'
  location: location
  tags: union(tags, {
    'hidden-link:${appInsightsId}': 'Resource'
  })
  kind: 'ping'
  properties: {
    SyntheticMonitorId: '${testPrefix}-api-ready-test'
    Name: '${testPrefix} API Readiness Check'
    Description: 'Tests the /health/ready endpoint for API readiness'
    Enabled: true
    Frequency: testFrequencySeconds
    Timeout: 30
    Kind: 'ping'
    RetryEnabled: true
    Locations: take(testLocations, 3) // Use fewer locations for readiness
    Configuration: {
      WebTest: apiReadyWebTest
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ADMIN API HEALTH ENDPOINT TEST
// Availability test for Admin API health endpoint
// ═══════════════════════════════════════════════════════════════════════════════
resource adminApiHealthTest 'Microsoft.Insights/webtests@2022-06-15' = if (enableAvailabilityTests && !empty(adminApiBaseUrl)) {
  name: '${testPrefix}-admin-api-health-test'
  location: location
  tags: union(tags, {
    'hidden-link:${appInsightsId}': 'Resource'
  })
  kind: 'ping'
  properties: {
    SyntheticMonitorId: '${testPrefix}-admin-api-health-test'
    Name: '${testPrefix} Admin API Health Check'
    Description: 'Tests the Admin API /health endpoint for availability'
    Enabled: true
    Frequency: testFrequencySeconds
    Timeout: 30
    Kind: 'ping'
    RetryEnabled: true
    Locations: take(testLocations, 3) // Use fewer locations for Admin API
    Configuration: {
      WebTest: adminApiHealthWebTest
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PWA HOME PAGE TEST (Optional)
// Tests the PWA is loading correctly
// ═══════════════════════════════════════════════════════════════════════════════
resource pwaHomeTest 'Microsoft.Insights/webtests@2022-06-15' = if (enableAvailabilityTests && !empty(pwaBaseUrl)) {
  name: '${testPrefix}-pwa-home-test'
  location: location
  tags: union(tags, {
    'hidden-link:${appInsightsId}': 'Resource'
  })
  kind: 'ping'
  properties: {
    SyntheticMonitorId: '${testPrefix}-pwa-home-test'
    Name: '${testPrefix} PWA Home Page Check'
    Description: 'Tests the PWA home page is accessible'
    Enabled: true
    Frequency: 600 // 10 minutes for PWA
    Timeout: 60
    Kind: 'ping'
    RetryEnabled: true
    Locations: testLocations
    Configuration: {
      WebTest: pwaHomeWebTest
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AVAILABILITY ALERT
// Alert when availability drops below threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAvailabilityTests && !empty(apiBaseUrl)) {
  name: '${testPrefix}-low-availability'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when API availability drops below 99%'
    severity: 1 // Critical
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT15M' // Look at 15 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'LowAvailability'
          metricName: 'availabilityResults/availabilityPercentage'
          metricNamespace: 'microsoft.insights/components'
          operator: 'LessThan'
          threshold: 99
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroupId
        webHookProperties: {}
      }
    ]
    autoMitigate: true
  }
}

// Outputs
output apiHealthTestId string = enableAvailabilityTests && !empty(apiBaseUrl) ? apiHealthTest.id : ''
output apiReadyTestId string = enableAvailabilityTests && !empty(apiBaseUrl) ? apiReadyTest.id : ''
output adminApiHealthTestId string = enableAvailabilityTests && !empty(adminApiBaseUrl) ? adminApiHealthTest.id : ''
output pwaHomeTestId string = enableAvailabilityTests && !empty(pwaBaseUrl) ? pwaHomeTest.id : ''
output availabilityAlertId string = enableAvailabilityTests && !empty(apiBaseUrl) ? availabilityAlert.id : ''
