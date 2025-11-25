import { useEffect, useMemo, useRef, useState } from 'react';
import { LogFilterBar } from './logs/LogFilterBar';
import { copyLogsToClipboard, exportLogs, findErrorIndices, formatTimestamp, highlightSearch } from './logs/logUtils';
import { useLogGrouping } from './logs/useLogGrouping';
import { LogFilter, ServiceLog } from './types';

interface LogsViewerProps {
  serviceName: string;
  logs: ServiceLog[];
  filteredLogs: ServiceLog[];
  filter: LogFilter;
  isAutoScroll: boolean;
  isMaximized: boolean;
  containerClass: string;
  maxLogs?: number;
  onFilterChange: (filter: LogFilter) => void;
  onAutoScrollChange: (enabled: boolean) => void;
  onMaxLogsChange?: (limit: number) => void;
}

export function LogsViewer({
  serviceName,
  logs,
  filteredLogs,
  filter,
  isAutoScroll,
  isMaximized,
  containerClass,
  maxLogs = 10000,
  onFilterChange,
  onAutoScrollChange,
  onMaxLogsChange,
}: LogsViewerProps) {
  const logEndRef = useRef<HTMLDivElement>(null);
  const logLineRefs = useRef<Map<number, HTMLDivElement>>(new Map());
  const [autoScrollToErrors, setAutoScrollToErrors] = useState(false);
  const [showLineNumbers, setShowLineNumbers] = useState(true);
  const [collapseSimilar, setCollapseSimilar] = useState(false);
  const [timestampFormat, setTimestampFormat] = useState<'time' | 'full' | 'relative'>('time');
  const [currentErrorIndex, setCurrentErrorIndex] = useState<number>(-1);

  const { groupedLogs, collapsedGroups, toggleGroup } = useLogGrouping(filteredLogs, collapseSimilar);
  const errorIndices = useMemo(() => findErrorIndices(filteredLogs), [filteredLogs]);

  // Format timestamp helper
  const formatTimestampHelper = (timestamp: number) => formatTimestamp(timestamp, timestampFormat);

  // Auto-scroll to errors
  useEffect(() => {
    if (autoScrollToErrors && errorIndices.length > 0 && currentErrorIndex >= 0) {
      const errorIndex = errorIndices[currentErrorIndex];
      const element = logLineRefs.current.get(errorIndex);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }
  }, [autoScrollToErrors, currentErrorIndex, errorIndices]);

  const logContainerRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to end - only scroll within log container, not main window
  useEffect(() => {
    if (isAutoScroll && logEndRef.current && logContainerRef.current) {
      const container = logContainerRef.current;
      const isNearBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 100;
      if (isNearBottom) {
        // Scroll within container only
        container.scrollTo({
          top: container.scrollHeight,
          behavior: 'smooth'
        });
      }
    }
  }, [filteredLogs, isAutoScroll]);

  // Auto-scroll to new errors
  useEffect(() => {
    if (autoScrollToErrors && errorIndices.length > 0) {
      const lastErrorIndex = errorIndices[errorIndices.length - 1];
      const element = logLineRefs.current.get(lastErrorIndex);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        setCurrentErrorIndex(errorIndices.length - 1);
      }
    }
  }, [errorIndices.length, autoScrollToErrors]);

  // Export logs
  const handleExportLogs = async () => {
    try {
      await exportLogs(serviceName, filteredLogs, formatTimestampHelper);
    } catch (error) {
      console.error('Failed to export logs:', error);
      alert(`Failed to export logs: ${error}`);
    }
  };

  // Copy visible (filtered) logs
  const handleCopyVisible = async () => {
    try {
      await copyLogsToClipboard(filteredLogs, formatTimestampHelper);
      // Show brief feedback (could use toast if available)
      const button = document.activeElement as HTMLElement;
      const originalText = button.textContent;
      button.textContent = '✓ Copied!';
      setTimeout(() => {
        button.textContent = originalText;
      }, 1500);
    } catch (error) {
      console.error('Failed to copy logs:', error);
      alert(`Failed to copy logs: ${error}`);
    }
  };

  // Copy all logs
  const handleCopyAll = async () => {
    try {
      await copyLogsToClipboard(logs, formatTimestampHelper);
      // Show brief feedback
      const button = document.activeElement as HTMLElement;
      const originalText = button.textContent;
      button.textContent = '✓ Copied!';
      setTimeout(() => {
        button.textContent = originalText;
      }, 1500);
    } catch (error) {
      console.error('Failed to copy logs:', error);
      alert(`Failed to copy logs: ${error}`);
    }
  };

  // Copy log line
  const handleCopyLog = async (log: ServiceLog) => {
    const logText = `[${formatTimestampHelper(log.timestamp)}] [${log.service}] ${log.message}`;
    try {
      await navigator.clipboard.writeText(logText);
    } catch (error) {
      console.error('Failed to copy log:', error);
    }
  };

  // Navigate to next/previous error
  const navigateError = (direction: 'next' | 'prev') => {
    if (errorIndices.length === 0) return;

    let newIndex = currentErrorIndex;
    if (direction === 'next') {
      newIndex = currentErrorIndex < errorIndices.length - 1 ? currentErrorIndex + 1 : 0;
    } else {
      newIndex = currentErrorIndex > 0 ? currentErrorIndex - 1 : errorIndices.length - 1;
    }

    setCurrentErrorIndex(newIndex);
    const errorIndex = errorIndices[newIndex];
    const element = logLineRefs.current.get(errorIndex);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'center' });
      element.classList.add('ring-2', 'ring-red-500');
      setTimeout(() => {
        element.classList.remove('ring-2', 'ring-red-500');
      }, 1000);
    }
  };

  // Apply filter preset
  const applyPreset = (preset: 'build-errors' | 'runtime-warnings' | 'all-errors' | 'build-only' | 'runtime-only') => {
    switch (preset) {
      case 'build-errors':
        onFilterChange({ ...filter, source: 'build', severity: 'errors', type: 'all' });
        break;
      case 'runtime-warnings':
        onFilterChange({ ...filter, source: 'run', severity: 'warnings', type: 'all' });
        break;
      case 'all-errors':
        onFilterChange({ ...filter, source: 'all', severity: 'errors', type: 'all' });
        break;
      case 'build-only':
        onFilterChange({ ...filter, source: 'build', severity: 'all', type: 'all' });
        break;
      case 'runtime-only':
        onFilterChange({ ...filter, source: 'run', severity: 'all', type: 'all' });
        break;
    }
  };

  // Flatten grouped logs for display
  const displayLogs = useMemo(() => {
    if (!collapseSimilar) {
      return filteredLogs;
    }

    const flat: ServiceLog[] = [];
    groupedLogs.forEach((group, groupIndex) => {
      const isCollapsed = collapsedGroups.has(groupIndex);
      if (isCollapsed && group.logs.length > 1) {
        flat.push(group.logs[0]); // Show only first log when collapsed
      } else {
        flat.push(...group.logs);
      }
    });
    return flat;
  }, [groupedLogs, collapsedGroups, collapseSimilar, filteredLogs]);

  return (
    <div className={`flex flex-col ${isMaximized ? 'h-full flex-1 min-h-0' : containerClass}`}>
          <LogFilterBar
            filter={filter}
            filteredLogs={filteredLogs}
            logs={logs}
            isAutoScroll={isAutoScroll}
            autoScrollToErrors={autoScrollToErrors}
            showLineNumbers={showLineNumbers}
            collapseSimilar={collapseSimilar}
            timestampFormat={timestampFormat}
            maxLogs={maxLogs}
            errorIndices={errorIndices}
            currentErrorIndex={currentErrorIndex}
            onFilterChange={onFilterChange}
            onAutoScrollChange={onAutoScrollChange}
            onAutoScrollToErrorsChange={setAutoScrollToErrors}
            onShowLineNumbersChange={setShowLineNumbers}
            onCollapseSimilarChange={setCollapseSimilar}
            onTimestampFormatChange={setTimestampFormat}
            onMaxLogsChange={onMaxLogsChange}
            onExport={handleExportLogs}
            onCopyVisible={handleCopyVisible}
            onCopyAll={handleCopyAll}
            onNavigateError={navigateError}
            onApplyPreset={applyPreset}
          />
      
      <div 
        ref={logContainerRef}
        className={`bg-black text-green-400 font-mono text-xs p-4 overflow-y-auto flex-1 relative ${isMaximized ? 'h-full' : ''}`}
      >
        {/* Small scroll-to-bottom button - unobtrusive */}
        {filteredLogs.length > 0 && (
          <button
            onClick={() => {
              if (logContainerRef.current) {
                logContainerRef.current.scrollTo({
                  top: logContainerRef.current.scrollHeight,
                  behavior: 'smooth'
                });
              }
            }}
            className="absolute bottom-1.5 right-1.5 w-5 h-5 bg-gray-900/70 hover:bg-gray-800/80 text-gray-500 hover:text-gray-300 rounded text-[10px] flex items-center justify-center transition-all opacity-50 hover:opacity-90 z-10 border border-gray-700/50"
            title="Scroll to bottom"
          >
            ↓
          </button>
        )}
        {displayLogs.length === 0 ? (
          <div className="text-gray-500">
            {logs.length === 0 
              ? 'No logs yet...' 
              : 'No logs match the current filter'}
          </div>
        ) : (
          <>
            {groupedLogs.map((group, groupIndex) => {
              const firstLog = group.logs[0];
              const isBuildLog = firstLog.source === 'build';
              const messageLower = firstLog.message.toLowerCase();
              const isWarning = messageLower.includes('warning') || messageLower.includes('warn') || messageLower.includes('deprecated');
              const isErrorMsg = firstLog.type === 'stderr' || 
                messageLower.includes('error') || 
                messageLower.includes('failed') || 
                messageLower.includes('exception') || 
                messageLower.includes('fatal');
              
              let textColor = 'text-green-400';
              if (isErrorMsg) {
                textColor = 'text-red-400';
              } else if (isWarning) {
                textColor = 'text-yellow-400';
              }

              const isGroupCollapsed = collapsedGroups.has(groupIndex);
              const shouldShow = !isGroupCollapsed || group.logs.length === 1;
              const displayLogs = shouldShow ? group.logs : [group.logs[0]];

              return (
                <div key={groupIndex}>
                  {displayLogs.map((log, logIndex) => {
                    const actualIndex = filteredLogs.indexOf(log);
                    
                    return (
                      <div
                        key={`${groupIndex}-${logIndex}`}
                        ref={(el) => {
                          if (el) logLineRefs.current.set(actualIndex, el);
                        }}
                        onClick={() => handleCopyLog(log)}
                        className={`${textColor} ${isBuildLog ? 'opacity-90' : ''} hover:bg-gray-900/50 px-1 py-0.5 rounded transition-colors cursor-pointer ${
                          actualIndex === errorIndices[currentErrorIndex] ? 'ring-2 ring-red-500' : ''
                        }`}
                        title={`Click to copy | Line ${actualIndex + 1} - ${isErrorMsg ? 'Error' : isWarning ? 'Warning' : 'Info'}`}
                      >
                        {showLineNumbers && (
                          <span className="text-gray-600 dark:text-gray-500 mr-2 text-[10px]">
                            {actualIndex + 1}
                          </span>
                        )}
                        <span className="text-gray-500 text-[10px]">
                          [{formatTimestampHelper(log.timestamp)}]
                        </span>
                        {isBuildLog && (
                          <span className="text-yellow-400 font-semibold ml-1 text-[10px]">
                            [BUILD]
                          </span>
                        )}
                        <span className="text-gray-500 ml-1 text-[10px]">
                          [{log.service}]
                        </span>
                        {isErrorMsg && (
                          <span className="text-red-500 ml-1 font-bold">⚠</span>
                        )}
                        {isWarning && !isErrorMsg && (
                          <span className="text-yellow-500 ml-1">⚠</span>
                        )}
                        <span className="ml-1">
                          {highlightSearch(log.message, filter.search)}
                        </span>
                      </div>
                    );
                  })}
                  {group.logs.length > 1 && (
                    <button
                      onClick={() => toggleGroup(groupIndex)}
                      className="text-gray-500 text-[10px] ml-4 italic hover:text-gray-400 transition-colors"
                    >
                      {isGroupCollapsed 
                        ? `... ${group.logs.length - 1} more similar log(s) (click to expand)`
                        : `... ${group.logs.length} similar log(s) grouped (click to collapse)`}
                    </button>
                  )}
                </div>
              );
            })}
            <div ref={logEndRef} />
          </>
        )}
      </div>
    </div>
  );
}
