import type { LayoutState } from './types';

export const DEFAULT_LAYOUT: LayoutState = {
  primarySidebarWidth: 280,
  primarySidebarCollapsed: false,
  secondarySidebarWidth: 320,
  secondarySidebarCollapsed: true,
  bottomPanelHeight: 250,
  bottomPanelCollapsed: true,
  activeBottomTab: '',
};

export const MIN_SIZES = {
  primarySidebar: 200,
  secondarySidebar: 200,
  bottomPanel: 100,
};

export const MAX_SIZES = {
  primarySidebar: 500,
  secondarySidebar: 600,
  bottomPanel: 500,
};

export const ACTIVITY_BAR_WIDTH = 48;

