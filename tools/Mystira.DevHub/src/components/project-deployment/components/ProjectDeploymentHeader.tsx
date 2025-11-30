interface ProjectDeploymentHeaderProps {
  hasDeployedInfrastructure: boolean;
  deploying: boolean;
  selectedCount: number;
  onDeploy: () => void;
  validationError?: string | null;
}

export function ProjectDeploymentHeader({
  hasDeployedInfrastructure,
  deploying,
  selectedCount,
  onDeploy,
  validationError,
}: ProjectDeploymentHeaderProps) {
  const isDisabled = !hasDeployedInfrastructure || deploying;

  const getButtonText = () => {
    if (deploying) {
      return (
        <span className="flex items-center gap-2">
          <svg
            className="animate-spin h-4 w-4"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
          Deploying...
        </span>
      );
    }
    return (
      <span className="flex items-center gap-2">
        <span aria-hidden="true">🚀</span>
        Deploy Projects
        {selectedCount > 0 && (
          <span className="bg-white/20 px-1.5 py-0.5 rounded text-xs font-bold">
            {selectedCount}
          </span>
        )}
      </span>
    );
  };

  const getDisabledReason = () => {
    if (!hasDeployedInfrastructure) {
      return 'Deploy infrastructure first';
    }
    if (deploying) {
      return 'Deployment in progress';
    }
    return null;
  };

  const disabledReason = getDisabledReason();

  return (
    <div className="mb-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">
            Step 3: Deploy Projects
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Deploy selected projects using GitHub Actions workflows
          </p>
        </div>
        <div className="relative">
          <button
            onClick={onDeploy}
            disabled={isDisabled}
            aria-disabled={isDisabled}
            aria-describedby={disabledReason ? 'deploy-disabled-reason' : undefined}
            className={`
              px-5 py-2.5 rounded-lg font-medium transition-all duration-200
              focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500
              dark:focus:ring-offset-gray-900
              ${isDisabled
                ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                : 'bg-green-600 hover:bg-green-700 active:bg-green-800 text-white shadow-sm hover:shadow-md'
              }
            `}
          >
            {getButtonText()}
          </button>
          {disabledReason && (
            <span
              id="deploy-disabled-reason"
              className="sr-only"
            >
              {disabledReason}
            </span>
          )}
        </div>
      </div>

      {/* Validation error message */}
      {validationError && (
        <div
          role="alert"
          aria-live="polite"
          className="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg flex items-start gap-2"
        >
          <span className="text-red-500 flex-shrink-0" aria-hidden="true">⚠️</span>
          <p className="text-sm text-red-700 dark:text-red-300">{validationError}</p>
        </div>
      )}
    </div>
  );
}
