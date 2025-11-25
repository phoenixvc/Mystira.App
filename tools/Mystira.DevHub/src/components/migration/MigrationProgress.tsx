interface MigrationProgressProps {
  currentOperation: string;
}

export function MigrationProgress({ currentOperation }: MigrationProgressProps) {
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Migration in Progress</h3>

      <div className="space-y-6">
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 dark:border-blue-400"></div>
          <div>
            <div className="font-medium text-gray-900 dark:text-white">{currentOperation}</div>
            <div className="text-sm text-gray-500 dark:text-gray-400">Please wait while migration is in progress...</div>
          </div>
        </div>

        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <p className="text-sm text-blue-800">
            Do not close this window while migration is running. The process may take several minutes depending on the amount of data.
          </p>
        </div>
      </div>
    </div>
  );
}

