import { useState } from 'react';
import type { Toast, FeedbackType } from '../types';

export function useToast() {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = (
    message: string,
    type: FeedbackType = 'info',
    options?: { duration?: number; action?: Toast['action'] }
  ): Toast => {
    const toast: Toast = {
      id: `toast-${Date.now()}-${Math.random().toString(36).slice(2)}`,
      message,
      type,
      duration: options?.duration ?? 5000,
      action: options?.action,
    };
    setToasts((prev) => [...prev, toast]);
    return toast;
  };

  const dismissToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  const clearToasts = () => {
    setToasts([]);
  };

  return {
    toasts,
    showToast,
    dismissToast,
    clearToasts,
    success: (msg: string, opts?: { duration?: number }) => showToast(msg, 'success', opts),
    error: (msg: string, opts?: { duration?: number }) => showToast(msg, 'error', opts),
    warning: (msg: string, opts?: { duration?: number }) => showToast(msg, 'warning', opts),
    info: (msg: string, opts?: { duration?: number }) => showToast(msg, 'info', opts),
  };
}

