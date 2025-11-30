import { BOTTOM_PANEL_TABS } from '../../types/constants';
import type { BottomPanelTab } from './VSCodeLayout';
import { LogsViewer } from '../services/LogsViewer';
import type { LogEntry, LogFilter } from '../services/types';

interface AppBottomPanelProps {
  allLogs: LogEntry[];
  filteredLogs: LogEntry[];
  problemsCount: number;
  logFilter: LogFilter;
  isAutoScroll: boolean;
  onFilterChange: (filter: LogFilter) => void;
  onAutoScrollChange: (enabled: boolean) => void;
  onClearLogs: () => void;
}

export function useAppBottomPanelTabs({
  allLogs,
  filteredLogs,
  problemsCount,
  logFilter,
  isAutoScroll,
  onFilterChange,
  onAutoScrollChange,
  onClearLogs,
}: AppBottomPanelProps): BottomPanelTab[] {
  return [
    {
      id: BOTTOM_PANEL_TABS.OUTPUT,
      title: 'Output',
      icon: '📋',
      badge: (allLogs.length > 0 || problemsCount > 0) ? (allLogs.length + problemsCount) : undefined,
      content: (
        <LogsViewer
          serviceName="Output"
          logs={allLogs}
          filteredLogs={filteredLogs}
          filter={logFilter}
          isAutoScroll={isAutoScroll}
          isMaximized={true}
          containerClass="h-full"
          onFilterChange={onFilterChange}
          onAutoScrollChange={onAutoScrollChange}
          onClearLogs={onClearLogs}
        />
      ),
    },
    {
      id: BOTTOM_PANEL_TABS.TERMINAL,
      title: 'Terminal',
      icon: '▸',
      content: (
        <div className="h-full overflow-auto p-2 font-mono text-xs bg-gray-900 text-gray-300">
          <div className="text-gray-500 italic">Terminal not available in this context</div>
        </div>
      ),
    },
  ];
}

