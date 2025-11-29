export interface SuccessDisplayProps {
  message: string | null;
  details?: Record<string, unknown> | null;
  className?: string;
}

export function SuccessDisplay({ message, details, className = '' }: SuccessDisplayProps) {
  if (!message) return null;

  return (
    <div className={`p-3 text-xs ${className}`}>
      <div className="flex items-center gap-2 text-green-600 dark:text-green-400 font-semibold mb-2">
        <span>✓</span>
        <span>Success</span>
      </div>

      <p className="text-green-700 dark:text-green-300 bg-green-50 dark:bg-green-900/30 p-3 rounded border border-green-200 dark:border-green-800">
        {message}
      </p>

      {details && (
        <div className="mt-3">
          <details className="text-xs">
            <summary className="cursor-pointer text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 font-medium">
              Details
            </summary>
            <pre className="mt-2 p-2 bg-gray-100 dark:bg-gray-800 rounded text-[10px] overflow-auto max-h-64">
              {JSON.stringify(details, null, 2)}
            </pre>
          </details>
        </div>
      )}
    </div>
  );
}

