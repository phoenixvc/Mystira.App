import { useEffect, useMemo, useRef, useState } from 'react';
import { LogFilterBar } from './logs/LogFilterBar';
import { LogGroup } from './logs/LogGroup';
import { copyLogsToClipboard, exportLogs, findErrorIndices, formatTimestamp } from './logs/logUtils';
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
  onClearLogs?: () => void;
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
  onClearLogs,
}: LogsViewerProps) {
  const logEndRef = useRef<HTMLDivElement>(null);
  const logLineRefs = useRef<Map<number, HTMLDivElement>>(new Map());
  const [autoScrollToErrors, setAutoScrollToErrors] = useState(false);
  const [showLineNumbers, setShowLineNumbers] = useState(true);
  const [collapseSimilar, setCollapseSimilar] = useState(false);
  const [wordWrap, setWordWrap] = useState(true);
  const [timestampFormat, setTimestampFormat] = useState<'time' | 'full' | 'relative'>('time');
  const [currentErrorIndex, setCurrentErrorIndex] = useState<number>(-1);

  const { groupedLogs, collapsedGroups, toggleGroup } = useLogGrouping(filteredLogs, collapseSimilar);
  const errorIndices = useMemo(() => findErrorIndices(filteredLogs), [filteredLogs]);

  const formatTimestampHelper = (timestamp: number) => formatTimestamp(timestamp, timestampFormat);

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

  useEffect(() => {
    if (isAutoScroll && logEndRef.current && logContainerRef.current) {
      const container = logContainerRef.current;
      const isNearBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 100;
      if (isNearBottom) {
        container.scrollTo({
          top: container.scrollHeight,
          behavior: 'smooth',
        });
      }
    }
  }, [filteredLogs, isAutoScroll]);

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

  const handleExportLogs = async () => {
    try {
      await exportLogs(serviceName, filteredLogs, formatTimestampHelper);
    } catch (error) {
      console.error('Failed to export logs:', error);
      alert(`Failed to export logs: ${error}`);
    }
  };

  const handleCopyVisible = async () => {
    const visibleLogs = filteredLogs;
    try {
      await copyLogsToClipboard(visibleLogs, formatTimestampHelper);
    } catch (error) {
      console.error('Failed to copy logs:', error);
      alert(`Failed to copy logs: ${error}`);
    }
  };

  const handleCopyAll = async () => {
    try {
      await copyLogsToClipboard(logs, formatTimestampHelper);
    } catch (error) {
      console.error('Failed to copy logs:', error);
      alert(`Failed to copy logs: ${error}`);
    }
  };

  const handleCopyLog = (log: ServiceLog) => {
    navigator.clipboard.writeText(`${formatTimestampHelper(log.timestamp)} [${log.service}] ${log.message}`);
  };

  const navigateError = (direction: 'next' | 'prev') => {
    if (errorIndices.length === 0) return;

    let newIndex: number;
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

  const displayLogs = useMemo(() => {
    if (!collapseSimilar) {
      return filteredLogs;
    }
    const flat: ServiceLog[] = [];
    groupedLogs.forEach((group, groupIndex) => {
      const isCollapsed = collapsedGroups.has(groupIndex);
      if (isCollapsed && group.logs.length > 1) {
        flat.push(group.logs[0]);
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
        wordWrap={wordWrap}
        timestampFormat={timestampFormat}
        maxLogs={maxLogs}
        errorIndices={errorIndices}
        currentErrorIndex={currentErrorIndex}
        onFilterChange={onFilterChange}
        onAutoScrollChange={onAutoScrollChange}
        onAutoScrollToErrorsChange={setAutoScrollToErrors}
        onShowLineNumbersChange={setShowLineNumbers}
        onCollapseSimilarChange={setCollapseSimilar}
        onWordWrapChange={setWordWrap}
        onTimestampFormatChange={setTimestampFormat}
        onMaxLogsChange={onMaxLogsChange}
        onExport={handleExportLogs}
        onCopyVisible={handleCopyVisible}
        onCopyAll={handleCopyAll}
        onNavigateError={navigateError}
        onApplyPreset={applyPreset}
        onClearLogs={onClearLogs}
      />

      <div
        ref={logContainerRef}
        className={`bg-black text-green-400 font-mono text-xs p-4 overflow-y-auto flex-1 relative ${isMaximized ? 'h-full' : ''} ${wordWrap ? '' : 'overflow-x-auto'}`}
        style={wordWrap ? {} : { whiteSpace: 'nowrap' }}
      >
        {filteredLogs.length > 0 && (
          <button
            onClick={() => {
              if (logContainerRef.current) {
                logContainerRef.current.scrollTo({
                  top: logContainerRef.current.scrollHeight,
                  behavior: 'smooth',
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
            {logs.length === 0 ? 'No logs yet...' : 'No logs match the current filter'}
          </div>
        ) : (
          <>
            {groupedLogs.map((group, groupIndex) => (
              <LogGroup
                key={groupIndex}
                group={group}
                groupIndex={groupIndex}
                filteredLogs={filteredLogs}
                showLineNumbers={showLineNumbers}
                wordWrap={wordWrap}
                timestampFormat={timestampFormat}
                filterSearch={filter.search}
                errorIndices={errorIndices}
                currentErrorIndex={currentErrorIndex}
                collapsedGroups={collapsedGroups}
                onToggleGroup={toggleGroup}
                onCopyLog={handleCopyLog}
                logLineRefs={logLineRefs}
              />
            ))}
            <div ref={logEndRef} />
          </>
        )}
      </div>
    </div>
  );
}

