import { useState } from 'react';

export interface ErrorDisplayProps {
  error: string | null;
  details?: Record<string, unknown> | null;
  onCopy?: () => void;
  className?: string;
}

export function ErrorDisplay({ error, details, onCopy, className = '' }: ErrorDisplayProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    const text = details
      ? `${error}\n\nDetails:\n${JSON.stringify(details, null, 2)}`
      : error || '';
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
    onCopy?.();
  };

  if (!error) return null;

  return (
    <div className={`p-3 text-xs ${className}`}>
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className="flex items-center gap-2 text-red-600 dark:text-red-400 font-semibold">
          <span>✕</span>
          <span>Error</span>
        </div>
        <button
          onClick={handleCopy}
          className="px-2 py-0.5 text-[10px] bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded text-gray-600 dark:text-gray-400 transition-colors"
        >
          {copied ? '✓ Copied' : 'Copy'}
        </button>
      </div>

      <pre className="whitespace-pre-wrap break-words text-red-700 dark:text-red-300 bg-red-50 dark:bg-red-900/30 p-3 rounded border border-red-200 dark:border-red-800 overflow-x-auto">
        {error}
      </pre>

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

