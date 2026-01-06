import { ServiceLog } from '../types';
import { LogLine } from './LogLine';
import { isErrorMessage, isStackTraceLine } from './logUtils';

interface LogGroupProps {
  group: { logs: ServiceLog[] };
  groupIndex: number;
  filteredLogs: ServiceLog[];
  showLineNumbers: boolean;
  wordWrap: boolean;
  timestampFormat: 'time' | 'full' | 'relative';
  filterSearch: string;
  errorIndices: number[];
  currentErrorIndex: number;
  collapsedGroups: Set<number>;
  onToggleGroup: (index: number) => void;
  onCopyLog: (log: ServiceLog) => void;
  logLineRefs: React.MutableRefObject<Map<number, HTMLDivElement>>;
}

export function LogGroup({
  group,
  groupIndex,
  filteredLogs,
  showLineNumbers,
  wordWrap,
  timestampFormat,
  filterSearch,
  errorIndices,
  currentErrorIndex,
  collapsedGroups,
  onToggleGroup,
  onCopyLog,
  logLineRefs,
}: LogGroupProps) {
  const firstLog = group.logs[0];
  const isBuildLog = firstLog.source === 'build';
  const messageLower = firstLog.message.toLowerCase();
  
  // Use proper error detection (excludes count messages)
  const isErrorMsg = firstLog.type === 'stderr' || isErrorMessage(firstLog.message);
  const isWarning = !isErrorMsg && (
    messageLower.includes('warning') || 
    messageLower.includes('warn') || 
    messageLower.includes('deprecated')
  );

  const isGroupCollapsed = collapsedGroups.has(groupIndex);
  const shouldShow = !isGroupCollapsed || group.logs.length === 1;
  const displayLogs = shouldShow ? group.logs : [group.logs[0]];

  return (
    <div key={groupIndex}>
      {displayLogs.map((log, logIndex) => {
        const actualIndex = filteredLogs.indexOf(log);
        // Check if this is a stack trace continuation line
        const isStackTrace = isStackTraceLine(log.message);
        // Stack trace lines should be treated as errors (red, no timestamp)
        const logIsError = isErrorMsg || (isStackTrace && logIndex > 0 && isErrorMsg);
        // Don't show warning status for stack traces
        const logIsWarning = !logIsError && isWarning && !isStackTrace;
        
        return (
          <div
            key={`${groupIndex}-${logIndex}`}
            ref={(el) => {
              if (el) logLineRefs.current.set(actualIndex, el);
            }}
          >
            <LogLine
              log={log}
              index={actualIndex}
              showLineNumbers={showLineNumbers}
              wordWrap={wordWrap}
              timestampFormat={timestampFormat}
              filterSearch={filterSearch}
              isError={logIsError}
              isWarning={logIsWarning}
              isBuildLog={isBuildLog}
              isHighlighted={actualIndex === errorIndices[currentErrorIndex]}
              onCopy={onCopyLog}
              isStackTrace={isStackTrace}
            />
          </div>
        );
      })}
      {group.logs.length > 1 && (
        <button
          onClick={() => onToggleGroup(groupIndex)}
          className="text-gray-500 text-[10px] ml-4 italic hover:text-gray-400 transition-colors"
        >
          {isGroupCollapsed
            ? `... ${group.logs.length - 1} more similar log(s) (click to expand)`
            : `... ${group.logs.length} similar log(s) grouped (click to collapse)`}
        </button>
      )}
    </div>
  );
}

