// Budget Alerts Module
// Creates Azure Cost Management budget with alert thresholds

@description('Name of the budget')
param budgetName string

@description('Monthly budget amount in USD')
param monthlyBudget int = 100

@description('Email addresses for budget alerts')
param alertEmailReceivers array = []

// Note: tags parameter removed as budgets don't support tags

@description('Enable budget alerts')
param enableBudget bool = true

@description('Current UTC time - used for budget start date calculation')
param currentTime string = utcNow()

@description('First threshold percentage (default 50%)')
param firstThresholdPercent int = 50

@description('Second threshold percentage (default 80%)')
param secondThresholdPercent int = 80

@description('Third threshold percentage (default 100%)')
param thirdThresholdPercent int = 100

// Calculate start and end dates for budget
// Budget runs from start of current month to 10 years from now
var startDate = '${substring(currentTime, 0, 7)}-01' // First day of current month
var endDate = '2034-12-31' // Far future date

resource budget 'Microsoft.Consumption/budgets@2023-05-01' = if (enableBudget && length(alertEmailReceivers) > 0) {
  name: budgetName
  properties: {
    category: 'Cost'
    amount: monthlyBudget
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: startDate
      endDate: endDate
    }
    filter: {
      // Budget applies to current resource group
      // Remove this to apply to entire subscription
    }
    notifications: {
      // First threshold alert (e.g., 50%)
      firstThreshold: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: firstThresholdPercent
        contactEmails: alertEmailReceivers
        thresholdType: 'Actual'
      }
      // Second threshold alert (e.g., 80%)
      secondThreshold: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: secondThresholdPercent
        contactEmails: alertEmailReceivers
        thresholdType: 'Actual'
      }
      // Third threshold alert (e.g., 100%)
      thirdThreshold: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: thirdThresholdPercent
        contactEmails: alertEmailReceivers
        thresholdType: 'Actual'
      }
      // Forecasted threshold alert (90% forecasted)
      forecastedThreshold: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 90
        contactEmails: alertEmailReceivers
        thresholdType: 'Forecasted'
      }
    }
  }
}

output budgetId string = enableBudget && length(alertEmailReceivers) > 0 ? budget.id : ''
output budgetName string = enableBudget && length(alertEmailReceivers) > 0 ? budget.name : ''
