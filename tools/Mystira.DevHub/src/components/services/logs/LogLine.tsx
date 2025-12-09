import { ServiceLog } from '../types';
import { formatTimestamp, highlightSearch } from './logUtils';

interface LogLineProps {
  log: ServiceLog;
  index: number;
  showLineNumbers: boolean;
  wordWrap: boolean;
  timestampFormat: 'time' | 'full' | 'relative';
  filterSearch: string;
  isError: boolean;
  isWarning: boolean;
  isBuildLog: boolean;
  isHighlighted: boolean;
  onCopy: (log: ServiceLog) => void;
  isStackTrace?: boolean;
}

export function LogLine({
  log,
  index,
  showLineNumbers,
  wordWrap,
  timestampFormat,
  filterSearch,
  isError,
  isWarning,
  isBuildLog,
  isHighlighted,
  onCopy,
  isStackTrace = false,
}: LogLineProps) {
  let textColor = 'text-green-400';
  if (isError) {
    textColor = 'text-red-400';
  } else if (isWarning) {
    textColor = 'text-yellow-400';
  }

  return (
    <div
      onClick={() => onCopy(log)}
      className={`${textColor} ${isBuildLog ? 'opacity-90' : ''} hover:bg-gray-900/50 px-1 py-0.5 rounded transition-colors cursor-pointer ${
        isHighlighted ? 'ring-2 ring-red-500' : ''
      } ${wordWrap ? 'break-words whitespace-pre-wrap' : ''}`}
      title={`Click to copy | Line ${index + 1} - ${isError ? 'Error' : isWarning ? 'Warning' : 'Info'}`}
    >
      {showLineNumbers && (
        <span className="text-gray-600 dark:text-gray-500 mr-2 text-[10px] flex-shrink-0">{index + 1}</span>
      )}
      {/* Don't show timestamp for stack trace continuation lines */}
      {!isStackTrace && (
        <span className="text-gray-500 text-[10px] flex-shrink-0">[{formatTimestamp(log.timestamp, timestampFormat)}]</span>
      )}
      {isStackTrace && (
        <span className="text-gray-700 text-[10px] flex-shrink-0 mr-1">   </span>
      )}
      {isBuildLog && !isStackTrace && (
        <span className="text-cyan-400 font-semibold ml-1 text-[10px] flex-shrink-0">[BUILD]</span>
      )}
      {!isStackTrace && (
        <span className="text-gray-500 ml-1 text-[10px] flex-shrink-0">[{log.service}]</span>
      )}
      {isError && !isStackTrace && <span className="text-red-500 ml-1 font-bold flex-shrink-0">⚠</span>}
      {isWarning && !isError && !isStackTrace && <span className="text-yellow-500 ml-1 flex-shrink-0">⚠</span>}
      {/* Indent stack trace lines slightly */}
      <span className={`${isStackTrace ? 'ml-6' : 'ml-1'} ${wordWrap ? 'break-words' : ''}`}>
        {highlightSearch(log.message, filterSearch)}
      </span>
    </div>
  );
}

