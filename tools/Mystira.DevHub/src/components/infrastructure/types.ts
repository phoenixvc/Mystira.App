export type ActionType = 'validate' | 'preview' | 'deploy' | 'destroy';

export type ButtonVariant = 'primary' | 'warning' | 'success' | 'danger';

export interface ActionButton {
  id: ActionType | string;
  icon: string;
  label: string;
  description: string;
  onClick: () => void;
  disabled: boolean;
  loading: boolean;
  variant: ButtonVariant;
}

export interface ActionButtonsProps {
  buttons: ActionButton[];
  loading: boolean;
}
