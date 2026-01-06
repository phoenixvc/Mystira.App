interface CosmosDbPreviewWarningProps {
  affectedResources?: string[];
  onDismiss?: () => void;
  compact?: boolean;
}

function CosmosDbPreviewWarning({
  affectedResources = [],
  onDismiss,
  compact = false
}: CosmosDbPreviewWarningProps) {
  return (
    <div className={`bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg ${compact ? 'p-3' : 'p-4'}`}>
      <div className="flex items-start gap-3">
        <span className={compact ? 'text-lg' : 'text-2xl'}>⚠️</span>
        <div className="flex-1 min-w-0">
          <h4 className={`font-semibold text-yellow-900 dark:text-yellow-300 ${compact ? 'text-sm' : ''}`}>
            Expected Cosmos DB Preview Warnings
          </h4>
          <p className={`text-yellow-800 dark:text-yellow-400 mt-1 ${compact ? 'text-xs' : 'text-sm'}`}>
            Azure's what-if preview cannot predict changes for nested Cosmos DB resources (databases and containers) that don't exist yet.
            This is a <a
              href="https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-what-if"
              target="_blank"
              rel="noopener noreferrer"
              className="underline hover:text-yellow-600 dark:hover:text-yellow-200"
            >
              known Azure limitation
            </a> and does not affect actual deployment.
          </p>

          {affectedResources.length > 0 && (
            <div className={`mt-2 ${compact ? 'text-xs' : 'text-sm'}`}>
              <span className="font-medium text-yellow-900 dark:text-yellow-300">Affected resources: </span>
              <span className="text-yellow-700 dark:text-yellow-400">
                {affectedResources.join(', ')}
              </span>
            </div>
          )}

          <div className={`mt-3 flex items-center gap-4 ${compact ? 'text-xs' : 'text-sm'}`}>
            <span className="text-yellow-700 dark:text-yellow-400">
              You can safely proceed with deployment.
            </span>
            {onDismiss && (
              <button
                onClick={onDismiss}
                className="text-yellow-600 dark:text-yellow-300 hover:text-yellow-800 dark:hover:text-yellow-100 font-medium underline"
              >
                Dismiss
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default CosmosDbPreviewWarning;
