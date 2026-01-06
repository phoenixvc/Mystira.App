import { useState } from 'react';

export function useViewManagement() {
  const [viewMode, setViewMode] = useState<Record<string, 'logs' | 'webview' | 'split'>>({});
  const [maximizedService, setMaximizedService] = useState<string | null>(null);
  const [webviewErrors, setWebviewErrors] = useState<Record<string, boolean>>({});
  const [showLogs, setShowLogs] = useState<Record<string, boolean>>({});

  const setViewModeForService = (serviceName: string, mode: 'logs' | 'webview' | 'split') => {
    setViewMode(prev => ({ ...prev, [serviceName]: mode }));
    if (mode === 'logs') {
      setShowLogs(prev => ({ ...prev, [serviceName]: true }));
    }
  };

  const toggleMaximize = (serviceName: string) => {
    setMaximizedService(prev => prev === serviceName ? null : serviceName);
  };

  return {
    viewMode,
    maximizedService,
    webviewErrors,
    showLogs,
    setViewModeForService,
    toggleMaximize,
    setWebviewErrors,
    setShowLogs,
  };
}

