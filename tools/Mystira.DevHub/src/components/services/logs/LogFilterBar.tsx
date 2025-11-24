import { LogFilter, ServiceLog } from '../types';

interface LogFilterBarProps {
  filter: LogFilter;
  filteredLogs: ServiceLog[];
  logs: ServiceLog[];
  isAutoScroll: boolean;
  autoScrollToErrors: boolean;
  showLineNumbers: boolean;
  collapseSimilar: boolean;
  timestampFormat: 'time' | 'full' | 'relative';
  maxLogs?: number;
  errorIndices: number[];
  currentErrorIndex: number;
  onFilterChange: (filter: LogFilter) => void;
  onAutoScrollChange: (enabled: boolean) => void;
  onAutoScrollToErrorsChange: (enabled: boolean) => void;
  onShowLineNumbersChange: (enabled: boolean) => void;
  onCollapseSimilarChange: (enabled: boolean) => void;
  onTimestampFormatChange: (format: 'time' | 'full' | 'relative') => void;
  onMaxLogsChange?: (limit: number) => void;
  onExport: () => void;
  onNavigateError: (direction: 'next' | 'prev') => void;
  onApplyPreset: (preset: 'build-errors' | 'runtime-warnings' | 'all-errors' | 'build-only' | 'runtime-only') => void;
}

export function LogFilterBar({
  filter,
  filteredLogs,
  logs,
  isAutoScroll,
  autoScrollToErrors,
  showLineNumbers,
  collapseSimilar,
  timestampFormat,
  maxLogs,
  errorIndices,
  currentErrorIndex,
  onFilterChange,
  onAutoScrollChange,
  onAutoScrollToErrorsChange,
  onShowLineNumbersChange,
  onCollapseSimilarChange,
  onTimestampFormatChange,
  onMaxLogsChange,
  onExport,
  onNavigateError,
  onApplyPreset,
}: LogFilterBarProps) {
  const stats = {
    errorCount: filteredLogs.filter(log => {
      const msg = log.message.toLowerCase();
      return log.type === 'stderr' || msg.includes('error') || msg.includes('failed') || msg.includes('exception') || msg.includes('fatal');
    }).length,
    warningCount: filteredLogs.filter(log => {
      const msg = log.message.toLowerCase();
      return msg.includes('warning') || msg.includes('warn') || msg.includes('deprecated');
    }).length,
  };

  return (
    <div className="bg-gray-100 dark:bg-gray-700 p-2 flex gap-2 items-center flex-wrap border-b border-gray-200 dark:border-gray-600">
      {/* Search Input */}
      <input
        type="text"
        placeholder="Search logs..."
        value={filter.search}
        onChange={(e) => onFilterChange({ ...filter, search: e.target.value })}
        className="flex-1 min-w-[180px] px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-xs bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
      />
      
      {/* Filter Presets */}
      <div className="flex gap-1 items-center">
        <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">Presets:</span>
        <button
          onClick={() => onApplyPreset('build-errors')}
          className="px-2 py-0.5 text-[10px] bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 rounded hover:bg-red-200 dark:hover:bg-red-900/50 transition-colors"
          title="Show build errors only"
        >
          Build Errors
        </button>
        <button
          onClick={() => onApplyPreset('runtime-warnings')}
          className="px-2 py-0.5 text-[10px] bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 rounded hover:bg-yellow-200 dark:hover:bg-yellow-900/50 transition-colors"
          title="Show runtime warnings only"
        >
          Runtime Warnings
        </button>
        <button
          onClick={() => onApplyPreset('all-errors')}
          className="px-2 py-0.5 text-[10px] bg-red-200 dark:bg-red-800/40 text-red-800 dark:text-red-200 rounded hover:bg-red-300 dark:hover:bg-red-800/60 transition-colors"
          title="Show all errors"
        >
          All Errors
        </button>
      </div>
      
      {/* Filter Group */}
      <div className="flex gap-1 items-center">
        {/* Severity Filter */}
        <div className="flex gap-0.5 border border-gray-300 dark:border-gray-600 rounded overflow-hidden">
          <button
            onClick={() => onFilterChange({ ...filter, severity: filter.severity === 'errors' ? 'all' : 'errors' })}
            className={`px-2 py-1 text-xs font-medium transition-colors ${
              filter.severity === 'errors'
                ? 'bg-red-500 text-white'
                : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-red-50 dark:hover:bg-red-900/20'
            }`}
            title="Show errors only"
          >
            🔴 Errors
          </button>
          <div className="w-px bg-gray-300 dark:bg-gray-600"></div>
          <button
            onClick={() => onFilterChange({ ...filter, severity: filter.severity === 'warnings' ? 'all' : 'warnings' })}
            className={`px-2 py-1 text-xs font-medium transition-colors ${
              filter.severity === 'warnings'
                ? 'bg-yellow-500 text-white'
                : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-yellow-50 dark:hover:bg-yellow-900/20'
            }`}
            title="Show warnings only"
          >
            ⚠️ Warnings
          </button>
        </div>

        {/* Source Filter */}
        <div className="flex gap-0.5 border border-gray-300 dark:border-gray-600 rounded overflow-hidden">
          <button
            onClick={() => onFilterChange({ ...filter, source: filter.source === 'build' ? 'all' : 'build' })}
            className={`px-2 py-1 text-xs font-medium transition-colors ${
              filter.source === 'build'
                ? 'bg-yellow-600 text-white'
                : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-yellow-50 dark:hover:bg-yellow-900/20'
            }`}
            title="Show build logs only"
          >
            🔨 Build
          </button>
          <div className="w-px bg-gray-300 dark:bg-gray-600"></div>
          <button
            onClick={() => onFilterChange({ ...filter, source: filter.source === 'run' ? 'all' : 'run' })}
            className={`px-2 py-1 text-xs font-medium transition-colors ${
              filter.source === 'run'
                ? 'bg-green-600 text-white'
                : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-green-50 dark:hover:bg-green-900/20'
            }`}
            title="Show runtime logs only"
          >
            ▶️ Run
          </button>
        </div>

        {/* Type Filter */}
        <select
          value={filter.type}
          onChange={(e) => onFilterChange({ ...filter, type: e.target.value as 'all' | 'stdout' | 'stderr' })}
          className="px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-xs bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500"
          title="Filter by stream type"
        >
          <option value="all">All Streams</option>
          <option value="stdout">Stdout</option>
          <option value="stderr">Stderr</option>
        </select>
      </div>

      {/* View Options */}
      <div className="flex gap-1 items-center border-l border-gray-300 dark:border-gray-600 pl-2">
        <label className="flex items-center gap-1 text-xs text-gray-700 dark:text-gray-300" title="Show line numbers">
          <input
            type="checkbox"
            checked={showLineNumbers}
            onChange={(e) => onShowLineNumbersChange(e.target.checked)}
            className="rounded border-gray-300 dark:border-gray-600 w-3 h-3"
          />
          <span>#</span>
        </label>
        <label className="flex items-center gap-1 text-xs text-gray-700 dark:text-gray-300" title="Collapse similar consecutive logs">
          <input
            type="checkbox"
            checked={collapseSimilar}
            onChange={(e) => onCollapseSimilarChange(e.target.checked)}
            className="rounded border-gray-300 dark:border-gray-600 w-3 h-3"
          />
          <span>Collapse</span>
        </label>
        <select
          value={timestampFormat}
          onChange={(e) => onTimestampFormatChange(e.target.value as 'time' | 'full' | 'relative')}
          className="px-1.5 py-0.5 border border-gray-300 dark:border-gray-600 rounded text-[10px] bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
          title="Timestamp format"
        >
          <option value="time">Time</option>
          <option value="full">Full</option>
          <option value="relative">Relative</option>
        </select>
      </div>

      {/* Error Navigation */}
      {errorIndices.length > 0 && (
        <div className="flex gap-1 items-center border-l border-gray-300 dark:border-gray-600 pl-2">
          <button
            onClick={() => onNavigateError('prev')}
            disabled={errorIndices.length === 0}
            className="px-2 py-1 text-xs bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-500 disabled:opacity-50 transition-colors"
            title="Previous error"
          >
            ⬆️ Prev
          </button>
          <span className="text-xs text-gray-600 dark:text-gray-400">
            {currentErrorIndex >= 0 ? `${currentErrorIndex + 1}/${errorIndices.length}` : `0/${errorIndices.length}`}
          </span>
          <button
            onClick={() => onNavigateError('next')}
            disabled={errorIndices.length === 0}
            className="px-2 py-1 text-xs bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-500 disabled:opacity-50 transition-colors"
            title="Next error"
          >
            ⬇️ Next
          </button>
        </div>
      )}

      {/* Export Button */}
      {filteredLogs.length > 0 && (
        <button
          onClick={onExport}
          className="px-2 py-1 text-xs bg-blue-500 text-white rounded hover:bg-blue-600 transition-colors"
          title="Export filtered logs to file"
        >
          💾 Export
        </button>
      )}

      {/* Auto-scroll Options */}
      <div className="flex items-center gap-2 border-l border-gray-300 dark:border-gray-600 pl-2">
        <label className="flex items-center gap-1.5 text-xs text-gray-700 dark:text-gray-300">
          <input
            type="checkbox"
            checked={isAutoScroll}
            onChange={(e) => onAutoScrollChange(e.target.checked)}
            className="rounded border-gray-300 dark:border-gray-600 w-3.5 h-3.5"
          />
          <span>Auto-scroll</span>
        </label>
        <label className="flex items-center gap-1.5 text-xs text-gray-700 dark:text-gray-300" title="Auto-scroll to errors when they occur">
          <input
            type="checkbox"
            checked={autoScrollToErrors}
            onChange={(e) => onAutoScrollToErrorsChange(e.target.checked)}
            className="rounded border-gray-300 dark:border-gray-600 w-3.5 h-3.5"
          />
          <span>Scroll to Errors</span>
        </label>
      </div>

      {/* Statistics and Retention */}
      <div className="flex items-center gap-2 text-xs border-l border-gray-300 dark:border-gray-600 pl-2">
        {stats.errorCount > 0 && (
          <span className="text-red-500 font-medium" title={`${stats.errorCount} error(s)`}>
            🔴 {stats.errorCount}
          </span>
        )}
        {stats.warningCount > 0 && (
          <span className="text-yellow-500 font-medium" title={`${stats.warningCount} warning(s)`}>
            ⚠️ {stats.warningCount}
          </span>
        )}
        <span className="text-gray-600 dark:text-gray-400 font-medium">
          {filteredLogs.length} / {logs.length}
        </span>
        {onMaxLogsChange && (
          <div className="flex items-center gap-1" title="Log retention limit">
            <span className="text-gray-500 dark:text-gray-400">Max:</span>
            <input
              type="number"
              min="100"
              max="100000"
              step="1000"
              value={maxLogs}
              onChange={(e) => {
                const value = parseInt(e.target.value, 10);
                if (!isNaN(value) && value >= 100) {
                  onMaxLogsChange(value);
                }
              }}
              className="w-16 px-1 py-0.5 border border-gray-300 dark:border-gray-600 rounded text-xs bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
              title="Maximum number of logs to retain"
              aria-label="Log retention limit"
            />
          </div>
        )}
      </div>
    </div>
  );
}

