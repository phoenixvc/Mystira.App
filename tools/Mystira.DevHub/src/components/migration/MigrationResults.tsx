import { MigrationResponse } from './types';

interface MigrationResultsProps {
  results: MigrationResponse;
  onReset: () => void;
}

export function MigrationResults({ results, onReset }: MigrationResultsProps) {
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Migration Complete</h3>

      {results.success ? (
        <div className="bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-800 rounded-lg p-6 mb-6">
          <div className="flex items-start">
            <div className="text-4xl mr-4">✅</div>
            <div className="flex-1">
              <h4 className="text-lg font-semibold text-green-900 dark:text-green-300 mb-2">
                Migration Successful
              </h4>
              {results.result && (
                <div className="text-green-800 dark:text-green-200 space-y-1">
                  <div>
                    <strong>Total Items:</strong> {results.result.totalItems}
                  </div>
                  <div>
                    <strong>Successful:</strong> {results.result.totalSuccess}
                  </div>
                  {results.result.totalFailures > 0 && (
                    <div className="text-yellow-700 dark:text-yellow-400">
                      <strong>Failed:</strong> {results.result.totalFailures}
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      ) : (
        <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-6 mb-6">
          <div className="flex items-start">
            <div className="text-4xl mr-4">❌</div>
            <div className="flex-1">
              <h4 className="text-lg font-semibold text-red-900 dark:text-red-300 mb-2">
                Migration Failed
              </h4>
              <p className="text-red-800 dark:text-red-200">{results.error || 'An error occurred during migration'}</p>
            </div>
          </div>
        </div>
      )}

      {results.result && results.result.results.length > 0 && (
        <div className="mb-6">
          <h4 className="font-semibold text-gray-900 dark:text-white mb-3">Detailed Results</h4>
          <div className="space-y-3">
            {results.result.results.map((result, index) => (
              <div
                key={index}
                className={`border rounded-lg p-4 ${
                  result.success
                    ? 'border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/30'
                    : 'border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/30'
                }`}
              >
                <div className="flex justify-between items-start mb-2">
                  <div className="font-medium text-gray-900 dark:text-white">
                    {result.success ? '✅' : '❌'} Migration {index + 1}
                  </div>
                  <div className="text-sm text-gray-600 dark:text-gray-400">{result.duration}</div>
                </div>
                <div className="text-sm text-gray-700 dark:text-gray-300 space-y-1">
                  <div>Total Items: {result.totalItems}</div>
                  <div>Successful: {result.successCount}</div>
                  {result.failureCount > 0 && (
                    <div className="text-red-600 dark:text-red-400">Failed: {result.failureCount}</div>
                  )}
                  {result.errors.length > 0 && (
                    <div className="mt-2">
                      <div className="font-medium text-red-600 dark:text-red-400">Errors:</div>
                      <ul className="list-disc list-inside text-xs space-y-1">
                        {result.errors.map((error, i) => (
                          <li key={i}>{error}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="flex justify-end">
        <button
          onClick={onReset}
          className="px-6 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors"
        >
          Start New Migration
        </button>
      </div>
    </div>
  );
}

