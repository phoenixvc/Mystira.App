import { ToastItem } from './ToastItem';
import type { Toast } from '../types';

export interface ToastContainerProps {
  toasts: Toast[];
  onClose: (id: string) => void;
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';
}

const positionStyles = {
  'top-right': 'top-4 right-4',
  'top-left': 'top-4 left-4',
  'bottom-right': 'bottom-4 right-4',
  'bottom-left': 'bottom-4 left-4',
};

export function ToastContainer({ toasts, onClose, position = 'top-right' }: ToastContainerProps) {
  const isTop = position.startsWith('top');

  return (
    <div className={`fixed ${positionStyles[position]} z-50 flex ${isTop ? 'flex-col-reverse' : 'flex-col'}`}>
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} onClose={onClose} />
      ))}
    </div>
  );
}

