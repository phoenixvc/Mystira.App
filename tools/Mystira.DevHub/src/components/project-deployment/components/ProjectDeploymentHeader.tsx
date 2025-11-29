interface ProjectDeploymentHeaderProps {
  hasDeployedInfrastructure: boolean;
  deploying: boolean;
  selectedCount: number;
  onDeploy: () => void;
}

export function ProjectDeploymentHeader({
  hasDeployedInfrastructure,
  deploying,
  selectedCount,
  onDeploy,
}: ProjectDeploymentHeaderProps) {
  return (
    <div className="flex items-center justify-between mb-4">
      <div>
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">
          Step 3: Deploy Projects
        </h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Deploy selected projects using GitHub Actions workflows
        </p>
      </div>
      <button
        onClick={onDeploy}
        disabled={!hasDeployedInfrastructure || deploying || selectedCount === 0}
        className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        {deploying ? '🚀 Deploying...' : '🚀 Deploy Projects'}
      </button>
    </div>
  );
}
