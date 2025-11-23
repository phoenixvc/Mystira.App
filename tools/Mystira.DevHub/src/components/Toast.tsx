import { useEffect } from 'react';

export interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
}

interface ToastProps {
  toast: Toast;
  onClose: (id: string) => void;
}

function ToastComponent({ toast, onClose }: ToastProps) {
  useEffect(() => {
    if (toast.duration !== 0) {
      const timer = setTimeout(() => {
        onClose(toast.id);
      }, toast.duration || 5000);
      return () => clearTimeout(timer);
    }
  }, [toast, onClose]);

  const bgColor = {
    success: 'bg-green-500',
    error: 'bg-red-500',
    warning: 'bg-yellow-500',
    info: 'bg-blue-500',
  }[toast.type];

  const icon = {
    success: '✓',
    error: '✗',
    warning: '⚠',
    info: 'ℹ',
  }[toast.type];

  return (
    <div
      className={`${bgColor} text-white px-4 py-3 rounded-lg shadow-lg mb-2 flex items-center justify-between min-w-[300px] max-w-[500px] animate-slide-in`}
      role="alert"
    >
      <div className="flex items-center gap-2">
        <span className="text-xl">{icon}</span>
        <span>{toast.message}</span>
      </div>
      <button
        onClick={() => onClose(toast.id)}
        className="ml-4 text-white hover:text-gray-200 font-bold"
        aria-label="Close"
      >
        ×
      </button>
    </div>
  );
}

interface ToastContainerProps {
  toasts: Toast[];
  onClose: (id: string) => void;
}

export function ToastContainer({ toasts, onClose }: ToastContainerProps) {
  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col-reverse">
      {toasts.map((toast) => (
        <ToastComponent key={toast.id} toast={toast} onClose={onClose} />
      ))}
    </div>
  );
}

export function useToast() {
  const showToast = (
    message: string,
    type: Toast['type'] = 'info',
    duration: number = 5000
  ): Toast => {
    return {
      id: `toast-${Date.now()}-${Math.random()}`,
      message,
      type,
      duration,
    };
  };

  return { showToast };
}

