import { useRef } from 'react';
import { ServiceLog } from '../types';
import { isErrorMessage, isStackTraceLine } from './logUtils';

interface LogDisplayProps {
  logs: ServiceLog[];
  filter: { search: string };
  showLineNumbers: boolean;
  isMaximized: boolean;
  logLineRefs: React.MutableRefObject<Map<number, HTMLDivElement>>;
  highlightErrorIndex?: number;
  onCopyLog: (log: ServiceLog) => void;
  formatTimestamp: (timestamp: number) => string;
  highlightSearch: (text: string, search: string) => JSX.Element;
}

export function LogDisplay({
  logs,
  filter,
  showLineNumbers,
  isMaximized,
  logLineRefs,
  highlightErrorIndex,
  onCopyLog,
  formatTimestamp,
  highlightSearch,
}: LogDisplayProps) {
  const logEndRef = useRef<HTMLDivElement>(null);

  return (
    <div className={`bg-black text-green-400 font-mono text-xs overflow-y-auto flex-1 ${isMaximized ? 'h-full' : ''}`}>
      {logs.length === 0 ? (
        <div className="text-gray-500">No logs to display</div>
      ) : (
        <>
          {logs.map((log, index) => {
            const isBuildLog = log.source === 'build';
            const messageLower = log.message.toLowerCase();
            
            // Check if this is a stack trace continuation line
            const isStackTrace = isStackTraceLine(log.message);
            
            // Check if previous line was an error (for stack trace detection)
            const prevLog = index > 0 ? logs[index - 1] : null;
            const prevWasError = prevLog && (
              prevLog.type === 'stderr' || 
              isErrorMessage(prevLog.message) ||
              isStackTraceLine(prevLog.message)
            );
            
            // Stack trace lines should be treated as error lines (red, no timestamp)
            const isErrorMsg = log.type === 'stderr' || 
              isErrorMessage(log.message) ||
              (isStackTrace && prevWasError);
            
            const isWarning = !isErrorMsg && (
              messageLower.includes('warning') || 
              messageLower.includes('warn') || 
              messageLower.includes('deprecated')
            );
            
            let textColor = 'text-green-400';
            if (isErrorMsg) {
              textColor = 'text-red-400';
            } else if (isWarning) {
              textColor = 'text-yellow-400';
            }
            
            return (
              <div
                key={index}
                ref={(el) => {
                  if (el) logLineRefs.current.set(index, el);
                }}
                onClick={() => onCopyLog(log)}
                className={`${textColor} ${isBuildLog ? 'opacity-90' : ''} hover:bg-gray-900/50 px-1 py-0.5 rounded transition-colors cursor-pointer ${
                  index === highlightErrorIndex ? 'ring-2 ring-red-500' : ''
                }`}
                title={`Click to copy | Line ${index + 1} - ${isErrorMsg ? 'Error' : isWarning ? 'Warning' : 'Info'}`}
              >
                {showLineNumbers && (
                  <span className="text-gray-600 dark:text-gray-500 mr-2 text-[10px]">
                    {index + 1}
                  </span>
                )}
                {/* Don't show timestamp for stack trace continuation lines */}
                {!isStackTrace && (
                  <span className="text-gray-500 text-[10px]">
                    [{formatTimestamp(log.timestamp)}]
                  </span>
                )}
                {isStackTrace && (
                  <span className="text-gray-700 text-[10px] mr-1">   </span>
                )}
                {isBuildLog && !isStackTrace && (
                  <span className="text-cyan-400 font-semibold ml-1 text-[10px]">
                    [BUILD]
                  </span>
                )}
                {!isStackTrace && (
                  <span className="text-gray-500 ml-1 text-[10px]">
                    [{log.service}]
                  </span>
                )}
                {isErrorMsg && !isStackTrace && (
                  <span className="text-red-500 ml-1 font-bold">⚠</span>
                )}
                {isWarning && !isErrorMsg && !isStackTrace && (
                  <span className="text-yellow-500 ml-1">⚠</span>
                )}
                {/* Indent stack trace lines slightly */}
                <span className={`ml-1 ${isStackTrace ? 'ml-6' : ''}`}>
                  {highlightSearch(log.message, filter.search)}
                </span>
              </div>
            );
          })}
          <div ref={logEndRef} />
        </>
      )}
    </div>
  );
}

