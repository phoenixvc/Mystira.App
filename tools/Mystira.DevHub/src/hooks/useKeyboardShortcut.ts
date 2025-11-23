import { useEffect } from 'react';

interface KeyboardShortcutOptions {
  key: string;
  ctrlKey?: boolean;
  shiftKey?: boolean;
  altKey?: boolean;
  metaKey?: boolean;
  enabled?: boolean;
}

/**
 * Hook for registering keyboard shortcuts
 * @param callback Function to call when shortcut is triggered
 * @param options Keyboard shortcut configuration
 */
export function useKeyboardShortcut(
  callback: () => void,
  options: KeyboardShortcutOptions
): void {
  const {
    key,
    ctrlKey = false,
    shiftKey = false,
    altKey = false,
    metaKey = false,
    enabled = true,
  } = options;

  useEffect(() => {
    if (!enabled) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      const matchesKey = event.key.toLowerCase() === key.toLowerCase();
      const matchesCtrl = ctrlKey === event.ctrlKey;
      const matchesShift = shiftKey === event.shiftKey;
      const matchesAlt = altKey === event.altKey;
      const matchesMeta = metaKey === event.metaKey;

      if (matchesKey && matchesCtrl && matchesShift && matchesAlt && matchesMeta) {
        event.preventDefault();
        callback();
      }
    };

    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [callback, key, ctrlKey, shiftKey, altKey, metaKey, enabled]);
}
