// Azure Dashboard Module
// Creates a monitoring dashboard for Application Insights metrics

@description('Name of the dashboard')
param dashboardName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Application Insights resource ID')
param appInsightsId string

@description('Application Insights name')
param appInsightsName string

@description('Tags for all resources')
param tags object = {}

@description('Environment name')
param environment string

@description('Project name')
param project string

// Dashboard definition
resource dashboard 'Microsoft.Portal/dashboards@2020-09-01-preview' = {
  name: dashboardName
  location: location
  tags: union(tags, {
    'hidden-title': '${project} - ${environment} Monitoring Dashboard'
  })
  properties: {
    lenses: [
      {
        order: 0
        parts: [
          // Request Rate Chart
          {
            position: {
              x: 0
              y: 0
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                  value: appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'requests/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT1H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Request Rate'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Response Time Chart
          {
            position: {
              x: 6
              y: 0
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                  value: appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'requests/duration'
                }
                {
                  name: 'TimeRange'
                  value: 'PT1H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Response Time (Avg)'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Error Rate Chart
          {
            position: {
              x: 0
              y: 4
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                  value: appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'requests/failed'
                }
                {
                  name: 'TimeRange'
                  value: 'PT1H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Failed Requests'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Availability Chart
          {
            position: {
              x: 6
              y: 4
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                  value: appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'availabilityResults/availabilityPercentage'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Availability %'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Exceptions Chart
          {
            position: {
              x: 0
              y: 8
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                  value: appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'exceptions/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT1H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Exceptions'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Dependencies Chart
          {
            position: {
              x: 6
              y: 8
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                  value: appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'dependencies/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT1H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Dependency Calls'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Application Insights Resource Link
          {
            position: {
              x: 0
              y: 12
              rowSpan: 2
              colSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'id'
                  value: appInsightsId
                }
              ]
              type: 'Extension/HubsExtension/PartType/ResourcePart'
            }
          }
          // Markdown Header
          {
            position: {
              x: 4
              y: 12
              rowSpan: 2
              colSpan: 8
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '## ${project} - ${environment}\n\nMonitoring dashboard for Application Insights telemetry.\n\n**Resources:** [Application Insights](https://portal.azure.com/#resource${appInsightsId}/overview)'
                    title: 'Quick Links'
                    subtitle: ''
                  }
                }
              }
            }
          }
        ]
      }
    ]
    metadata: {
      model: {
        timeRange: {
          value: {
            relative: {
              duration: 24
              timeUnit: 1 // Hours
            }
          }
          type: 'MsPortalFx.Composition.Configuration.ValueTypes.TimeRange'
        }
        filterLocale: {
          value: 'en-us'
        }
        filters: {
          value: {
            MsPortalFx_TimeRange: {
              model: {
                format: 'utc'
                granularity: 'auto'
                relative: '24h'
              }
              displayCache: {
                name: 'UTC Time'
                value: 'Past 24 hours'
              }
            }
          }
        }
      }
    }
  }
}

output dashboardId string = dashboard.id
output dashboardName string = dashboard.name
