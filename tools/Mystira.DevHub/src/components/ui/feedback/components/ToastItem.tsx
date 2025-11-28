import { useEffect } from 'react';
import type { Toast, FeedbackType } from '../types';

interface ToastItemProps {
  toast: Toast;
  onClose: (id: string) => void;
}

const toastStyles: Record<FeedbackType, string> = {
  success: 'bg-green-600 dark:bg-green-500',
  error: 'bg-red-600 dark:bg-red-500',
  warning: 'bg-yellow-500 dark:bg-yellow-400 text-black',
  info: 'bg-blue-600 dark:bg-blue-500',
};

const toastIcons: Record<FeedbackType, string> = {
  success: '✓',
  error: '✕',
  warning: '⚠',
  info: 'ℹ',
};

export function ToastItem({ toast, onClose }: ToastItemProps) {
  useEffect(() => {
    if (toast.duration !== 0) {
      const timer = setTimeout(() => {
        onClose(toast.id);
      }, toast.duration || 5000);
      return () => clearTimeout(timer);
    }
  }, [toast, onClose]);

  return (
    <div
      className={`
        ${toastStyles[toast.type]}
        text-white px-4 py-3 rounded-lg shadow-lg mb-2
        flex items-center justify-between
        min-w-[300px] max-w-[500px]
        animate-slide-in
      `.trim().replace(/\s+/g, ' ')}
      role="alert"
    >
      <div className="flex items-center gap-2">
        <span className="text-lg">{toastIcons[toast.type]}</span>
        <span className="text-sm">{toast.message}</span>
      </div>
      <div className="flex items-center gap-2 ml-4">
        {toast.action && (
          <button
            onClick={toast.action.onClick}
            className="text-sm font-medium underline hover:no-underline"
          >
            {toast.action.label}
          </button>
        )}
        <button
          onClick={() => onClose(toast.id)}
          className="text-white hover:text-gray-200 font-bold text-lg leading-none"
          aria-label="Close"
        >
          ×
        </button>
      </div>
    </div>
  );
}

