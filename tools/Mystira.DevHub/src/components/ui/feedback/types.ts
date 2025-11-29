export type FeedbackType = 'success' | 'error' | 'warning' | 'info';
export type BadgeVariant = 'default' | 'success' | 'error' | 'warning' | 'info' | 'outline';
export type BadgeSize = 'xs' | 'sm' | 'md';

export interface Toast {
  id: string;
  message: string;
  type: FeedbackType;
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

