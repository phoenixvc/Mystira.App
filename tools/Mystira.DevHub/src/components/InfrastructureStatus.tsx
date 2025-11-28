import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useRef, useState } from 'react';
import type { CommandResponse } from '../types';
import { formatTimeSince } from './services/utils/serviceUtils';

export interface ResourceInstance {
  name: string;
  health: 'healthy' | 'degraded' | 'unhealthy' | 'unknown';
  location?: string;
  status?: string;
}

export interface InfrastructureStatus {
  available: boolean;
  resources: {
    storage: { exists: boolean; name?: string; health?: 'healthy' | 'degraded' | 'unhealthy' | 'unknown'; instances?: ResourceInstance[] };
    cosmos: { exists: boolean; name?: string; health?: 'healthy' | 'degraded' | 'unhealthy' | 'unknown'; instances?: ResourceInstance[] };
    appService: { exists: boolean; name?: string; health?: 'healthy' | 'degraded' | 'unhealthy' | 'unknown'; instances?: ResourceInstance[] };
    keyVault: { exists: boolean; name?: string; health?: 'healthy' | 'degraded' | 'unhealthy' | 'unknown'; instances?: ResourceInstance[] };
  };
  lastChecked: number;
  resourceGroup: string;
}

interface InfrastructureStatusProps {
  environment: string;
  resourceGroup: string;
  onStatusChange?: (status: InfrastructureStatus) => void;
  onLoadingChange?: (loading: boolean) => void;
  refreshInterval?: number; // Configurable refresh interval in milliseconds (default: 30000)
}

function InfrastructureStatus({ environment, resourceGroup, onStatusChange, onLoadingChange, refreshInterval = 30000 }: InfrastructureStatusProps) {
  const [status, setStatus] = useState<InfrastructureStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const checkingRef = useRef(false);
  
  // Notify parent of loading state changes
  useEffect(() => {
    onLoadingChange?.(loading);
  }, [loading, onLoadingChange]);

  const checkInfrastructureStatus = async () => {
    // Prevent concurrent checks
    if (checkingRef.current) return;
    
    checkingRef.current = true;
    setLoading(true);
    setError(null);
    try {
      const response: CommandResponse<InfrastructureStatus> = await invoke('check_infrastructure_status', {
        environment,
        resourceGroup,
      });

      if (response.success && response.result) {
        setStatus(response.result);
        onStatusChange?.(response.result);
      } else {
        const errorMsg = response.error || 'Failed to check infrastructure status';
        setError(errorMsg);
        // Notify parent that infrastructure is unavailable
        const unavailableStatus: InfrastructureStatus = {
          available: false,
          resources: {
            storage: { exists: false, health: 'unknown' },
            cosmos: { exists: false, health: 'unknown' },
            appService: { exists: false, health: 'unknown' },
            keyVault: { exists: false, health: 'unknown' },
          },
          lastChecked: Date.now(),
          resourceGroup,
        };
        setStatus(unavailableStatus);
        onStatusChange?.(unavailableStatus);
      }
    } catch (err) {
      const errorMsg = `Error checking status: ${err}`;
      setError(errorMsg);
      // Notify parent that infrastructure is unavailable
      const unavailableStatus: InfrastructureStatus = {
        available: false,
        resources: {
          storage: { exists: false, health: 'unknown' },
          cosmos: { exists: false, health: 'unknown' },
          appService: { exists: false, health: 'unknown' },
          keyVault: { exists: false, health: 'unknown' },
        },
        lastChecked: Date.now(),
        resourceGroup,
      };
      setStatus(unavailableStatus);
      onStatusChange?.(unavailableStatus);
    } finally {
      setLoading(false);
      checkingRef.current = false;
    }
  };

  useEffect(() => {
    let mounted = true;
    let timeoutId: NodeJS.Timeout | null = null;
    
    const checkAndSetInterval = () => {
      if (mounted) {
        checkInfrastructureStatus();
      }
    };
    
    // Debounce: wait 500ms after prop changes before checking
    timeoutId = setTimeout(() => {
      if (mounted) {
        checkAndSetInterval();
      }
    }, 500);
    
    const interval = setInterval(checkAndSetInterval, refreshInterval);
    
    return () => {
      mounted = false;
      if (timeoutId) clearTimeout(timeoutId);
      clearInterval(interval);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [environment, resourceGroup]);

  const getHealthColor = (health?: string) => {
    switch (health) {
      case 'healthy': return 'text-green-600 dark:text-green-400';
      case 'degraded': return 'text-yellow-600 dark:text-yellow-400';
      case 'unhealthy': return 'text-red-600 dark:text-red-400';
      default: return 'text-gray-600 dark:text-gray-400';
    }
  };

  const getHealthIcon = (health?: string) => {
    switch (health) {
      case 'healthy': return '✅';
      case 'degraded': return '⚠️';
      case 'unhealthy': return '❌';
      default: return '❓';
    }
  };

  if (loading && !status) {
    return (
      <div className="p-4 bg-white dark:bg-gray-800 rounded-lg shadow-md border border-gray-200 dark:border-gray-700">
        <div className="flex items-center gap-2">
          <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></span>
          <span className="text-sm text-gray-600 dark:text-gray-400">Checking infrastructure status...</span>
        </div>
      </div>
    );
  }

  if (error && !status) {
    return (
      <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg shadow-md border border-red-200 dark:border-red-800">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-semibold text-red-800 dark:text-red-200 mb-1">Status Check Failed</h3>
            <p className="text-xs text-red-600 dark:text-red-400">{error}</p>
          </div>
          <button
            onClick={checkInfrastructureStatus}
            className="px-3 py-1 text-xs bg-red-600 hover:bg-red-700 text-white rounded-md"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!status) return null;

  return (
    <div className="space-y-4">
      {/* Infrastructure Availability */}
      <div className="p-4 bg-white dark:bg-gray-800 rounded-lg shadow-md border border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-3">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Infrastructure Status</h3>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              Last checked: {formatTimeSince(status.lastChecked)} • Resource Group: {status.resourceGroup}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <span className={`text-sm font-medium ${status.available ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
              {status.available ? '✅ Available' : '❌ Not Available'}
            </span>
            <button
              onClick={checkInfrastructureStatus}
              disabled={loading}
              className="px-2 py-1 text-xs bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-md disabled:opacity-50"
              title="Refresh status"
            >
              🔄
            </button>
          </div>
        </div>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {Object.entries(status.resources).map(([key, resource]) => {
            const instances = resource.instances || [];
            const hasMultiple = instances.length > 1;
            const isAppService = key === 'appService';
            
            return (
              <div key={key} className="p-3 bg-gray-50 dark:bg-gray-900 rounded-md border border-gray-200 dark:border-gray-700">
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-medium text-gray-700 dark:text-gray-300 capitalize">{key}</span>
                  {resource.exists ? (
                    <span className={getHealthColor(resource.health)}>{getHealthIcon(resource.health)}</span>
                  ) : (
                    <span className="flex items-center gap-1 text-gray-400">
                      <span className={getHealthColor(resource.health)}>{getHealthIcon(resource.health)}</span>
                      <span className="text-xs">Not deployed</span>
                    </span>
                  )}
                </div>
                {resource.exists && (
                  <div className="text-xs text-gray-600 dark:text-gray-400">
                    {hasMultiple ? (
                      <div className="space-y-1">
                        <div className="font-semibold">{instances.length} instance(s)</div>
                        {instances.slice(0, 2).map((inst: ResourceInstance, idx: number) => (
                          <div key={idx} className="flex items-center justify-between">
                            <span className="font-mono truncate flex-1" title={inst.name}>{inst.name}</span>
                            <span className={getHealthColor(inst.health)}>{getHealthIcon(inst.health)}</span>
                          </div>
                        ))}
                        {instances.length > 2 && (
                          <div className="text-gray-400">+{instances.length - 2} more</div>
                        )}
                      </div>
                    ) : (
                      <>
                        <div className="font-mono truncate" title={resource.name}>{resource.name}</div>
                        <div className="mt-1 flex items-center justify-between">
                          <span>{resource.health || 'unknown'}</span>
                          {isAppService && resource.name && (
                            <button
                              onClick={async () => {
                                try {
                                  const healthResponse = await invoke<CommandResponse<{ health: string; details: any }>>('check_resource_health_endpoint', {
                                    resourceType: 'Microsoft.Web/sites',
                                    resourceName: resource.name,
                                    resourceGroup: status.resourceGroup,
                                  });
                                  if (healthResponse.success && healthResponse.result) {
                                    const result = healthResponse.result;
                                    alert(`Health Check Result:\nStatus: ${result.health}\nDetails: ${JSON.stringify(result.details, null, 2)}`);
                                  } else {
                                    alert(`Health check failed: ${healthResponse.error || 'Unknown error'}`);
                                  }
                                } catch (err) {
                                  console.error('Failed to check health endpoint:', err);
                                  alert(`Failed to check health endpoint: ${err}`);
                                }
                              }}
                              className="ml-2 text-xs text-blue-600 dark:text-blue-400 hover:underline"
                              title="Check health endpoint"
                            >
                              🔍
                            </button>
                          )}
                        </div>
                      </>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

    </div>
  );
}

export default InfrastructureStatus;

