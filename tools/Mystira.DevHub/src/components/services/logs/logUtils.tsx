import { ServiceLog } from '../types';

export function formatTimestamp(timestamp: number, format: 'time' | 'full' | 'relative'): string {
  const date = new Date(timestamp);
  switch (format) {
    case 'full':
      return date.toLocaleString();
    case 'relative':
      const seconds = Math.floor((Date.now() - timestamp) / 1000);
      if (seconds < 60) return `${seconds}s ago`;
      const minutes = Math.floor(seconds / 60);
      if (minutes < 60) return `${minutes}m ago`;
      const hours = Math.floor(minutes / 60);
      if (hours < 24) return `${hours}h ago`;
      return `${Math.floor(hours / 24)}d ago`;
    default:
      return date.toLocaleTimeString();
  }
}

export function highlightSearch(text: string, search: string): JSX.Element {
  if (!search) {
    return <>{text}</>;
  }

  const parts = text.split(new RegExp(`(${search.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi'));
  return (
    <>
      {parts.map((part, i) => 
        part.toLowerCase() === search.toLowerCase() ? (
          <mark key={i} className="bg-yellow-400 text-black dark:bg-yellow-600 dark:text-white">
            {part}
          </mark>
        ) : (
          <span key={i}>{part}</span>
        )
      )}
    </>
  );
}

export function findErrorIndices(logs: ServiceLog[]): number[] {
  return logs
    .map((log, index) => {
      const msg = log.message.toLowerCase();
      const isError = log.type === 'stderr' || 
        msg.includes('error') || 
        msg.includes('failed') || 
        msg.includes('exception') || 
        msg.includes('fatal');
      return isError ? index : -1;
    })
    .filter(index => index !== -1);
}

export async function exportLogs(
  serviceName: string,
  logs: ServiceLog[],
  formatTimestamp: (timestamp: number) => string
): Promise<void> {
  const { save } = await import('@tauri-apps/api/dialog');
  const { writeTextFile } = await import('@tauri-apps/api/fs');

  const filePath = await save({
    defaultPath: `${serviceName}-logs-${new Date().toISOString().replace(/[:.]/g, '-')}.txt`,
    filters: [
      { name: 'Text', extensions: ['txt'] },
      { name: 'Log', extensions: ['log'] },
    ],
  });

  if (filePath) {
    const logContent = logs
      .map((log) => {
        const timestamp = formatTimestamp(log.timestamp);
        const source = log.source === 'build' ? '[BUILD]' : '';
        const type = log.type === 'stderr' ? 'ERROR' : 'INFO';
        return `[${timestamp}] ${source} [${log.service}] [${type}] ${log.message}`;
      })
      .join('\n');

    await writeTextFile(filePath, logContent);
    alert(`Logs exported successfully to:\n${filePath}`);
  }
}

