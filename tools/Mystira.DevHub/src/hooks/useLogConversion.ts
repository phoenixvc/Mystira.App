import { useMemo } from 'react';
import type { LogEntry, LogFilter } from '../components/services/types';
import { LOG_SEVERITY, LOG_SOURCES, LOG_SOURCE_NAMES, LOG_TYPES } from '../types/constants';

interface GlobalLog {
  timestamp: Date;
  message: string;
  type: 'info' | 'error' | 'warn';
}

interface Problem {
  id: string;
  timestamp: Date;
  severity: 'error' | 'warning' | 'info';
  message: string;
  source?: string;
  details?: string;
}

export function useLogConversion(
  globalLogs: GlobalLog[],
  deploymentLogs: string | null,
  problems: Problem[],
  logFilter: LogFilter
) {
  // Convert logs and problems to LogEntry format for LogsViewer
  const allLogs = useMemo<LogEntry[]>(() => {
    const logs: LogEntry[] = [];
    
    // Add deployment logs
    if (deploymentLogs) {
      const lines = deploymentLogs.split('\n');
      lines.forEach((line, index) => {
        if (line.trim()) {
          const isError = line.toLowerCase().includes('error') || line.toLowerCase().includes('failed');
          logs.push({
            service: LOG_SOURCE_NAMES.INFRASTRUCTURE,
            type: isError ? LOG_TYPES.STDERR : LOG_TYPES.STDOUT,
            source: LOG_SOURCES.RUN,
            message: line,
            timestamp: Date.now() - (lines.length - index) * 1000, // Stagger timestamps slightly
          });
        }
      });
    }
    
    // Add global logs
    globalLogs.forEach((log) => {
      logs.push({
        service: LOG_SOURCE_NAMES.SYSTEM,
        type: log.type === 'error' ? LOG_TYPES.STDERR : LOG_TYPES.STDOUT,
        source: LOG_SOURCES.RUN,
        message: log.message,
        timestamp: log.timestamp.getTime(),
      });
    });
    
    // Add problems as logs (errors/warnings)
    problems.forEach((problem) => {
      logs.push({
        service: problem.source || LOG_SOURCE_NAMES.INFRASTRUCTURE,
        type: problem.severity === 'error' ? LOG_TYPES.STDERR : LOG_TYPES.STDOUT,
        source: LOG_SOURCES.RUN,
        message: `[${problem.severity.toUpperCase()}] ${problem.message}${problem.details ? '\n' + problem.details : ''}`,
        timestamp: problem.timestamp.getTime(),
      });
    });
    
    return logs;
  }, [globalLogs, deploymentLogs, problems]);

  // Filter logs based on current filter
  const filteredLogs = useMemo<LogEntry[]>(() => {
    let filtered = allLogs;

    // Filter by search
    if (logFilter.search) {
      const searchLower = logFilter.search.toLowerCase();
      filtered = filtered.filter(log => 
        log.message.toLowerCase().includes(searchLower) ||
        log.service.toLowerCase().includes(searchLower)
      );
    }

    // Filter by type
    if (logFilter.type !== LOG_TYPES.ALL) {
      filtered = filtered.filter(log => log.type === logFilter.type);
    }

    // Filter by source
    if (logFilter.source && logFilter.source !== LOG_SOURCES.ALL) {
      filtered = filtered.filter(log => log.source === logFilter.source);
    }

    // Filter by severity
    if (logFilter.severity && logFilter.severity !== LOG_SEVERITY.ALL) {
      filtered = filtered.filter(log => {
        const msgLower = log.message.toLowerCase();
        if (logFilter.severity === LOG_SEVERITY.ERRORS) {
          return log.type === LOG_TYPES.STDERR || msgLower.includes('error') || msgLower.includes('failed');
        } else if (logFilter.severity === LOG_SEVERITY.WARNINGS) {
          return msgLower.includes('warning') || msgLower.includes('warn');
        }
        return true;
      });
    }

    return filtered;
  }, [allLogs, logFilter]);

  return { allLogs, filteredLogs };
}

