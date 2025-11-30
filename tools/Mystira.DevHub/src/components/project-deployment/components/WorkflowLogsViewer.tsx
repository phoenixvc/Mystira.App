import { useRef, useEffect, useState } from 'react';
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
  projectId,
  logs,
  run,
  showLog,
  onToggle,
  onRefSet,
}: WorkflowLogsViewerProps) {
  const ref = useRef<HTMLDivElement | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [autoScroll, setAutoScroll] = useState(true);
  const [isExpanded, setIsExpanded] = useState(false);

  useEffect(() => {
    onRefSet(ref.current);
  }, [onRefSet]);

  // Auto-scroll to bottom when new logs arrive (if autoScroll is enabled)
  useEffect(() => {
    if (ref.current && autoScroll && (run?.status === 'in_progress' || run?.status === 'queued')) {
      ref.current.scrollIntoView({ behavior: 'smooth', block: 'end' });
    }
  }, [logs, run, autoScroll]);

  // Detect when user manually scrolls
  const handleScroll = () => {
    if (!containerRef.current) return;

    const { scrollTop, scrollHeight, clientHeight } = containerRef.current;
    const isNearBottom = scrollHeight - scrollTop - clientHeight < 50;
    setAutoScroll(isNearBottom);
  };

  if (logs.length === 0) return null;

  const isRunning = run?.status === 'in_progress' || run?.status === 'queued';
  const isCompleted = run?.status === 'completed';
  const isSuccess = isCompleted && run?.conclusion === 'success';
  const isFailed = isCompleted && run?.conclusion === 'failure';

  const getStatusIndicator = () => {
    if (isRunning) {
      return (
        <span className="flex items-center gap-1.5 text-blue-400">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-2 w-2 bg-blue-500"></span>
          </span>
          <span className="text-xs">Live</span>
        </span>
      );
    }
    if (isSuccess) {
      return <span className="text-green-400 text-xs">Completed</span>;
    }
    if (isFailed) {
      return <span className="text-red-400 text-xs">Failed</span>;
    }
    return null;
  };

  const maxHeight = isExpanded ? 'max-h-[600px]' : 'max-h-80';

  return (
    <div className="mt-3" role="region" aria-label={`Workflow logs for ${projectId}`}>
      <div className="flex items-center justify-between mb-2">
        <button
          onClick={onToggle}
          className="text-xs text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 flex items-center gap-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1 rounded px-1"
          aria-expanded={showLog}
          aria-controls={`logs-${projectId}`}
        >
          <span aria-hidden="true">{showLog ? '▼' : '▶'}</span>
          <span>{showLog ? 'Hide Logs' : 'Show Logs'}</span>
          <span className="text-gray-400">({logs.length} lines)</span>
        </button>

        {showLog && (
          <div className="flex items-center gap-3">
            {getStatusIndicator()}
            {isRunning && (
              <button
                onClick={() => setAutoScroll(!autoScroll)}
                className={`text-xs px-2 py-0.5 rounded transition-colors ${
                  autoScroll
                    ? 'bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300'
                    : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400'
                }`}
                aria-pressed={autoScroll}
              >
                Auto-scroll {autoScroll ? 'ON' : 'OFF'}
              </button>
            )}
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
              aria-label={isExpanded ? 'Collapse logs' : 'Expand logs'}
            >
              {isExpanded ? '↙ Collapse' : '↗ Expand'}
            </button>
          </div>
        )}
      </div>

      {showLog && (
        <div
          id={`logs-${projectId}`}
          className={`border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden transition-all duration-200 ${
            isFailed ? 'border-red-300 dark:border-red-800' : ''
          }`}
        >
          {/* Log header */}
          <div className="bg-gray-800 dark:bg-gray-900 px-3 py-1.5 border-b border-gray-700 flex items-center justify-between">
            <span className="text-xs text-gray-400 font-mono">
              {run?.name || 'Workflow Logs'}
            </span>
            {run?.html_url && (
              <a
                href={run.html_url}
                target="_blank"
                rel="noopener noreferrer"
                className="text-xs text-blue-400 hover:text-blue-300 hover:underline"
              >
                View on GitHub
              </a>
            )}
          </div>

          {/* Log content */}
          <div
            ref={containerRef}
            onScroll={handleScroll}
            className={`bg-gray-900 text-gray-100 font-mono text-xs p-3 ${maxHeight} overflow-y-auto scroll-smooth`}
            tabIndex={0}
            aria-label="Log output"
          >
            {logs.map((line, index) => (
              <div
                key={index}
                className={`whitespace-pre-wrap py-0.5 ${
                  line.includes('❌') || line.toLowerCase().includes('error')
                    ? 'text-red-400 bg-red-900/20'
                    : line.includes('✅') || line.toLowerCase().includes('success')
                    ? 'text-green-400'
                    : line.includes('⚠️') || line.toLowerCase().includes('warning')
                    ? 'text-yellow-400'
                    : line.includes('🚀')
                    ? 'text-blue-400'
                    : 'text-green-400'
                }`}
              >
                <span className="text-gray-500 select-none mr-3">{String(index + 1).padStart(3, ' ')}</span>
                {line}
              </div>
            ))}
            <div ref={ref} />

            {/* Loading indicator for active runs */}
            {isRunning && (
              <div className="flex items-center gap-2 mt-2 text-gray-400 animate-pulse">
                <span className="text-gray-500 select-none mr-3">...</span>
                <span>Waiting for more logs...</span>
              </div>
            )}
          </div>

          {/* Log footer with summary */}
          {isCompleted && (
            <div
              className={`px-3 py-2 text-xs font-medium ${
                isSuccess
                  ? 'bg-green-900/30 text-green-400 border-t border-green-800'
                  : 'bg-red-900/30 text-red-400 border-t border-red-800'
              }`}
            >
              {isSuccess ? '✓ Workflow completed successfully' : '✗ Workflow failed'}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
