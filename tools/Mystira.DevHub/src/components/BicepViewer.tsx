import Editor from '@monaco-editor/react';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import type { CommandResponse } from '../types';

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
  const [isDeploying, setIsDeploying] = useState(false);
  const [deployError, setDeployError] = useState<string | null>(null);
  const [repoRoot, setRepoRoot] = useState<string>('');
  const [isDarkMode, setIsDarkMode] = useState(false);

  useEffect(() => {
    // Check for dark mode
    const checkDarkMode = () => {
      setIsDarkMode(document.documentElement.classList.contains('dark') || 
                   window.matchMedia('(prefers-color-scheme: dark)').matches);
    };
    checkDarkMode();
    const observer = new MutationObserver(checkDarkMode);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const fetchRepoRoot = async () => {
      try {
        const root = await invoke<string>('get_repo_root');
        setRepoRoot(root);
      } catch (error) {
        console.error('Failed to get repo root:', error);
      }
    };
    fetchRepoRoot();
  }, []);

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

  const handleDeploy = async () => {
    if (!repoRoot) {
      setDeployError('Repository root not available');
      return;
    }

    // Only allow deploying main.bicep, not individual modules
    if (!selectedFile.includes('main.bicep')) {
      setDeployError('Only the main.bicep template can be deployed. Individual modules are deployed as part of the main template.');
      return;
    }

    const confirmDeploy = confirm(
      `Are you sure you want to deploy this Bicep template?\n\n` +
      `File: ${selectedFile}\n` +
      `Environment: dev\n\n` +
      `This will deploy all infrastructure resources to the dev environment.`
    );

    if (!confirmDeploy) return;

    setIsDeploying(true);
    setDeployError(null);

    try {
      // Deploy main template with all resources
      const response = await invoke<CommandResponse>('azure_deploy_infrastructure', {
        repoRoot,
        environment: 'dev',
        deployStorage: true,
        deployCosmos: true,
        deployAppService: true,
      });

      if (response.success) {
        alert('Deployment started successfully!');
      } else {
        setDeployError(response.error || 'Deployment failed');
      }
    } catch (err) {
      setDeployError(`Failed to deploy: ${err}`);
    } finally {
      setIsDeploying(false);
    }
  };

  const renderFileTree = (files: BicepFile[], depth: number = 0) => {
    return files.map((file) => (
      <div key={file.path}>
        {file.type === 'folder' ? (
          <div>
            <button
              onClick={() => toggleFolder(file.path)}
              className={`w-full text-left px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center`}
              style={{ paddingLeft: `${depth * 16 + 8}px` }}
            >
              <span className="mr-2 text-gray-500 dark:text-gray-400">
                {expandedFolders.has(file.path) ? '📂' : '📁'}
              </span>
              <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{file.name}</span>
            </button>
            {expandedFolders.has(file.path) && file.children && (
              <div>{renderFileTree(file.children, depth + 1)}</div>
            )}
          </div>
        ) : (
          <button
            onClick={() => setSelectedFile(file.path)}
            className={`w-full text-left px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center ${
              selectedFile === file.path ? 'bg-blue-50 dark:bg-blue-900/30 border-l-2 border-blue-500' : ''
            }`}
            style={{ paddingLeft: `${depth * 16 + 8}px` }}
          >
            <span className="mr-2 text-gray-500 dark:text-gray-400">📄</span>
            <span className="text-sm text-gray-700 dark:text-gray-300">{file.name}</span>
          </button>
        )}
      </div>
    ));
  };

  return (
    <div className="flex h-[600px] border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-800">
      {/* File Tree Sidebar */}
      <div className="w-64 bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 overflow-y-auto">
        <div className="p-3 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <h3 className="font-semibold text-gray-900 dark:text-gray-100 text-sm">Bicep Templates</h3>
        </div>
        <div className="py-2">{renderFileTree(BICEP_FILES)}</div>
      </div>

      {/* Monaco Editor */}
      <div className="flex-1 flex flex-col">
        <div className="px-4 py-2 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
          <div className="flex items-center">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{selectedFile}</span>
            <span className="ml-3 text-xs px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 rounded">
              Read-only
            </span>
          </div>
          <div className="flex gap-2">
            <button
              onClick={handleDeploy}
              disabled={isDeploying || !repoRoot || !selectedFile.includes('main.bicep')}
              className="text-sm px-3 py-1 bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded transition-colors"
              title={!selectedFile.includes('main.bicep') ? 'Only main.bicep can be deployed' : 'Deploy infrastructure'}
            >
              {isDeploying ? 'Deploying...' : '🚀 Deploy'}
            </button>
            <button
              onClick={() => loadFile(selectedFile)}
              className="text-sm px-3 py-1 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded transition-colors"
              disabled={loading}
            >
              {loading ? 'Refreshing...' : '🔄 Refresh'}
            </button>
          </div>
        </div>

        {deployError && (
          <div className="px-4 py-2 bg-red-50 dark:bg-red-900/30 border-b border-red-200 dark:border-red-800">
            <div className="text-sm text-red-800 dark:text-red-200">❌ {deployError}</div>
          </div>
        )}

        <div className="flex-1 relative">
          {error ? (
            <div className="absolute inset-0 flex items-center justify-center bg-red-50 dark:bg-red-900/30">
              <div className="text-center p-6">
                <div className="text-4xl mb-3">⚠️</div>
                <div className="text-red-900 dark:text-red-300 font-medium mb-2">Failed to Load File</div>
                <div className="text-red-700 dark:text-red-400 text-sm">{error}</div>
              </div>
            </div>
          ) : loading ? (
            <div className="absolute inset-0 flex items-center justify-center bg-gray-50 dark:bg-gray-900">
              <div className="text-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400 mx-auto mb-3"></div>
                <div className="text-gray-600 dark:text-gray-400">Loading file...</div>
              </div>
            </div>
          ) : (
            <Editor
              height="100%"
              defaultLanguage="bicep"
              language="bicep"
              value={fileContent}
              theme={isDarkMode ? "vs-dark" : "vs-light"}
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
        <div className="px-4 py-2 bg-gray-50 dark:bg-gray-900 border-t border-gray-200 dark:border-gray-700">
          <p className="text-xs text-gray-600 dark:text-gray-400">
            💡 Tip: Edit Bicep files in your IDE. This viewer is read-only for safety.
          </p>
        </div>
      </div>
    </div>
  );
}

export default BicepViewer;
