import { invoke } from '@tauri-apps/api/tauri';
import { useState, useEffect } from 'react';
import { ArrowRight, Cloud, Database, HardDrive, Loader2, RefreshCw, AlertTriangle, CheckCircle } from 'lucide-react';
import { ENVIRONMENT_PRESETS, EnvironmentPreset } from './types';

interface EnvironmentConnectionStrings {
  cosmos_connection: string | null;
  storage_connection: string | null;
  resource_group: string | null;
  storage_account: string | null;
  error: string | null;
}

interface AzureLoginStatus {
  logged_in: boolean;
  account_name: string | null;
  subscription_id: string | null;
  subscription_name: string | null;
}

interface EnvironmentSelectorProps {
  sourceEnvironment: string;
  destEnvironment: string;
  onSourceChange: (envId: string) => void;
  onDestChange: (envId: string) => void;
  onConnectionsFetched: (
    source: { cosmos: string; storage: string; databaseName: string },
    dest: { cosmos: string; storage: string; databaseName: string }
  ) => void;
}

interface DiscoveredInfo {
  resourceGroup?: string;
  storageAccount?: string;
}

export function EnvironmentSelector({
  sourceEnvironment,
  destEnvironment,
  onSourceChange,
  onDestChange,
  onConnectionsFetched,
}: EnvironmentSelectorProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [fetchedEnvs, setFetchedEnvs] = useState<Set<string>>(new Set());
  const [azureStatus, setAzureStatus] = useState<AzureLoginStatus | null>(null);
  const [checkingAzure, setCheckingAzure] = useState(true);
  const [discoveredInfo, setDiscoveredInfo] = useState<Record<string, DiscoveredInfo>>({});

  const sourcePreset = ENVIRONMENT_PRESETS.find((p) => p.id === sourceEnvironment);
  const destPreset = ENVIRONMENT_PRESETS.find((p) => p.id === destEnvironment);

  // Check Azure login status on mount
  useEffect(() => {
    const checkLogin = async () => {
      try {
        const status: AzureLoginStatus = await invoke('check_azure_login');
        setAzureStatus(status);
      } catch (error) {
        setAzureStatus({
          logged_in: false,
          account_name: null,
          subscription_id: null,
          subscription_name: null,
        });
      } finally {
        setCheckingAzure(false);
      }
    };
    checkLogin();
  }, []);

  const fetchConnections = async () => {
    if (sourceEnvironment === 'custom' || destEnvironment === 'custom') {
      return;
    }

    if (!sourcePreset || !destPreset) {
      setFetchError('Please select both source and destination environments');
      return;
    }

    if (!azureStatus?.logged_in) {
      setFetchError('Please log in to Azure CLI first. Run "az login" in your terminal.');
      return;
    }

    setIsLoading(true);
    setFetchError(null);

    try {
      // Fetch source connections (auto-discovers resource group and storage)
      const sourceConns: EnvironmentConnectionStrings = await invoke('fetch_environment_connections', {
        cosmosAccountName: sourcePreset.cosmosAccountName,
      });

      // Fetch destination connections
      const destConns: EnvironmentConnectionStrings = await invoke('fetch_environment_connections', {
        cosmosAccountName: destPreset.cosmosAccountName,
      });

      // Store discovered info for display
      setDiscoveredInfo({
        [sourceEnvironment]: {
          resourceGroup: sourceConns.resource_group || undefined,
          storageAccount: sourceConns.storage_account || undefined,
        },
        [destEnvironment]: {
          resourceGroup: destConns.resource_group || undefined,
          storageAccount: destConns.storage_account || undefined,
        },
      });

      if (sourceConns.error && destConns.error) {
        setFetchError(`Source: ${sourceConns.error}\nDest: ${destConns.error}`);
        return;
      }

      if (sourceConns.error) {
        setFetchError(`Source environment error: ${sourceConns.error}`);
        return;
      }

      if (destConns.error) {
        setFetchError(`Destination environment error: ${destConns.error}`);
        return;
      }

      onConnectionsFetched(
        {
          cosmos: sourceConns.cosmos_connection || '',
          storage: sourceConns.storage_connection || '',
          databaseName: sourcePreset.defaultDatabaseName || 'MystiraDb',
        },
        {
          cosmos: destConns.cosmos_connection || '',
          storage: destConns.storage_connection || '',
          databaseName: destPreset.defaultDatabaseName || 'MystiraAppDb',
        }
      );

      setFetchedEnvs(new Set([sourceEnvironment, destEnvironment]));
    } catch (error) {
      setFetchError(`Error fetching connections: ${String(error)}`);
    } finally {
      setIsLoading(false);
    }
  };

  const renderEnvironmentCard = (
    preset: EnvironmentPreset | undefined,
    type: 'source' | 'dest',
    value: string,
    onChange: (id: string) => void
  ) => {
    const isFetched = fetchedEnvs.has(value);
    const discovered = discoveredInfo[value];

    return (
      <div className="flex-1">
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          {type === 'source' ? 'Source Environment' : 'Destination Environment'}
        </label>
        <select
          value={value}
          onChange={(e) => {
            onChange(e.target.value);
            setFetchedEnvs((prev) => {
              const next = new Set(prev);
              next.delete(value);
              return next;
            });
          }}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        >
          <option value="">Select environment...</option>
          <optgroup label="Legacy Environments">
            {ENVIRONMENT_PRESETS.filter((p) => p.isLegacy).map((preset) => (
              <option key={preset.id} value={preset.id}>
                {preset.name}
              </option>
            ))}
          </optgroup>
          <optgroup label="Current Environments">
            {ENVIRONMENT_PRESETS.filter((p) => !p.isLegacy && p.id !== 'custom').map((preset) => (
              <option key={preset.id} value={preset.id}>
                {preset.name}
              </option>
            ))}
          </optgroup>
          <optgroup label="Manual Entry">
            <option value="custom">Custom (Enter manually)</option>
          </optgroup>
        </select>

        {preset && preset.id !== 'custom' && (
          <div className="mt-3 p-3 bg-gray-50 dark:bg-gray-900 rounded-lg text-sm">
            <p className="text-gray-600 dark:text-gray-400 mb-2">{preset.description}</p>
            <div className="space-y-1 text-xs">
              <div className="flex items-center gap-2">
                <Database className="w-3 h-3 text-purple-500" />
                <span className="text-gray-500 dark:text-gray-500">Cosmos:</span>
                <span className="text-gray-700 dark:text-gray-300 font-mono">{preset.cosmosAccountName}</span>
              </div>
              <div className="flex items-center gap-2">
                <Database className="w-3 h-3 text-blue-400" />
                <span className="text-gray-500 dark:text-gray-500">Database:</span>
                <span className="text-gray-700 dark:text-gray-300 font-mono">{preset.defaultDatabaseName || 'MystiraDb'}</span>
              </div>
              {discovered?.resourceGroup && (
                <div className="flex items-center gap-2">
                  <Cloud className="w-3 h-3 text-blue-500" />
                  <span className="text-gray-500 dark:text-gray-500">RG:</span>
                  <span className="text-gray-700 dark:text-gray-300 font-mono">{discovered.resourceGroup}</span>
                  <span className="text-green-500 text-xs">(discovered)</span>
                </div>
              )}
              {discovered?.storageAccount && (
                <div className="flex items-center gap-2">
                  <HardDrive className="w-3 h-3 text-green-500" />
                  <span className="text-gray-500 dark:text-gray-500">Storage:</span>
                  <span className="text-gray-700 dark:text-gray-300 font-mono">{discovered.storageAccount}</span>
                  <span className="text-green-500 text-xs">(discovered)</span>
                </div>
              )}
            </div>
            {isFetched && (
              <div className="mt-2 flex items-center gap-1 text-green-600 dark:text-green-400">
                <CheckCircle className="w-3 h-3" />
                <span>Connections fetched</span>
              </div>
            )}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="mb-6">
      <h4 className="text-lg font-medium text-gray-900 dark:text-white mb-4 flex items-center gap-2">
        <RefreshCw className="w-5 h-5" />
        Environment Migration
      </h4>

      {/* Azure Login Status */}
      {checkingAzure ? (
        <div className="mb-4 p-3 bg-gray-50 dark:bg-gray-900 rounded-lg flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
          <Loader2 className="w-4 h-4 animate-spin" />
          Checking Azure CLI status...
        </div>
      ) : azureStatus?.logged_in ? (
        <div className="mb-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg flex items-center gap-2 text-sm text-green-700 dark:text-green-400">
          <CheckCircle className="w-4 h-4" />
          <span>Logged in to Azure: <span className="font-medium">{azureStatus.subscription_name || 'Unknown subscription'}</span></span>
        </div>
      ) : (
        <div className="mb-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-center gap-2 text-sm text-amber-700 dark:text-amber-400">
          <AlertTriangle className="w-4 h-4" />
          <span>Not logged in to Azure CLI. Run <code className="px-1 py-0.5 bg-amber-100 dark:bg-amber-900/40 rounded">az login</code> first to auto-fetch connections.</span>
        </div>
      )}

      <div className="flex items-start gap-4">
        {renderEnvironmentCard(sourcePreset, 'source', sourceEnvironment, onSourceChange)}

        <div className="flex items-center justify-center pt-8">
          <ArrowRight className="w-6 h-6 text-gray-400" />
        </div>

        {renderEnvironmentCard(destPreset, 'dest', destEnvironment, onDestChange)}
      </div>

      {sourceEnvironment && destEnvironment && sourceEnvironment !== 'custom' && destEnvironment !== 'custom' && (
        <div className="mt-4">
          <button
            onClick={fetchConnections}
            disabled={isLoading || sourceEnvironment === destEnvironment || !azureStatus?.logged_in}
            className="flex items-center gap-2 px-4 py-2 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                Discovering resources from Azure...
              </>
            ) : (
              <>
                <Cloud className="w-4 h-4" />
                Auto-Discover & Fetch Connections
              </>
            )}
          </button>

          {sourceEnvironment === destEnvironment && (
            <p className="mt-2 text-sm text-amber-600 dark:text-amber-400">
              Source and destination cannot be the same environment
            </p>
          )}
        </div>
      )}

      {fetchError && (
        <div className="mt-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm whitespace-pre-wrap">
          {fetchError}
        </div>
      )}
    </div>
  );
}
