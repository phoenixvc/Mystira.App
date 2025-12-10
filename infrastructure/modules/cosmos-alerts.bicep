// Cosmos DB Alerts Module
// Defines metric-based alerts for Azure Cosmos DB performance monitoring

@description('Environment name for naming convention')
param environment string

@description('Project name for naming convention')
param project string

@description('Cosmos DB account resource ID')
param cosmosDbId string

@description('Action Group ID for alert notifications')
param actionGroupId string

@description('Tags for all resources')
param tags object = {}

@description('Enable Cosmos DB alerts')
param enableCosmosAlerts bool = true

// Naming convention helper
var alertPrefix = '${environment}-${project}'

// ═══════════════════════════════════════════════════════════════════════════════
// HIGH REQUEST UNIT CONSUMPTION ALERT
// Triggers when RU consumption exceeds threshold (indicating potential throttling)
// ═══════════════════════════════════════════════════════════════════════════════
resource highRuAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableCosmosAlerts) {
  name: '${alertPrefix}-cosmos-high-ru'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Cosmos DB normalized RU consumption exceeds 80%'
    severity: 2 // Warning
    enabled: true
    scopes: [cosmosDbId]
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT15M' // Look at 15 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighRuConsumption'
          metricName: 'NormalizedRUConsumption'
          metricNamespace: 'Microsoft.DocumentDB/databaseAccounts'
          operator: 'GreaterThan'
          threshold: 80
          timeAggregation: 'Maximum'
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

// ═══════════════════════════════════════════════════════════════════════════════
// THROTTLED REQUESTS ALERT
// Triggers when requests are being throttled (429 errors)
// ═══════════════════════════════════════════════════════════════════════════════
resource throttledRequestsAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableCosmosAlerts) {
  name: '${alertPrefix}-cosmos-throttled'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Cosmos DB requests are being throttled (429 errors)'
    severity: 1 // Critical
    enabled: true
    scopes: [cosmosDbId]
    evaluationFrequency: 'PT1M' // Check every 1 minute
    windowSize: 'PT5M' // Look at 5 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'ThrottledRequests'
          metricName: 'TotalRequests'
          metricNamespace: 'Microsoft.DocumentDB/databaseAccounts'
          operator: 'GreaterThan'
          threshold: 10 // Alert when more than 10 throttled requests in window (reduce noise)
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
          dimensions: [
            {
              name: 'StatusCode'
              operator: 'Include'
              values: ['429']
            }
          ]
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

// ═══════════════════════════════════════════════════════════════════════════════
// HIGH LATENCY ALERT
// Triggers when server-side latency exceeds threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource highLatencyAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableCosmosAlerts) {
  name: '${alertPrefix}-cosmos-high-latency'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Cosmos DB average server-side latency exceeds 100ms'
    severity: 2 // Warning
    enabled: true
    scopes: [cosmosDbId]
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT15M' // Look at 15 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighLatency'
          metricName: 'ServerSideLatency'
          metricNamespace: 'Microsoft.DocumentDB/databaseAccounts'
          operator: 'GreaterThan'
          threshold: 100 // 100ms
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

// ═══════════════════════════════════════════════════════════════════════════════
// FAILED REQUESTS ALERT
// Triggers when there are server errors (5xx status codes)
// ═══════════════════════════════════════════════════════════════════════════════
resource failedRequestsAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableCosmosAlerts) {
  name: '${alertPrefix}-cosmos-failed-requests'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Cosmos DB has server-side errors (5xx)'
    severity: 1 // Critical
    enabled: true
    scopes: [cosmosDbId]
    evaluationFrequency: 'PT1M' // Check every 1 minute
    windowSize: 'PT5M' // Look at 5 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'ServerErrors'
          metricName: 'TotalRequests'
          metricNamespace: 'Microsoft.DocumentDB/databaseAccounts'
          operator: 'GreaterThan'
          threshold: 0
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
          dimensions: [
            {
              name: 'StatusCode'
              operator: 'Include'
              values: ['500', '502', '503', '504']
            }
          ]
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

// ═══════════════════════════════════════════════════════════════════════════════
// DATA STORAGE ALERT
// Triggers when data storage exceeds threshold (for capacity planning)
// ═══════════════════════════════════════════════════════════════════════════════
resource highStorageAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableCosmosAlerts) {
  name: '${alertPrefix}-cosmos-high-storage'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Cosmos DB data storage exceeds 80% of provisioned capacity'
    severity: 2 // Warning
    enabled: true
    scopes: [cosmosDbId]
    evaluationFrequency: 'PT1H' // Check every hour
    windowSize: 'PT1H' // Look at 1 hour window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighStorage'
          metricName: 'DataUsage'
          metricNamespace: 'Microsoft.DocumentDB/databaseAccounts'
          operator: 'GreaterThan'
          threshold: 42949672960 // 40GB (80% of 50GB typical limit)
          timeAggregation: 'Maximum'
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
output highRuAlertId string = enableCosmosAlerts ? highRuAlert.id : ''
output throttledRequestsAlertId string = enableCosmosAlerts ? throttledRequestsAlert.id : ''
output highLatencyAlertId string = enableCosmosAlerts ? highLatencyAlert.id : ''
output failedRequestsAlertId string = enableCosmosAlerts ? failedRequestsAlert.id : ''
output highStorageAlertId string = enableCosmosAlerts ? highStorageAlert.id : ''
