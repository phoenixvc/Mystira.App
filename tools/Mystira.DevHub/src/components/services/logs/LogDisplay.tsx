import { useRef } from 'react';
import { ServiceLog } from '../types';

interface LogDisplayProps {
  logs: ServiceLog[];
  filter: { search: string };
  showLineNumbers: boolean;
  timestampFormat: 'time' | 'full' | 'relative';
  isAutoScroll: boolean;
  isMaximized: boolean;
  containerClass: string;
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
  timestampFormat,
  isAutoScroll,
  isMaximized,
  containerClass,
  logLineRefs,
  highlightErrorIndex,
  onCopyLog,
  formatTimestamp,
  highlightSearch,
}: LogDisplayProps) {
  const logEndRef = useRef<HTMLDivElement>(null);

  return (
    <div className={`bg-black text-green-400 font-mono text-xs p-4 overflow-y-auto flex-1 ${isMaximized ? 'h-full' : ''}`}>
      {logs.length === 0 ? (
        <div className="text-gray-500">No logs to display</div>
      ) : (
        <>
          {logs.map((log, index) => {
            const isBuildLog = log.source === 'build';
            const messageLower = log.message.toLowerCase();
            const isWarning = messageLower.includes('warning') || messageLower.includes('warn') || messageLower.includes('deprecated');
            const isErrorMsg = log.type === 'stderr' || 
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
                <span className="text-gray-500 text-[10px]">
                  [{formatTimestamp(log.timestamp)}]
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
          <div ref={logEndRef} />
        </>
      )}
    </div>
  );
}

