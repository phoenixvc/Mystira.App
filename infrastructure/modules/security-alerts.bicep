// Security Alerts Module
// Defines security-focused alerts based on custom metrics from SecurityMetrics service

@description('Environment name for naming convention')
param environment string

@description('Project name for naming convention')
param project string

@description('Application Insights resource ID')
param appInsightsId string

@description('Action Group ID for alert notifications')
param actionGroupId string

@description('Tags for all resources')
param tags object = {}

@description('Enable security alerts')
param enableSecurityAlerts bool = true

// Naming convention helper
var alertPrefix = '${environment}-${project}'

// ═══════════════════════════════════════════════════════════════════════════════
// BRUTE FORCE DETECTION ALERT
// Triggers when authentication failures from same source exceed threshold
// ═══════════════════════════════════════════════════════════════════════════════
resource bruteForceAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (enableSecurityAlerts) {
  name: '${alertPrefix}-brute-force-detected'
  location: resourceGroup().location
  tags: tags
  properties: {
    displayName: '${alertPrefix} Brute Force Detection'
    description: 'Alert when more than 10 authentication failures detected in 1 minute from same source'
    enabled: true
    severity: 1 // Critical
    evaluationFrequency: 'PT1M' // Check every 1 minute
    windowSize: 'PT5M' // Look at 5 minute window
    scopes: [appInsightsId]
    criteria: {
      allOf: [
        {
          query: '''
            customEvents
            | where name == "Security.AuthenticationFailed" or name == "Security.BruteForceDetected"
            | summarize FailedAttempts = count() by bin(timestamp, 1m), tostring(customDimensions.ClientIP)
            | where FailedAttempts > 10
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
// SUSTAINED RATE LIMITING ALERT
// Triggers when rate limit hits are sustained over time
// ═══════════════════════════════════════════════════════════════════════════════
resource rateLimitSustainedAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (enableSecurityAlerts) {
  name: '${alertPrefix}-rate-limit-sustained'
  location: resourceGroup().location
  tags: tags
  properties: {
    displayName: '${alertPrefix} Sustained Rate Limiting'
    description: 'Alert when rate limiting is sustained (>100 hits in 5 minutes)'
    enabled: true
    severity: 2 // Warning
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT5M' // Look at 5 minute window
    scopes: [appInsightsId]
    criteria: {
      allOf: [
        {
          query: '''
            customEvents
            | where name == "Security.RateLimitHit"
            | summarize HitCount = count() by bin(timestamp, 5m)
            | where HitCount > 100
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
// JWT VALIDATION FAILURE SPIKE ALERT
// Triggers when token validation failures spike
// ═══════════════════════════════════════════════════════════════════════════════
resource jwtValidationAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (enableSecurityAlerts) {
  name: '${alertPrefix}-jwt-validation-spike'
  location: resourceGroup().location
  tags: tags
  properties: {
    displayName: '${alertPrefix} JWT Validation Failure Spike'
    description: 'Alert when JWT validation failures exceed 20 in 5 minutes'
    enabled: true
    severity: 2 // Warning
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT5M' // Look at 5 minute window
    scopes: [appInsightsId]
    criteria: {
      allOf: [
        {
          query: '''
            customEvents
            | where name == "Security.TokenValidationFailed"
            | summarize FailureCount = count() by bin(timestamp, 5m)
            | where FailureCount > 20
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
// SUSPICIOUS ACTIVITY ALERT
// Triggers on any suspicious request detection
// ═══════════════════════════════════════════════════════════════════════════════
resource suspiciousActivityAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (enableSecurityAlerts) {
  name: '${alertPrefix}-suspicious-activity'
  location: resourceGroup().location
  tags: tags
  properties: {
    displayName: '${alertPrefix} Suspicious Activity Detected'
    description: 'Alert when suspicious request patterns are detected'
    enabled: true
    severity: 2 // Warning
    evaluationFrequency: 'PT5M' // Check every 5 minutes
    windowSize: 'PT15M' // Look at 15 minute window
    scopes: [appInsightsId]
    criteria: {
      allOf: [
        {
          query: '''
            customEvents
            | where name == "Security.SuspiciousRequest" or name == "Security.InvalidInput"
            | summarize Count = count() by bin(timestamp, 5m)
            | where Count > 5
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

// Outputs
output bruteForceAlertId string = enableSecurityAlerts ? bruteForceAlert.id : ''
output rateLimitSustainedAlertId string = enableSecurityAlerts ? rateLimitSustainedAlert.id : ''
output jwtValidationAlertId string = enableSecurityAlerts ? jwtValidationAlert.id : ''
output suspiciousActivityAlertId string = enableSecurityAlerts ? suspiciousActivityAlert.id : ''
