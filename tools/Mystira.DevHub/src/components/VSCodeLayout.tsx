import { useRef } from 'react';
import { ActivityBar } from './vscode-layout/components/ActivityBar';
import { Sidebar } from './vscode-layout/components/Sidebar';
import { BottomPanel } from './vscode-layout/components/BottomPanel';
import { useVSCodeLayout } from './vscode-layout/hooks/useVSCodeLayout';
import { ACTIVITY_BAR_WIDTH } from './vscode-layout/constants';
import type { VSCodeLayoutProps } from './vscode-layout/types';

export type { ActivityBarItem, PanelConfig, BottomPanelTab, LayoutState, VSCodeLayoutProps } from './vscode-layout/types';
export { SidebarPanel, TreeItem, OutputPanel } from './vscode-layout/components';

export function VSCodeLayout({
  activityBarItems,
  activeActivityId,
  onActivityChange,
  primarySidebar,
  primarySidebarTitle,
  children,
  secondarySidebar,
  secondarySidebarTitle,
  bottomPanelTabs = [],
  defaultBottomTab,
  statusBarLeft,
  statusBarRight,
  onLayoutChange,
  storageKey = 'vscodeLayout',
}: VSCodeLayoutProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  const {
    layout,
    resizing,
    startResize,
    togglePrimarySidebar,
    toggleSecondarySidebar,
    toggleBottomPanel,
    setActiveBottomTab,
  } = useVSCodeLayout({
    defaultBottomTab,
    bottomPanelTabs,
    storageKey,
    onLayoutChange,
  });

  return (
    <div ref={containerRef} className="flex flex-col h-screen bg-gray-900 text-white overflow-hidden">
      <div className="flex flex-1 min-h-0">
        <ActivityBar
          items={activityBarItems}
          activeId={activeActivityId}
          onActivityChange={onActivityChange}
          onPrimarySidebarToggle={togglePrimarySidebar}
          primarySidebarCollapsed={layout.primarySidebarCollapsed}
          secondarySidebar={secondarySidebar}
          onSecondarySidebarToggle={toggleSecondarySidebar}
          secondarySidebarCollapsed={layout.secondarySidebarCollapsed}
          bottomPanelTabs={bottomPanelTabs}
          onBottomPanelToggle={toggleBottomPanel}
          bottomPanelCollapsed={layout.bottomPanelCollapsed}
          activityBarWidth={ACTIVITY_BAR_WIDTH}
        />

        {primarySidebar && (
          <Sidebar
            children={primarySidebar}
            title={primarySidebarTitle}
            collapsed={layout.primarySidebarCollapsed}
            width={layout.primarySidebarWidth}
            onToggle={togglePrimarySidebar}
            onStartResize={(e) => startResize('primary', e)}
            resizing={resizing?.type === 'primary'}
            position="left"
          />
        )}

        <div className="flex-1 flex flex-col min-w-0 min-h-0">
          <div className="flex-1 overflow-auto bg-gray-900">
            {children}
          </div>

          <BottomPanel
            tabs={bottomPanelTabs}
            activeTab={layout.activeBottomTab}
            collapsed={layout.bottomPanelCollapsed}
            height={layout.bottomPanelHeight}
            onTabChange={setActiveBottomTab}
            onToggle={toggleBottomPanel}
            onStartResize={(e) => startResize('bottom', e)}
            resizing={resizing?.type === 'bottom'}
          />
        </div>

        {secondarySidebar && (
          <Sidebar
            children={secondarySidebar}
            title={secondarySidebarTitle}
            collapsed={layout.secondarySidebarCollapsed}
            width={layout.secondarySidebarWidth}
            onToggle={toggleSecondarySidebar}
            onStartResize={(e) => startResize('secondary', e)}
            resizing={resizing?.type === 'secondary'}
            position="right"
          />
        )}
      </div>

      <div className="flex items-center justify-between px-3 py-1 bg-blue-600 text-white text-xs flex-shrink-0">
        <div className="flex items-center gap-3">
          {statusBarLeft}
        </div>
        <div className="flex items-center gap-3">
          {statusBarRight}
        </div>
      </div>
    </div>
  );
}

export default VSCodeLayout;
