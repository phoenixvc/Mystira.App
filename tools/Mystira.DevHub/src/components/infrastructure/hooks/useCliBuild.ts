import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useRef, useState } from 'react';
import type { CommandResponse } from '../../../types';

export function useCliBuild() {
  const [isBuilding, setIsBuilding] = useState(false);
  const [buildTime, setBuildTime] = useState<number | null>(null);
  const [logs, setLogs] = useState<string[]>([]);
  const [showLogs, setShowLogs] = useState(false);
  const logsEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const fetchBuildTime = async () => {
      try {
        const time = await invoke<number | null>('get_cli_build_time');
        setBuildTime(time);
      } catch (error) {
        console.error('Failed to get CLI build time:', error);
        setBuildTime(null);
      }
    };
    fetchBuildTime();
  }, [isBuilding]);

  useEffect(() => {
    if (logsEndRef.current && (isBuilding || logs.length > 0)) {
      logsEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [logs, isBuilding]);

  const build = async () => {
    setIsBuilding(true);
    setShowLogs(true);
    setLogs([]);
    try {
      const response = await invoke<CommandResponse>('build_cli');
      if (response.result && typeof response.result === 'object' && 'output' in response.result) {
        const output = (response.result as any).output as string;
        const lines = output.split('\n').filter(line => line.trim().length > 0);
        setLogs(lines);
      }
      if (response.success) {
        if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
          const time = (response.result as any).buildTime as number | null;
          if (time) {
            setBuildTime(time);
          } else {
            await fetchBuildTimeWithRetry();
          }
        } else {
          await fetchBuildTimeWithRetry();
        }
      }
    } catch (error) {
      setLogs([`Error: ${error}`]);
      console.error('Failed to build CLI:', error);
    } finally {
      setIsBuilding(false);
    }
  };

  const fetchBuildTimeWithRetry = async (retries = 3) => {
    for (let i = 0; i < retries; i++) {
      await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
      try {
        const time = await invoke<number | null>('get_cli_build_time');
        if (time) {
          setBuildTime(time);
          return;
        }
      } catch (error) {
        console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
      }
    }
  };

  return {
    isBuilding,
    buildTime,
    logs,
    showLogs,
    setShowLogs,
    logsEndRef,
    build,
  };
}

