import { useEffect, useRef } from 'react';

interface CliBuildLogsViewerProps {
  isBuilding: boolean;
  logs: string[];
  showLogs: boolean;
  onClose: () => void;
}

export function CliBuildLogsViewer({ isBuilding, logs, showLogs, onClose }: CliBuildLogsViewerProps) {
  const logsEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (logsEndRef.current && (isBuilding || logs.length > 0)) {
      logsEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [logs, isBuilding]);

  if (!showLogs && logs.length === 0 && !isBuilding) {
    return null;
  }

  return (
    <div className="mb-6 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
      <div className="bg-gray-50 dark:bg-gray-800 px-4 py-2 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center gap-2">
          <h3 className="font-semibold text-gray-900 dark:text-white">CLI Build Logs</h3>
          {isBuilding && (
            <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></span>
          )}
        </div>
        <button
          onClick={onClose}
          className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
          title="Close logs"
        >
          ✕
        </button>
      </div>
      <div className="bg-gray-900 text-green-400 font-mono text-sm p-4 max-h-96 overflow-y-auto">
        {logs.length === 0 ? (
          <div className="text-gray-500">Waiting for build output...</div>
        ) : (
          <>
            {logs.map((line, index) => (
              <div key={index} className="whitespace-pre-wrap">
                {line}
              </div>
            ))}
            <div ref={logsEndRef} />
          </>
        )}
      </div>
    </div>
  );
}

