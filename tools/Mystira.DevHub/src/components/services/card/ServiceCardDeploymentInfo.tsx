import { formatDeploymentInfo, type DeploymentInfo } from '../components';
import type { ServiceConfig } from '../types';

interface ServiceCardDeploymentInfoProps {
  config: ServiceConfig;
  currentEnv: 'local' | 'dev' | 'prod';
  deploymentInfo?: DeploymentInfo | null;
}

export function ServiceCardDeploymentInfo({
  config,
  currentEnv,
  deploymentInfo,
}: ServiceCardDeploymentInfoProps) {
  if (currentEnv === 'local' || !config.url) {
    return null;
  }

  return (
    <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700 space-y-1">
      <div className="flex items-center gap-2 text-sm">
        <span className="text-gray-600 dark:text-gray-400 font-medium">URL:</span>
        <code className="px-2 py-1 bg-gray-100 dark:bg-gray-700 rounded text-xs text-gray-800 dark:text-gray-200 font-mono">
          {config.url}
        </code>
        <button
          onClick={() => navigator.clipboard.writeText(config.url || '')}
          className="px-2 py-1 text-xs bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-500 transition-colors"
          title="Copy URL to clipboard"
        >
          📋 Copy
        </button>
      </div>
      {deploymentInfo && (() => {
        const info = formatDeploymentInfo(deploymentInfo);
        return (
          <div className="flex items-center gap-2 text-xs">
            <span className={info.statusColor} title={info.tooltip}>
              {info.text}
            </span>
            {deploymentInfo.workflowRunUrl && (
              <a
                href={deploymentInfo.workflowRunUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="text-blue-500 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                title="View deployment in GitHub Actions"
              >
                🔗 View Deployment
              </a>
            )}
          </div>
        );
      })()}
    </div>
  );
}

