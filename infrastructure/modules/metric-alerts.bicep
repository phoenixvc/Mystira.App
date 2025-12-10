// Metric Alerts Module
// Defines metric-based alerts for Application Insights and App Services

@description('Environment name for naming convention')
param environment string

@description('Project name for naming convention')
param project string

@description('Application Insights resource ID')
param appInsightsId string

@description('App Service Plan resource ID (for CPU/Memory alerts)')
param appServicePlanId string

@description('Action Group ID for alert notifications')
param actionGroupId string

@description('Tags for all resources')
param tags object = {}

@description('Enable alerts (set to false for dev environments to reduce noise)')
param enableAlerts bool = true

// Naming convention helper
var alertPrefix = '${environment}-${project}'

// ═══════════════════════════════════════════════════════════════════════════════
// HTTP 5XX ERROR RATE ALERT
// Triggers when server errors exceed threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource httpErrorAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAlerts) {
  name: '${alertPrefix}-high-error-rate'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when HTTP 5xx errors exceed 5 in 5 minutes'
    severity: 1 // Critical
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT1M' // Check every 1 minute
    windowSize: 'PT5M' // Look at 5 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighErrorRate'
          metricName: 'requests/failed'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Count'
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
// SLOW RESPONSE TIME ALERT (Average)
// Triggers when average response time exceeds threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource slowResponseAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAlerts) {
  name: '${alertPrefix}-slow-response'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when average response time exceeds 3 seconds'
    severity: 2 // Warning
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT15M' // Look at 15 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'SlowResponse'
          metricName: 'requests/duration'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 3000 // 3 seconds in milliseconds
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
// P95 RESPONSE TIME ALERT (Log-based query for accurate percentile)
// Triggers when 95th percentile response time exceeds threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource p95ResponseAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (enableAlerts) {
  name: '${alertPrefix}-p95-response'
  location: resourceGroup().location
  tags: tags
  properties: {
    displayName: '${alertPrefix} P95 Response Time'
    description: 'Alert when P95 response time exceeds 5 seconds'
    enabled: true
    severity: 2 // Warning
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT15M' // Look at 15 minute window
    scopes: [appInsightsId]
    criteria: {
      allOf: [
        {
          query: '''
            requests
            | where timestamp > ago(15m)
            | summarize P95 = percentile(duration, 95)
            | where P95 > 5000
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [actionGroupId]
    }
    autoMitigate: true
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// HIGH FAILED REQUEST RATE ALERT
// Triggers when failed request percentage exceeds threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource failedRequestRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAlerts) {
  name: '${alertPrefix}-high-failure-rate'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when failed request count exceeds 10 in 15 minutes'
    severity: 1 // Critical
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT15M' // Look at 15 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighFailureRate'
          metricName: 'requests/failed'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 10 // This is a count, not percentage
          timeAggregation: 'Count'
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
// HIGH CPU ALERT (App Service)
// Triggers when CPU usage exceeds threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource highCpuAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAlerts) {
  name: '${alertPrefix}-high-cpu'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when CPU usage exceeds 80% for 5 minutes'
    severity: 2 // Warning
    enabled: true
    scopes: [appServicePlanId]
    evaluationFrequency: 'PT1M' // Check every 1 minute
    windowSize: 'PT5M' // Look at 5 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighCpu'
          metricName: 'CpuPercentage'
          metricNamespace: 'Microsoft.Web/serverfarms'
          operator: 'GreaterThan'
          threshold: 80
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
// HIGH MEMORY ALERT (App Service)
// Triggers when memory usage exceeds threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource highMemoryAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAlerts) {
  name: '${alertPrefix}-high-memory'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when memory usage exceeds 80% for 5 minutes'
    severity: 2 // Warning
    enabled: true
    scopes: [appServicePlanId]
    evaluationFrequency: 'PT1M' // Check every 1 minute
    windowSize: 'PT5M' // Look at 5 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighMemory'
          metricName: 'MemoryPercentage'
          metricNamespace: 'Microsoft.Web/serverfarms'
          operator: 'GreaterThan'
          threshold: 80
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
// EXCEPTION SPIKE ALERT
// Triggers when exception count spikes
// ═══════════════════════════════════════════════════════════════════════════════
resource exceptionAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAlerts) {
  name: '${alertPrefix}-exception-spike'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when exception count exceeds 10 in 5 minutes'
    severity: 2 // Warning
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT1M' // Check every 1 minute
    windowSize: 'PT5M' // Look at 5 minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'ExceptionSpike'
          metricName: 'exceptions/count'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 10
          timeAggregation: 'Count'
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
output httpErrorAlertId string = enableAlerts ? httpErrorAlert.id : ''
output slowResponseAlertId string = enableAlerts ? slowResponseAlert.id : ''
output p95ResponseAlertId string = enableAlerts ? p95ResponseAlert.id : ''
output failedRequestRateAlertId string = enableAlerts ? failedRequestRateAlert.id : ''
output highCpuAlertId string = enableAlerts ? highCpuAlert.id : ''
output highMemoryAlertId string = enableAlerts ? highMemoryAlert.id : ''
output exceptionAlertId string = enableAlerts ? exceptionAlert.id : ''
