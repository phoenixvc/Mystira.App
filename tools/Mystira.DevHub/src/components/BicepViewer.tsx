import Editor from '@monaco-editor/react';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';

interface BicepFile {
  name: string;
  path: string;
  type: 'file' | 'folder';
  children?: BicepFile[];
}

const BICEP_FILES: BicepFile[] = [
  {
    name: 'infrastructure/dev',
    path: 'infrastructure/dev',
    type: 'folder',
    children: [
      {
        name: 'main.bicep',
        path: 'infrastructure/dev/main.bicep',
        type: 'file',
      },
      {
        name: 'modules',
        path: 'infrastructure/dev/modules',
        type: 'folder',
        children: [
          {
            name: 'cosmos-db.bicep',
            path: 'infrastructure/dev/modules/cosmos-db.bicep',
            type: 'file',
          },
          {
            name: 'storage.bicep',
            path: 'infrastructure/dev/modules/storage.bicep',
            type: 'file',
          },
          {
            name: 'app-service.bicep',
            path: 'infrastructure/dev/modules/app-service.bicep',
            type: 'file',
          },
          {
            name: 'communication-services.bicep',
            path: 'infrastructure/dev/modules/communication-services.bicep',
            type: 'file',
          },
          {
            name: 'log-analytics.bicep',
            path: 'infrastructure/dev/modules/log-analytics.bicep',
            type: 'file',
          },
          {
            name: 'application-insights.bicep',
            path: 'infrastructure/dev/modules/application-insights.bicep',
            type: 'file',
          },
        ],
      },
    ],
  },
];

function BicepViewer() {
  const [selectedFile, setSelectedFile] = useState<string>('infrastructure/dev/main.bicep');
  const [fileContent, setFileContent] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [expandedFolders, setExpandedFolders] = useState<Set<string>>(
    new Set(['infrastructure/dev', 'infrastructure/dev/modules'])
  );

  useEffect(() => {
    loadFile(selectedFile);
  }, [selectedFile]);

  const loadFile = async (filePath: string) => {
    setLoading(true);
    setError(null);
    try {
      const content = await invoke<string>('read_bicep_file', { relativePath: filePath });
      setFileContent(content);
    } catch (err) {
      setError(`Failed to load file: ${err}`);
      setFileContent('');
    } finally {
      setLoading(false);
    }
  };

  const toggleFolder = (folderPath: string) => {
    setExpandedFolders((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(folderPath)) {
        newSet.delete(folderPath);
      } else {
        newSet.add(folderPath);
      }
      return newSet;
    });
  };

  const renderFileTree = (files: BicepFile[], depth: number = 0) => {
    return files.map((file) => (
      <div key={file.path}>
        {file.type === 'folder' ? (
          <div>
            <button
              onClick={() => toggleFolder(file.path)}
              className={`w-full text-left px-2 py-1 hover:bg-gray-100 flex items-center`}
              style={{ paddingLeft: `${depth * 16 + 8}px` }}
            >
              <span className="mr-2 text-gray-500">
                {expandedFolders.has(file.path) ? '📂' : '📁'}
              </span>
              <span className="text-sm font-medium text-gray-700">{file.name}</span>
            </button>
            {expandedFolders.has(file.path) && file.children && (
              <div>{renderFileTree(file.children, depth + 1)}</div>
            )}
          </div>
        ) : (
          <button
            onClick={() => setSelectedFile(file.path)}
            className={`w-full text-left px-2 py-1 hover:bg-gray-100 flex items-center ${
              selectedFile === file.path ? 'bg-blue-50 border-l-2 border-blue-500' : ''
            }`}
            style={{ paddingLeft: `${depth * 16 + 8}px` }}
          >
            <span className="mr-2 text-gray-500">📄</span>
            <span className="text-sm text-gray-700">{file.name}</span>
          </button>
        )}
      </div>
    ));
  };

  return (
    <div className="flex h-[600px] border border-gray-200 rounded-lg overflow-hidden">
      {/* File Tree Sidebar */}
      <div className="w-64 bg-gray-50 border-r border-gray-200 overflow-y-auto">
        <div className="p-3 border-b border-gray-200 bg-white">
          <h3 className="font-semibold text-gray-900 text-sm">Bicep Templates</h3>
        </div>
        <div className="py-2">{renderFileTree(BICEP_FILES)}</div>
      </div>

      {/* Monaco Editor */}
      <div className="flex-1 flex flex-col">
        <div className="px-4 py-2 bg-white border-b border-gray-200 flex items-center justify-between">
          <div className="flex items-center">
            <span className="text-sm font-medium text-gray-700">{selectedFile}</span>
            <span className="ml-3 text-xs px-2 py-1 bg-gray-100 text-gray-600 rounded">
              Read-only
            </span>
          </div>
          <button
            onClick={() => loadFile(selectedFile)}
            className="text-sm px-3 py-1 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition-colors"
            disabled={loading}
          >
            {loading ? 'Refreshing...' : '🔄 Refresh'}
          </button>
        </div>

        <div className="flex-1 relative">
          {error ? (
            <div className="absolute inset-0 flex items-center justify-center bg-red-50">
              <div className="text-center p-6">
                <div className="text-4xl mb-3">⚠️</div>
                <div className="text-red-900 font-medium mb-2">Failed to Load File</div>
                <div className="text-red-700 text-sm">{error}</div>
              </div>
            </div>
          ) : loading ? (
            <div className="absolute inset-0 flex items-center justify-center bg-gray-50">
              <div className="text-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-3"></div>
                <div className="text-gray-600">Loading file...</div>
              </div>
            </div>
          ) : (
            <Editor
              height="100%"
              defaultLanguage="bicep"
              language="bicep"
              value={fileContent}
              theme="vs-light"
              options={{
                readOnly: true,
                minimap: { enabled: true },
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
                fontSize: 13,
                wordWrap: 'on',
                automaticLayout: true,
              }}
            />
          )}
        </div>

        {/* Info Footer */}
        <div className="px-4 py-2 bg-gray-50 border-t border-gray-200">
          <p className="text-xs text-gray-600">
            💡 Tip: Edit Bicep files in your IDE. This viewer is read-only for safety.
          </p>
        </div>
      </div>
    </div>
  );
}

export default BicepViewer;
