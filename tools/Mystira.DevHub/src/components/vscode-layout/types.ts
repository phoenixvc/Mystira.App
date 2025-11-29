export interface ActivityBarItem {
  id: string;
  icon: string;
  title: string;
  badge?: number | string;
}

export interface PanelConfig {
  id: string;
  title: string;
  icon?: string;
  content: React.ReactNode;
  closable?: boolean;
  badge?: number | string;
}

export interface BottomPanelTab {
  id: string;
  title: string;
  icon?: string;
  content: React.ReactNode;
  badge?: number | string;
}

export interface LayoutState {
  primarySidebarWidth: number;
  primarySidebarCollapsed: boolean;
  secondarySidebarWidth: number;
  secondarySidebarCollapsed: boolean;
  bottomPanelHeight: number;
  bottomPanelCollapsed: boolean;
  activeBottomTab: string;
}

export interface VSCodeLayoutProps {
  activityBarItems: ActivityBarItem[];
  activeActivityId: string;
  onActivityChange: (id: string) => void;
  primarySidebar?: React.ReactNode;
  primarySidebarTitle?: string;
  children: React.ReactNode;
  secondarySidebar?: React.ReactNode;
  secondarySidebarTitle?: string;
  bottomPanelTabs?: BottomPanelTab[];
  defaultBottomTab?: string;
  statusBarLeft?: React.ReactNode;
  statusBarRight?: React.ReactNode;
  onLayoutChange?: (layout: LayoutState) => void;
  storageKey?: string;
}

