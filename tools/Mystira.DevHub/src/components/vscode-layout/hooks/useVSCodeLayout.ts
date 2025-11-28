import { useCallback, useEffect, useState } from 'react';
import { DEFAULT_LAYOUT, MIN_SIZES, MAX_SIZES } from '../constants';
import type { LayoutState, BottomPanelTab } from '../types';

interface UseVSCodeLayoutProps {
  defaultBottomTab?: string;
  bottomPanelTabs?: BottomPanelTab[];
  storageKey?: string;
  onLayoutChange?: (layout: LayoutState) => void;
}

export function useVSCodeLayout({
  defaultBottomTab,
  bottomPanelTabs = [],
  storageKey = 'vscodeLayout',
  onLayoutChange,
}: UseVSCodeLayoutProps) {
  const [layout, setLayout] = useState<LayoutState>(() => {
    const saved = localStorage.getItem(storageKey);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        return { ...DEFAULT_LAYOUT, ...parsed };
      } catch {
        // Fall through to default
      }
    }
    return { ...DEFAULT_LAYOUT, activeBottomTab: defaultBottomTab || bottomPanelTabs[0]?.id || '' };
  });

  const [resizing, setResizing] = useState<{
    type: 'primary' | 'secondary' | 'bottom';
    startPos: number;
    startSize: number;
  } | null>(null);

  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(layout));
    onLayoutChange?.(layout);
  }, [layout, storageKey, onLayoutChange]);

  useEffect(() => {
    if (!resizing) return;

    const handleMouseMove = (e: MouseEvent) => {
      const { type, startPos, startSize } = resizing;

      if (type === 'primary') {
        const delta = e.clientX - startPos;
        const newWidth = Math.max(MIN_SIZES.primarySidebar, Math.min(MAX_SIZES.primarySidebar, startSize + delta));
        setLayout(prev => ({ ...prev, primarySidebarWidth: newWidth }));
      } else if (type === 'secondary') {
        const delta = startPos - e.clientX;
        const newWidth = Math.max(MIN_SIZES.secondarySidebar, Math.min(MAX_SIZES.secondarySidebar, startSize + delta));
        setLayout(prev => ({ ...prev, secondarySidebarWidth: newWidth }));
      } else if (type === 'bottom') {
        const delta = startPos - e.clientY;
        const newHeight = Math.max(MIN_SIZES.bottomPanel, Math.min(MAX_SIZES.bottomPanel, startSize + delta));
        setLayout(prev => ({ ...prev, bottomPanelHeight: newHeight }));
      }
    };

    const handleMouseUp = () => {
      setResizing(null);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [resizing]);

  const startResize = useCallback((type: 'primary' | 'secondary' | 'bottom', e: React.MouseEvent) => {
    e.preventDefault();
    const startPos = type === 'bottom' ? e.clientY : e.clientX;
    const startSize = type === 'primary'
      ? layout.primarySidebarWidth
      : type === 'secondary'
        ? layout.secondarySidebarWidth
        : layout.bottomPanelHeight;
    setResizing({ type, startPos, startSize });
  }, [layout]);

  const togglePrimarySidebar = useCallback(() => {
    setLayout(prev => ({ ...prev, primarySidebarCollapsed: !prev.primarySidebarCollapsed }));
  }, []);

  const toggleSecondarySidebar = useCallback(() => {
    setLayout(prev => ({ ...prev, secondarySidebarCollapsed: !prev.secondarySidebarCollapsed }));
  }, []);

  const toggleBottomPanel = useCallback(() => {
    setLayout(prev => ({ ...prev, bottomPanelCollapsed: !prev.bottomPanelCollapsed }));
  }, []);

  const setActiveBottomTab = useCallback((tabId: string) => {
    setLayout(prev => ({
      ...prev,
      activeBottomTab: tabId,
      bottomPanelCollapsed: false,
    }));
  }, []);

  return {
    layout,
    resizing,
    startResize,
    togglePrimarySidebar,
    toggleSecondarySidebar,
    toggleBottomPanel,
    setActiveBottomTab,
  };
}

