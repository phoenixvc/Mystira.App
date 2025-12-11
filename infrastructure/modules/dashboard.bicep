// Azure Dashboard Module
// Creates a monitoring dashboard for Application Insights metrics

@description('Name of the dashboard')
param dashboardName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Application Insights resource ID')
param appInsightsId string

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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'requests/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Server requests'
                          }
                        }
                      ]
                      title: 'Request Rate'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 3600000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'requests/duration'
                          aggregationType: 4
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Server response time'
                          }
                        }
                      ]
                      title: 'Response Time (Avg)'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 3600000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'requests/failed'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Failed requests'
                          }
                        }
                      ]
                      title: 'Failed Requests'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 3600000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'availabilityResults/availabilityPercentage'
                          aggregationType: 4
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Availability'
                          }
                        }
                      ]
                      title: 'Availability %'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'exceptions/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Exceptions'
                          }
                        }
                      ]
                      title: 'Exceptions'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 3600000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'dependencies/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Dependency calls'
                          }
                        }
                      ]
                      title: 'Dependency Calls'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 3600000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customEvents/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Custom events'
                          }
                        }
                      ]
                      title: 'Custom Events (Security)'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'requests/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Rate Limited (429)'
                            resourceDisplayName: appInsightsId
                          }
                        }
                      ]
                      title: 'Rate Limited Requests (429)'
                      titleKind: 1
                      visualization: {
                        chartType: 4
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      filterCollection: {
                        filters: [
                          {
                            key: 'request/resultCode'
                            operator: 0
                            values: [
                              '429'
                            ]
                          }
                        ]
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'requests/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Auth Failures (401/403)'
                          }
                        }
                      ]
                      title: 'Auth Failures (401/403)'
                      titleKind: 1
                      visualization: {
                        chartType: 4
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      filterCollection: {
                        filters: [
                          {
                            key: 'request/resultCode'
                            operator: 0
                            values: [
                              '401'
                              '403'
                            ]
                          }
                        ]
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'requests/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Server Errors (5xx)'
                          }
                        }
                      ]
                      title: 'Server Errors (5xx)'
                      titleKind: 1
                      visualization: {
                        chartType: 4
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      filterCollection: {
                        filters: [
                          {
                            key: 'request/resultCode'
                            operator: 0
                            values: [
                              '500'
                              '502'
                              '503'
                              '504'
                            ]
                          }
                        ]
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(appServiceId) ? appServiceId : appInsightsId
                          }
                          name: !empty(appServiceId) ? 'CpuPercentage' : 'performanceCounters/processCpuPercentage'
                          aggregationType: 4
                          namespace: !empty(appServiceId) ? 'Microsoft.Web/sites' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'CPU Usage %'
                          }
                        }
                      ]
                      title: 'CPU Usage %'
                      titleKind: 1
                      visualization: {
                        chartType: 3
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 14400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(appServiceId) ? appServiceId : appInsightsId
                          }
                          name: !empty(appServiceId) ? 'MemoryWorkingSet' : 'performanceCounters/processPrivateBytes'
                          aggregationType: 4
                          namespace: !empty(appServiceId) ? 'Microsoft.Web/sites' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Memory Usage'
                          }
                        }
                      ]
                      title: 'Memory Usage'
                      titleKind: 1
                      visualization: {
                        chartType: 3
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 14400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(appServiceId) ? appServiceId : appInsightsId
                          }
                          name: !empty(appServiceId) ? 'AppConnections' : 'performanceCounters/requestsInQueue'
                          aggregationType: 4
                          namespace: !empty(appServiceId) ? 'Microsoft.Web/sites' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Active Connections'
                          }
                        }
                      ]
                      title: 'Active Connections'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 14400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                          }
                          name: !empty(cosmosDbId) ? 'NormalizedRUConsumption' : 'dependencies/duration'
                          aggregationType: 3
                          namespace: !empty(cosmosDbId) ? 'Microsoft.DocumentDB/databaseAccounts' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'RU Consumption %'
                          }
                        }
                      ]
                      title: 'RU Consumption %'
                      titleKind: 1
                      visualization: {
                        chartType: 3
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 14400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                          }
                          name: !empty(cosmosDbId) ? 'TotalRequestUnits' : 'dependencies/count'
                          aggregationType: 1
                          namespace: !empty(cosmosDbId) ? 'Microsoft.DocumentDB/databaseAccounts' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Total Request Units'
                          }
                        }
                      ]
                      title: 'Total Request Units'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 14400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                          }
                          name: !empty(cosmosDbId) ? 'TotalRequests' : 'dependencies/failed'
                          aggregationType: 7
                          namespace: !empty(cosmosDbId) ? 'Microsoft.DocumentDB/databaseAccounts' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Throttled Requests'
                          }
                        }
                      ]
                      title: 'Throttled Requests (429)'
                      titleKind: 1
                      visualization: {
                        chartType: 4
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      filterCollection: !empty(cosmosDbId) ? {
                        filters: [
                          {
                            key: 'StatusCode'
                            operator: 0
                            values: [
                              '429'
                            ]
                          }
                        ]
                      } : {}
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                          }
                          name: !empty(cosmosDbId) ? 'ServerSideLatency' : 'dependencies/duration'
                          aggregationType: 4
                          namespace: !empty(cosmosDbId) ? 'Microsoft.DocumentDB/databaseAccounts' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Server Side Latency (ms)'
                          }
                        }
                      ]
                      title: 'Server Side Latency (ms)'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 14400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: !empty(cosmosDbId) ? cosmosDbId : appInsightsId
                          }
                          name: !empty(cosmosDbId) ? 'DocumentCount' : 'traces/count'
                          aggregationType: 1
                          namespace: !empty(cosmosDbId) ? 'Microsoft.DocumentDB/databaseAccounts' : 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Document Count'
                          }
                        }
                      ]
                      title: 'Document Count'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'pageViews/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Page views'
                          }
                        }
                      ]
                      title: 'Page Views'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'sessions/count'
                          aggregationType: 5
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Sessions'
                          }
                        }
                      ]
                      title: 'Active Sessions'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'users/count'
                          aggregationType: 5
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Users'
                          }
                        }
                      ]
                      title: 'Unique Users'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'browserTimings/totalDuration'
                          aggregationType: 4
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Browser page load time'
                          }
                        }
                      ]
                      title: 'Browser Load Time'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // Custom Metrics (Business Events) - Content Plays
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
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customMetrics/Mystira.ContentPlays'
                          aggregationType: 1
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Content Plays'
                          }
                        }
                      ]
                      title: 'Content Plays'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 86400000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // ═══════════════════════════════════════════════════════════════════════════════
          // USER JOURNEY ANALYTICS SECTION
          // Track user flows, engagement, and drop-off points
          // ═══════════════════════════════════════════════════════════════════════════════
          // User Journey Header
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
                    content: '## User Journey Analytics'
                    title: ''
                    subtitle: 'Session tracking, scenario engagement, and user flow analytics'
                  }
                }
              }
            }
          }
          // Daily Active Users (Sessions Started)
          {
            position: {
              x: 0
              y: 47
              rowSpan: 4
              colSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customMetrics/Journey.DailyActiveUsers'
                          aggregationType: 1
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Daily Active Users'
                          }
                        }
                      ]
                      title: 'Daily Active Users'
                      titleKind: 1
                      visualization: {
                        chartType: 4
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 604800000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // New User Registrations
          {
            position: {
              x: 4
              y: 47
              rowSpan: 4
              colSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customMetrics/Journey.NewUsers'
                          aggregationType: 1
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'New Users'
                          }
                        }
                      ]
                      title: 'New User Registrations'
                      titleKind: 1
                      visualization: {
                        chartType: 4
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 604800000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // Scenario Plays
          {
            position: {
              x: 8
              y: 47
              rowSpan: 4
              colSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customMetrics/Journey.ScenarioPlays'
                          aggregationType: 1
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Scenario Plays'
                          }
                        }
                      ]
                      title: 'Scenario Plays'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 604800000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // Scenario Completions
          {
            position: {
              x: 0
              y: 51
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customMetrics/Journey.ScenarioCompletions'
                          aggregationType: 1
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Scenario Completions'
                          }
                        }
                      ]
                      title: 'Scenario Completions'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 604800000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // Scenario Abandons
          {
            position: {
              x: 6
              y: 51
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customMetrics/Journey.ScenarioAbandons'
                          aggregationType: 1
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Scenario Abandons'
                          }
                        }
                      ]
                      title: 'Scenario Abandons'
                      titleKind: 1
                      visualization: {
                        chartType: 4
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 604800000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // Average Scenario Duration
          {
            position: {
              x: 0
              y: 55
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'customMetrics/Journey.ScenarioDuration'
                          aggregationType: 4
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Avg Scenario Duration'
                          }
                        }
                      ]
                      title: 'Avg Scenario Duration (sec)'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 604800000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // User Journey Info
          {
            position: {
              x: 6
              y: 55
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '### Journey Metrics Guide\n\n- **Daily Active Users**: Unique users who started a session\n- **New Users**: First-time registrations\n- **Scenario Plays**: Total scenario starts\n- **Completions**: Successfully finished scenarios\n- **Abandons**: Scenarios left incomplete (high rates may indicate UX issues)\n- **Duration**: Average time to complete scenarios\n\nUse these metrics to identify drop-off points and optimize user experience.'
                    title: ''
                    subtitle: ''
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
              y: 59
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
          // Deployment Annotations Timeline
          {
            position: {
              x: 0
              y: 60
              rowSpan: 4
              colSpan: 12
            }
            metadata: {
              inputs: [
                {
                  name: 'sharedTimeRange'
                  isOptional: true
                }
                {
                  name: 'options'
                  value: {
                    chart: {
                      metrics: [
                        {
                          resourceMetadata: {
                            id: appInsightsId
                          }
                          name: 'requests/count'
                          aggregationType: 7
                          namespace: 'microsoft.insights/components'
                          metricVisualization: {
                            displayName: 'Server requests'
                          }
                        }
                      ]
                      title: 'Request Rate with Deployment Markers'
                      titleKind: 1
                      visualization: {
                        chartType: 2
                        legendVisualization: {
                          isVisible: true
                          position: 2
                          hideSubtitle: false
                        }
                        axisVisualization: {
                          x: {
                            isVisible: true
                            axisType: 2
                          }
                          y: {
                            isVisible: true
                            axisType: 1
                          }
                        }
                      }
                      timespan: {
                        relative: {
                          duration: 604800000
                        }
                      }
                    }
                  }
                  isOptional: true
                }
              ]
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
            }
          }
          // Deployment Info Markdown
          {
            position: {
              x: 0
              y: 64
              rowSpan: 2
              colSpan: 12
            }
            metadata: {
              inputs: []
              type: 'Extension/HubsExtension/PartType/MarkdownPart'
              settings: {
                content: {
                  settings: {
                    content: '### Deployment Annotations\n\nDeployment markers are automatically added via CI/CD pipelines. Each deployment creates an annotation in Application Insights showing:\n- **Version** (Git commit SHA)\n- **Branch** deployed\n- **Deployer** (GitHub actor)\n- **Timestamp**\n\nHover over vertical markers on the chart above to see deployment details.'
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
              timeUnit: 1
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
