import { useMemo, useState } from 'react';
import { ServiceLog } from '../types';

interface GroupedLog {
  logs: ServiceLog[];
  lineNumber: number;
}

export function useLogGrouping(filteredLogs: ServiceLog[], collapseSimilar: boolean) {
  const [collapsedGroups, setCollapsedGroups] = useState<Set<number>>(new Set());

  const groupedLogs = useMemo(() => {
    if (!collapseSimilar) {
      return filteredLogs.map((log, index) => ({
        logs: [log],
        lineNumber: index + 1,
      }));
    }

    const groups: GroupedLog[] = [];
    let currentGroup: ServiceLog[] = [];
    let lineNumber = 1;

    filteredLogs.forEach((log) => {
      if (currentGroup.length === 0) {
        currentGroup = [log];
      } else {
        const lastLog = currentGroup[currentGroup.length - 1];
        const isSimilar = 
          log.message.substring(0, 50) === lastLog.message.substring(0, 50) &&
          Math.abs(log.timestamp - lastLog.timestamp) < 2000;
        
        if (isSimilar) {
          currentGroup.push(log);
        } else {
          groups.push({ logs: currentGroup, lineNumber });
          lineNumber += currentGroup.length;
          currentGroup = [log];
        }
      }
    });

    if (currentGroup.length > 0) {
      groups.push({ logs: currentGroup, lineNumber });
    }

    return groups;
  }, [filteredLogs, collapseSimilar, collapsedGroups]);

  const toggleGroup = (groupIndex: number) => {
    setCollapsedGroups(prev => {
      const next = new Set(prev);
      if (next.has(groupIndex)) {
        next.delete(groupIndex);
      } else {
        next.add(groupIndex);
      }
      return next;
    });
  };

  return { groupedLogs, collapsedGroups, toggleGroup };
}

