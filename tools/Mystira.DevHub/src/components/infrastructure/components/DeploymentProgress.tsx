interface DeploymentProgressProps {
  progress: string | null;
}

export function DeploymentProgress({ progress }: DeploymentProgressProps) {
  if (!progress) return null;

  return (
    <div className="mb-6 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
      <div className="flex items-center gap-3">
        <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 dark:border-blue-400"></div>
        <p className="text-sm font-medium text-blue-900 dark:text-blue-200">
          {progress}
        </p>
      </div>
    </div>
  );
}

