import { useEffect, useState } from 'react';
import { open } from '@tauri-apps/api/shell';

interface WebViewPanelProps {
  url: string;
  title: string;
  onClose?: () => void;
  embedded?: boolean;
}

function WebViewPanel({ url, title, onClose, embedded = false }: WebViewPanelProps) {
  const [useEmbedded, setUseEmbedded] = useState(embedded);

  useEffect(() => {
    if (!useEmbedded) {
      // Open in external browser for now
      // TODO: In future, use Tauri's window API to create embedded webview windows
      open(url).catch(console.error);
      if (onClose) {
        onClose();
      }
    }
  }, [url, useEmbedded, onClose]);

  if (!useEmbedded) {
    return null;
  }

  // Render an iframe for embedded webview
  return (
    <div className="w-full h-full border-t">
      <div className="flex items-center justify-between p-2 bg-gray-100 border-b">
        <span className="font-semibold">{title}</span>
        {onClose && (
          <button
            onClick={onClose}
            className="px-2 py-1 bg-red-500 text-white rounded text-sm hover:bg-red-600"
          >
            Close
          </button>
        )}
      </div>
      <iframe
        src={url}
        title={title}
        className="w-full h-full border-0"
        sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals"
      />
    </div>
  );
}

export default WebViewPanel;

