import { useRef, useEffect } from 'react';
import type { WorkflowRun } from '../types';

interface WorkflowLogsViewerProps {
  projectId: string;
  logs: string[];
  run: WorkflowRun | undefined;
  showLog: boolean;
  onToggle: () => void;
  logsEndRef: HTMLDivElement | null;
  onRefSet: (el: HTMLDivElement | null) => void;
}

export function WorkflowLogsViewer({
  logs,
  run,
  showLog,
  onToggle,
  onRefSet,
}: WorkflowLogsViewerProps) {
  const ref = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    onRefSet(ref.current);
  }, [onRefSet]);

  useEffect(() => {
    if (ref.current && (run?.status === 'in_progress' || run?.status === 'queued')) {
      setTimeout(() => ref.current?.scrollIntoView({ behavior: 'smooth' }), 100);
    }
  }, [logs, run]);

  if (logs.length === 0) return null;

  return (
    <>
      <div className="mb-2">
        <button
          onClick={onToggle}
          className="text-xs text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
        >
          {showLog ? '📋 Hide Logs' : '📋 Show Logs'}
        </button>
      </div>

      {showLog && (
        <div className="mt-3 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <div className="bg-gray-900 text-green-400 font-mono text-xs p-3 max-h-64 overflow-y-auto">
            {logs.map((line, index) => (
              <div key={index} className="whitespace-pre-wrap">
                {line}
              </div>
            ))}
            <div ref={ref} />
          </div>
        </div>
      )}
    </>
  );
}

