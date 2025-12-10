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

@description('App Service resource ID (optional, for infrastructure metrics)')
param appServiceId string = ''

@description('Cosmos DB account resource ID (optional, for database metrics)')
param cosmosDbId string = ''

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
          // Security Metrics Header
          {
            position: {
              x: 0
              y: 12
              rowSpan: 1
              colSpan: 12
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '## Security Metrics'
                    title: ''
                    subtitle: 'Authentication, rate limiting, and security event monitoring'
                  }
                }
              }
            }
          }
          // Custom Events Chart (Security Events)
          {
            position: {
              x: 0
              y: 13
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
                  value: 'customEvents/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Custom Events (Security)'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // 429 Response Rate (Rate Limiting)
          {
            position: {
              x: 6
              y: 13
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
                  name: 'Dimensions'
                  value: {
                    'request/resultCode': '429'
                  }
                }
                {
                  name: 'MetricName'
                  value: 'requests/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Rate Limited Requests (429)'
                  titleKind: 1
                  visualization: {
                    chartType: 4 // Bar chart
                  }
                }
              }
            }
          }
          // 401/403 Auth Failures
          {
            position: {
              x: 0
              y: 17
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
                  name: 'Dimensions'
                  value: {
                    'request/resultCode': ['401', '403']
                  }
                }
                {
                  name: 'MetricName'
                  value: 'requests/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Auth Failures (401/403)'
                  titleKind: 1
                  visualization: {
                    chartType: 4 // Bar chart
                  }
                }
              }
            }
          }
          // Server Errors 5xx
          {
            position: {
              x: 6
              y: 17
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
                  name: 'Dimensions'
                  value: {
                    'request/resultCode': ['500', '502', '503', '504']
                  }
                }
                {
                  name: 'MetricName'
                  value: 'requests/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Server Errors (5xx)'
                  titleKind: 1
                  visualization: {
                    chartType: 4 // Bar chart
                  }
                }
              }
            }
          }
          // Application Insights Resource Link
          {
            position: {
              x: 0
              y: 21
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
              y: 21
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
          // ═══════════════════════════════════════════════════════════════════════════════
          // INFRASTRUCTURE METRICS SECTION
          // App Service and hosting metrics
          // ═══════════════════════════════════════════════════════════════════════════════
          // Infrastructure Metrics Header
          {
            position: {
              x: 0
              y: 23
              rowSpan: 1
              colSpan: 12
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '## Infrastructure Metrics'
                    title: ''
                    subtitle: 'App Service CPU, memory, and connection metrics'
                  }
                }
              }
            }
          }
          // App Service CPU Percentage
          {
            position: {
              x: 0
              y: 24
              rowSpan: 4
              colSpan: 4
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
                  value: !empty(appServiceId) ? appServiceId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'CpuPercentage'
                }
                {
                  name: 'TimeRange'
                  value: 'PT4H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'CPU Usage %'
                  titleKind: 1
                  visualization: {
                    chartType: 3 // Area chart
                  }
                }
              }
            }
          }
          // App Service Memory Percentage
          {
            position: {
              x: 4
              y: 24
              rowSpan: 4
              colSpan: 4
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
                  value: !empty(appServiceId) ? appServiceId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'MemoryWorkingSet'
                }
                {
                  name: 'TimeRange'
                  value: 'PT4H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Memory Usage'
                  titleKind: 1
                  visualization: {
                    chartType: 3 // Area chart
                  }
                }
              }
            }
          }
          // App Service Connections
          {
            position: {
              x: 8
              y: 24
              rowSpan: 4
              colSpan: 4
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
                  value: !empty(appServiceId) ? appServiceId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'AppConnections'
                }
                {
                  name: 'TimeRange'
                  value: 'PT4H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Active Connections'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // ═══════════════════════════════════════════════════════════════════════════════
          // COSMOS DB METRICS SECTION
          // Database performance and throughput metrics
          // ═══════════════════════════════════════════════════════════════════════════════
          // Cosmos DB Header
          {
            position: {
              x: 0
              y: 28
              rowSpan: 1
              colSpan: 12
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '## Database Metrics (Cosmos DB)'
                    title: ''
                    subtitle: 'Request Units, throttling, and latency monitoring'
                  }
                }
              }
            }
          }
          // Cosmos DB Normalized RU Consumption
          {
            position: {
              x: 0
              y: 29
              rowSpan: 4
              colSpan: 4
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
                  value: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'NormalizedRUConsumption'
                }
                {
                  name: 'TimeRange'
                  value: 'PT4H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'RU Consumption %'
                  titleKind: 1
                  visualization: {
                    chartType: 3 // Area chart
                  }
                }
              }
            }
          }
          // Cosmos DB Total Request Units
          {
            position: {
              x: 4
              y: 29
              rowSpan: 4
              colSpan: 4
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
                  value: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'TotalRequestUnits'
                }
                {
                  name: 'TimeRange'
                  value: 'PT4H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Total Request Units'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Cosmos DB Throttled Requests (429s)
          {
            position: {
              x: 8
              y: 29
              rowSpan: 4
              colSpan: 4
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
                  value: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'TotalRequests'
                }
                {
                  name: 'Dimensions'
                  value: {
                    StatusCode: '429'
                  }
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Throttled Requests (429)'
                  titleKind: 1
                  visualization: {
                    chartType: 4 // Bar chart
                  }
                }
              }
            }
          }
          // Cosmos DB Server Side Latency
          {
            position: {
              x: 0
              y: 33
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
                  value: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'ServerSideLatency'
                }
                {
                  name: 'TimeRange'
                  value: 'PT4H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Server Side Latency (ms)'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Cosmos DB Document Count
          {
            position: {
              x: 6
              y: 33
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
                  value: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                }
                {
                  name: 'MetricName'
                  value: 'DocumentCount'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Document Count'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // ═══════════════════════════════════════════════════════════════════════════════
          // BUSINESS METRICS SECTION
          // Custom events for key user actions
          // ═══════════════════════════════════════════════════════════════════════════════
          // Business Metrics Header
          {
            position: {
              x: 0
              y: 37
              rowSpan: 1
              colSpan: 12
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '## Business Metrics'
                    title: ''
                    subtitle: 'User activity, content interactions, and key business events'
                  }
                }
              }
            }
          }
          // Page Views
          {
            position: {
              x: 0
              y: 38
              rowSpan: 4
              colSpan: 4
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
                  value: 'pageViews/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Page Views'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Active Users (Sessions)
          {
            position: {
              x: 4
              y: 38
              rowSpan: 4
              colSpan: 4
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
                  value: 'sessions/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Active Sessions'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Users Count
          {
            position: {
              x: 8
              y: 38
              rowSpan: 4
              colSpan: 4
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
                  value: 'users/count'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Unique Users'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Browser Timings
          {
            position: {
              x: 0
              y: 42
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
                  value: 'browserTimings/totalDuration'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Browser Load Time'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // Custom Metrics (Business Events)
          {
            position: {
              x: 6
              y: 42
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
                  value: 'customMetrics/Mystira.ContentPlays'
                }
                {
                  name: 'TimeRange'
                  value: 'PT24H'
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Content Plays'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                }
              }
            }
          }
          // ═══════════════════════════════════════════════════════════════════════════════
          // DEPLOYMENTS SECTION
          // Deployment timeline and release annotations
          // ═══════════════════════════════════════════════════════════════════════════════
          // Deployments Header
          {
            position: {
              x: 0
              y: 46
              rowSpan: 1
              colSpan: 12
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '## Deployments & Releases'
                    title: ''
                    subtitle: 'Deployment annotations and release tracking'
                  }
                }
              }
            }
          }
          // Deployment Annotations Timeline (Application Insights shows these automatically)
          {
            position: {
              x: 0
              y: 47
              rowSpan: 4
              colSpan: 12
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
                  value: 'P7D' // 7 days to see deployment patterns
                }
                {
                  name: 'showAnnotations'
                  value: true
                }
              ]
              type: 'Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart'
              settings: {
                chartSettings: {
                  title: 'Request Rate with Deployment Markers'
                  titleKind: 1
                  visualization: {
                    chartType: 2 // Line chart
                  }
                  showAnnotations: true
                }
              }
            }
          }
          // Deployment Info Markdown
          {
            position: {
              x: 0
              y: 51
              rowSpan: 2
              colSpan: 12
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '### Deployment Annotations\n\nDeployment markers are automatically added via CI/CD pipelines. Each deployment creates an annotation in Application Insights showing:\n- **Version** (Git commit SHA)\n- **Branch** deployed\n- **Deployer** (GitHub actor)\n- **Timestamp**\n\n📍 Hover over vertical markers on the chart above to see deployment details.'
                    title: ''
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
