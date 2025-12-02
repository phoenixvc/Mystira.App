import { useState } from 'react';
import {
  Rocket,
  GitBranch,
  Server,
  ArrowRight,
  CheckCircle2,
  Info,
  Zap,
} from 'lucide-react';
import type { ResourceGroup, StaticWebApp } from './ResourceDiscoveryPanel';

export type DeployMode = 'infrastructure' | 'code' | 'auto';

interface SmartDeployDecisionProps {
  hasInfrastructure: boolean;
  selectedResourceGroup: ResourceGroup | null;
  selectedStaticWebApp: StaticWebApp | null;
  onDeployModeSelect: (mode: DeployMode) => void;
  selectedMode: DeployMode;
}

export function SmartDeployDecision({
  hasInfrastructure,
  selectedResourceGroup,
  selectedStaticWebApp,
  onDeployModeSelect,
  selectedMode,
}: SmartDeployDecisionProps) {
  const [showInfo, setShowInfo] = useState(false);

  // Auto-suggest based on infrastructure state
  const suggestedMode: DeployMode = hasInfrastructure ? 'code' : 'infrastructure';

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20">
        <div className="flex items-center gap-3">
          <Zap className="w-5 h-5 text-blue-500" />
          <span className="font-semibold text-gray-900 dark:text-white">
            Smart Deploy Decision
          </span>
        </div>
        <button
          onClick={() => setShowInfo(!showInfo)}
          className="p-1 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
        >
          <Info className="w-4 h-4" />
        </button>
      </div>

      {/* Info Panel */}
      {showInfo && (
        <div className="px-4 py-3 bg-blue-50 dark:bg-blue-900/20 border-b border-blue-100 dark:border-blue-800">
          <p className="text-sm text-blue-700 dark:text-blue-300">
            <strong>Smart Deploy</strong> automatically determines the best deployment path:
          </p>
          <ul className="mt-2 text-sm text-blue-600 dark:text-blue-400 space-y-1 ml-4 list-disc">
            <li><strong>No infrastructure:</strong> Deploy infrastructure first using Bicep templates</li>
            <li><strong>Infrastructure exists:</strong> Deploy code by pushing to trigger CI/CD</li>
          </ul>
        </div>
      )}

      {/* Status Banner */}
      <div className={`px-4 py-3 border-b ${
        hasInfrastructure
          ? 'bg-green-50 dark:bg-green-900/20 border-green-100 dark:border-green-800'
          : 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-100 dark:border-yellow-800'
      }`}>
        <div className="flex items-center gap-2">
          <CheckCircle2 className={`w-4 h-4 ${
            hasInfrastructure ? 'text-green-500' : 'text-yellow-500'
          }`} />
          <span className={`text-sm font-medium ${
            hasInfrastructure
              ? 'text-green-700 dark:text-green-300'
              : 'text-yellow-700 dark:text-yellow-300'
          }`}>
            {hasInfrastructure
              ? 'Infrastructure is deployed. Ready to deploy code.'
              : 'Infrastructure not deployed. Deploy infrastructure first.'}
          </span>
        </div>
        {selectedResourceGroup && (
          <div className="mt-1 text-xs text-gray-500 dark:text-gray-400 ml-6">
            Selected: {selectedResourceGroup.name} ({selectedResourceGroup.location})
          </div>
        )}
      </div>

      {/* Deploy Options */}
      <div className="p-4 space-y-3">
        {/* Deploy Infrastructure */}
        <button
          onClick={() => onDeployModeSelect('infrastructure')}
          disabled={hasInfrastructure}
          className={`w-full p-4 rounded-lg border-2 transition-all text-left ${
            selectedMode === 'infrastructure'
              ? 'border-purple-500 bg-purple-50 dark:bg-purple-900/20'
              : hasInfrastructure
              ? 'border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-700/50 opacity-50 cursor-not-allowed'
              : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-purple-300 dark:hover:border-purple-700'
          }`}
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className={`p-2 rounded-lg ${
                selectedMode === 'infrastructure'
                  ? 'bg-purple-100 dark:bg-purple-800'
                  : 'bg-gray-100 dark:bg-gray-700'
              }`}>
                <Server className={`w-5 h-5 ${
                  selectedMode === 'infrastructure'
                    ? 'text-purple-600 dark:text-purple-400'
                    : 'text-gray-500 dark:text-gray-400'
                }`} />
              </div>
              <div>
                <div className="font-medium text-gray-900 dark:text-white">
                  Deploy Infrastructure
                </div>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  Create Azure resources using Bicep templates
                </div>
              </div>
            </div>
            {suggestedMode === 'infrastructure' && !hasInfrastructure && (
              <span className="px-2 py-1 text-xs font-medium text-purple-700 dark:text-purple-300 bg-purple-100 dark:bg-purple-800 rounded">
                Recommended
              </span>
            )}
          </div>
          {hasInfrastructure && (
            <div className="mt-2 ml-12 text-xs text-gray-400 dark:text-gray-500">
              Infrastructure already deployed. Use code deployment instead.
            </div>
          )}
        </button>

        {/* Deploy Code */}
        <button
          onClick={() => onDeployModeSelect('code')}
          disabled={!hasInfrastructure}
          className={`w-full p-4 rounded-lg border-2 transition-all text-left ${
            selectedMode === 'code'
              ? 'border-green-500 bg-green-50 dark:bg-green-900/20'
              : !hasInfrastructure
              ? 'border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-700/50 opacity-50 cursor-not-allowed'
              : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-green-300 dark:hover:border-green-700'
          }`}
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className={`p-2 rounded-lg ${
                selectedMode === 'code'
                  ? 'bg-green-100 dark:bg-green-800'
                  : 'bg-gray-100 dark:bg-gray-700'
              }`}>
                <GitBranch className={`w-5 h-5 ${
                  selectedMode === 'code'
                    ? 'text-green-600 dark:text-green-400'
                    : 'text-gray-500 dark:text-gray-400'
                }`} />
              </div>
              <div>
                <div className="font-medium text-gray-900 dark:text-white">
                  Deploy Code
                </div>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  Push to trigger CI/CD pipeline
                </div>
              </div>
            </div>
            {suggestedMode === 'code' && hasInfrastructure && (
              <span className="px-2 py-1 text-xs font-medium text-green-700 dark:text-green-300 bg-green-100 dark:bg-green-800 rounded">
                Recommended
              </span>
            )}
          </div>
          {!hasInfrastructure && (
            <div className="mt-2 ml-12 text-xs text-gray-400 dark:text-gray-500">
              Deploy infrastructure first before deploying code.
            </div>
          )}
        </button>

        {/* Visual indicator of the flow */}
        <div className="flex items-center justify-center gap-2 py-2 text-gray-400 dark:text-gray-500">
          <Server className="w-4 h-4" />
          <ArrowRight className="w-4 h-4" />
          <GitBranch className="w-4 h-4" />
          <ArrowRight className="w-4 h-4" />
          <Rocket className="w-4 h-4" />
        </div>
        <p className="text-center text-xs text-gray-400 dark:text-gray-500">
          Infrastructure → Code → Live Application
        </p>
      </div>
    </div>
  );
}

export default SmartDeployDecision;
