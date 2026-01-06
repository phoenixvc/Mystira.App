import { useState, useEffect } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import {
  CheckCircle2,
  XCircle,
  Loader2,
  RefreshCw,
  Server,
  Database,
  Globe,
  HardDrive,
  Layers,
  ChevronDown,
  ChevronRight,
  ExternalLink,
} from 'lucide-react';
import type { CommandResponse } from '../../../../types';

export interface ResourceGroup {
  name: string;
  location: string;
  hasResources: boolean;
  resourceCount: number;
}

export interface StaticWebApp {
  name: string;
  resourceGroup: string;
  location: string;
  defaultHostname: string;
}

export interface DiscoveredResources {
  resourceGroups: ResourceGroup[];
  staticWebApps: StaticWebApp[];
  lastScanned: Date | null;
}

interface ResourceDiscoveryPanelProps {
  onResourcesDiscovered?: (resources: DiscoveredResources) => void;
  onResourceGroupSelected?: (resourceGroup: ResourceGroup | null) => void;
  onStaticWebAppSelected?: (swa: StaticWebApp | null) => void;
  selectedResourceGroup?: ResourceGroup | null;
}

export function ResourceDiscoveryPanel({
  onResourcesDiscovered,
  onResourceGroupSelected,
  onStaticWebAppSelected,
  selectedResourceGroup,
}: ResourceDiscoveryPanelProps) {
  const [resources, setResources] = useState<DiscoveredResources>({
    resourceGroups: [],
    staticWebApps: [],
    lastScanned: null,
  });
  const [isScanning, setIsScanning] = useState(false);
  const [scanError, setScanError] = useState<string | null>(null);
  const [expandedSections, setExpandedSections] = useState({
    resourceGroups: true,
    staticWebApps: true,
  });

  const scanResources = async () => {
    setIsScanning(true);
    setScanError(null);

    try {
      // Scan for existing resources
      const response = await invoke<CommandResponse>('scan_existing_resources');

      if (response.success && response.result) {
        const result = response.result as {
          resourceGroups: ResourceGroup[];
          staticWebApps: StaticWebApp[];
        };

        const discovered: DiscoveredResources = {
          resourceGroups: result.resourceGroups || [],
          staticWebApps: result.staticWebApps || [],
          lastScanned: new Date(),
        };

        setResources(discovered);
        onResourcesDiscovered?.(discovered);
      } else {
        setScanError(response.error || 'Failed to scan resources');
      }
    } catch (error) {
      setScanError(error instanceof Error ? error.message : 'Failed to scan resources');
    } finally {
      setIsScanning(false);
    }
  };

  useEffect(() => {
    scanResources();
  }, []);

  const toggleSection = (section: keyof typeof expandedSections) => {
    setExpandedSections(prev => ({
      ...prev,
      [section]: !prev[section],
    }));
  };

  const getLocationDisplay = (location: string) => {
    const locationNames: Record<string, string> = {
      southafricanorth: 'South Africa North',
      eastus2: 'East US 2',
      westus2: 'West US 2',
      centralus: 'Central US',
      westeurope: 'West Europe',
      northeurope: 'North Europe',
      eastasia: 'East Asia',
      southeastasia: 'Southeast Asia',
    };
    return locationNames[location] || location;
  };

  const hasResources = resources.resourceGroups.length > 0 || resources.staticWebApps.length > 0;

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 bg-gray-50 dark:bg-gray-700/50">
        <div className="flex items-center gap-3">
          <Server className="w-4 h-4 text-blue-500" />
          <span className="font-medium text-gray-900 dark:text-white text-sm">
            Existing Resources
          </span>
          {hasResources && (
            <span className="text-xs text-gray-500 dark:text-gray-400">
              ({resources.resourceGroups.length} RGs, {resources.staticWebApps.length} SWAs)
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {resources.lastScanned && (
            <span className="text-xs text-gray-500 dark:text-gray-400">
              Last scan: {resources.lastScanned.toLocaleTimeString()}
            </span>
          )}
          <button
            onClick={scanResources}
            disabled={isScanning}
            className="p-1 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 disabled:opacity-50"
            title="Rescan resources"
          >
            <RefreshCw className={`w-4 h-4 ${isScanning ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="p-4">
        {isScanning && !hasResources ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="w-6 h-6 text-blue-500 animate-spin mr-2" />
            <span className="text-gray-600 dark:text-gray-300">Scanning for existing resources...</span>
          </div>
        ) : scanError ? (
          <div className="flex items-center gap-2 py-4 text-red-600 dark:text-red-400">
            <XCircle className="w-5 h-5" />
            <span>{scanError}</span>
            <button
              onClick={scanResources}
              className="ml-2 text-sm text-blue-600 hover:underline"
            >
              Retry
            </button>
          </div>
        ) : !hasResources ? (
          <div className="py-8 text-center">
            <Layers className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
            <p className="text-gray-500 dark:text-gray-400 text-sm">
              No existing Mystira resources found
            </p>
            <p className="text-gray-400 dark:text-gray-500 text-xs mt-1">
              Deploy infrastructure to get started
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {/* Resource Groups */}
            {resources.resourceGroups.length > 0 && (
              <div>
                <button
                  onClick={() => toggleSection('resourceGroups')}
                  className="flex items-center gap-2 w-full text-left mb-2"
                >
                  {expandedSections.resourceGroups ? (
                    <ChevronDown className="w-4 h-4 text-gray-500" />
                  ) : (
                    <ChevronRight className="w-4 h-4 text-gray-500" />
                  )}
                  <Database className="w-4 h-4 text-purple-500" />
                  <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Resource Groups ({resources.resourceGroups.length})
                  </span>
                </button>
                {expandedSections.resourceGroups && (
                  <div className="ml-6 space-y-2">
                    {resources.resourceGroups.map((rg) => (
                      <button
                        key={rg.name}
                        onClick={() => onResourceGroupSelected?.(
                          selectedResourceGroup?.name === rg.name ? null : rg
                        )}
                        className={`w-full text-left p-3 rounded-lg border transition-colors ${
                          selectedResourceGroup?.name === rg.name
                            ? 'bg-blue-50 dark:bg-blue-900/30 border-blue-300 dark:border-blue-700'
                            : 'bg-gray-50 dark:bg-gray-700 border-gray-200 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-600'
                        }`}
                      >
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            {rg.hasResources ? (
                              <CheckCircle2 className="w-4 h-4 text-green-500" />
                            ) : (
                              <div className="w-4 h-4 rounded-full border-2 border-gray-300 dark:border-gray-500" />
                            )}
                            <span className="font-mono text-sm text-gray-900 dark:text-white">
                              {rg.name}
                            </span>
                          </div>
                          {selectedResourceGroup?.name === rg.name && (
                            <span className="text-xs text-blue-600 dark:text-blue-400 font-medium">
                              Selected
                            </span>
                          )}
                        </div>
                        <div className="flex items-center gap-4 mt-1 ml-6">
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            <Globe className="w-3 h-3 inline mr-1" />
                            {getLocationDisplay(rg.location)}
                          </span>
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            <HardDrive className="w-3 h-3 inline mr-1" />
                            {rg.resourceCount} resources
                          </span>
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Static Web Apps */}
            {resources.staticWebApps.length > 0 && (
              <div>
                <button
                  onClick={() => toggleSection('staticWebApps')}
                  className="flex items-center gap-2 w-full text-left mb-2"
                >
                  {expandedSections.staticWebApps ? (
                    <ChevronDown className="w-4 h-4 text-gray-500" />
                  ) : (
                    <ChevronRight className="w-4 h-4 text-gray-500" />
                  )}
                  <Globe className="w-4 h-4 text-cyan-500" />
                  <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Static Web Apps ({resources.staticWebApps.length})
                  </span>
                </button>
                {expandedSections.staticWebApps && (
                  <div className="ml-6 space-y-2">
                    {resources.staticWebApps.map((swa) => (
                      <button
                        key={swa.name}
                        onClick={() => onStaticWebAppSelected?.(swa)}
                        className="w-full text-left p-3 rounded-lg border bg-gray-50 dark:bg-gray-700 border-gray-200 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors"
                      >
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <CheckCircle2 className="w-4 h-4 text-green-500" />
                            <span className="font-mono text-sm text-gray-900 dark:text-white">
                              {swa.name}
                            </span>
                          </div>
                          <a
                            href={`https://${swa.defaultHostname}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={(e) => e.stopPropagation()}
                            className="text-blue-500 hover:text-blue-700"
                          >
                            <ExternalLink className="w-4 h-4" />
                          </a>
                        </div>
                        <div className="flex items-center gap-4 mt-1 ml-6">
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            RG: {swa.resourceGroup}
                          </span>
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            {getLocationDisplay(swa.location)}
                          </span>
                        </div>
                        <div className="mt-1 ml-6">
                          <span className="text-xs text-blue-500 dark:text-blue-400">
                            https://{swa.defaultHostname}
                          </span>
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default ResourceDiscoveryPanel;
