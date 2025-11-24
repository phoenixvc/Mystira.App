import { useEffect, useRef } from 'react';
import { LogFilter, ServiceLog } from './types';

interface LogsViewerProps {
  serviceName: string;
  logs: ServiceLog[];
  filteredLogs: ServiceLog[];
  filter: LogFilter;
  isAutoScroll: boolean;
  isMaximized: boolean;
  containerClass: string;
  onFilterChange: (filter: LogFilter) => void;
  onAutoScrollChange: (enabled: boolean) => void;
}

export function LogsViewer({
  serviceName: _serviceName,
  logs,
  filteredLogs,
  filter,
  isAutoScroll,
  isMaximized,
  containerClass,
  onFilterChange,
  onAutoScrollChange,
}: LogsViewerProps) {
  const logEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isAutoScroll && logEndRef.current) {
      logEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [filteredLogs, isAutoScroll]);

  return (
    <div className={`flex flex-col ${isMaximized ? 'h-full flex-1 min-h-0' : containerClass}`}>
      {/* Log Filter Controls */}
      <div className="bg-gray-100 dark:bg-gray-700 p-2 flex gap-2 items-center flex-wrap border-b border-gray-200 dark:border-gray-600">
        <input
          type="text"
          placeholder="Search logs..."
          value={filter.search}
          onChange={(e) => onFilterChange({ ...filter, search: e.target.value })}
          className="flex-1 min-w-[200px] px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500"
        />
        <select
          value={filter.type}
          onChange={(e) => onFilterChange({ ...filter, type: e.target.value as 'all' | 'stdout' | 'stderr' })}
          className="px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
          title="Filter log type"
          aria-label="Filter log type"
        >
          <option value="all">All</option>
          <option value="stdout">Stdout</option>
          <option value="stderr">Stderr</option>
        </select>
        <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
          <input
            type="checkbox"
            checked={isAutoScroll}
            onChange={(e) => onAutoScrollChange(e.target.checked)}
            className="rounded border-gray-300 dark:border-gray-600"
          />
          <span>Auto-scroll</span>
        </label>
        <span className="text-sm text-gray-600 dark:text-gray-400">
          {filteredLogs.length} / {logs.length} lines
        </span>
      </div>
      
      {/* Log Display */}
      <div className={`bg-black text-green-400 font-mono text-xs p-4 overflow-y-auto flex-1 ${isMaximized ? 'h-full' : ''}`}>
        {filteredLogs.length === 0 ? (
          <div className="text-gray-500">
            {logs.length === 0 
              ? 'No logs yet...' 
              : 'No logs match the current filter'}
          </div>
        ) : (
          <>
            {filteredLogs.map((log, index) => (
              <div
                key={index}
                className={log.type === 'stderr' ? 'text-red-400' : 'text-green-400'}
              >
                <span className="text-gray-500">
                  [{new Date(log.timestamp).toLocaleTimeString()}] [{log.service}]
                </span>{' '}
                {log.message}
              </div>
            ))}
            <div ref={logEndRef} />
          </>
        )}
      </div>
    </div>
  );
}

