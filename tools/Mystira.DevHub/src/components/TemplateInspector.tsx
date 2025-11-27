import Editor from '@monaco-editor/react';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';

interface TemplateFile {
  name: string;
  path: string;
  type: 'bicep' | 'json' | 'yml';
  description: string;
}

interface TemplateInspectorProps {
  environment: string;
}

function TemplateContentViewer({ filePath }: { filePath: string }) {
  const [content, setContent] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [isDarkMode, setIsDarkMode] = useState(false);

  useEffect(() => {
    const checkDarkMode = () => {
      setIsDarkMode(window.matchMedia('(prefers-color-scheme: dark)').matches);
    };
    checkDarkMode();
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    mediaQuery.addEventListener('change', checkDarkMode);
    return () => mediaQuery.removeEventListener('change', checkDarkMode);
  }, []);

  useEffect(() => {
    loadFile();
  }, [filePath]);

  const loadFile = async () => {
    setLoading(true);
    try {
      const fileContent = await invoke<string>('read_bicep_file', { filePath });
      setContent(fileContent);
    } catch (error) {
      console.error('Failed to load file:', error);
      setContent(`// Error loading file: ${error}`);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="h-full flex items-center justify-center">
        <div className="text-gray-500 dark:text-gray-400">Loading...</div>
      </div>
    );
  }

  return (
    <div className="h-full">
      <Editor
        height="100%"
        language="bicep"
        value={content}
        theme={isDarkMode ? 'vs-dark' : 'vs'}
        options={{
          readOnly: true,
          minimap: { enabled: true },
          fontSize: 14,
          wordWrap: 'on',
        }}
      />
    </div>
  );
}

function TemplateInspector({ environment }: TemplateInspectorProps) {
  const [templates, setTemplates] = useState<TemplateFile[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadTemplates();
  }, [environment]);

  const loadTemplates = async () => {
    setLoading(true);
    try {
      const basePath = `infrastructure/${environment}`;
      const templateFiles: TemplateFile[] = [
        {
          name: 'main.bicep',
          path: `${basePath}/main.bicep`,
          type: 'bicep',
          description: 'Main deployment template orchestrating all modules',
        },
        {
          name: 'storage.bicep',
          path: `${basePath}/storage.bicep`,
          type: 'bicep',
          description: 'Azure Storage Account with blob services',
        },
        {
          name: 'cosmos-db.bicep',
          path: `${basePath}/cosmos-db.bicep`,
          type: 'bicep',
          description: 'Azure Cosmos DB account with database and containers',
        },
        {
          name: 'app-service.bicep',
          path: `${basePath}/app-service.bicep`,
          type: 'bicep',
          description: 'Azure App Service with Linux runtime',
        },
        {
          name: 'key-vault.bicep',
          path: `${basePath}/key-vault.bicep`,
          type: 'bicep',
          description: 'Azure Key Vault for secrets management',
        },
      ];

      setTemplates(templateFiles);
      if (templateFiles.length > 0) {
        setSelectedTemplate(templateFiles[0].path);
      }
    } catch (error) {
      console.error('Failed to load templates:', error);
    } finally {
      setLoading(false);
    }
  };

  const getFileIcon = (type: string) => {
    switch (type) {
      case 'bicep': return '📄';
      case 'json': return '📋';
      case 'yml': return '⚙️';
      default: return '📄';
    }
  };

  const handleCopyTemplate = async (template: TemplateFile) => {
    try {
      const content = await invoke<string>('read_bicep_file', { filePath: template.path });
      await navigator.clipboard.writeText(content);
      alert(`Template ${template.name} copied to clipboard!`);
    } catch (error) {
      console.error('Failed to copy template:', error);
      alert('Failed to copy template');
    }
  };

  return (
    <div className="h-full flex flex-col">
      <div className="mb-4">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">
          Template & Resource Inspector
        </h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Inspect, modify, copy, and edit Bicep templates and infrastructure resources
        </p>
      </div>

      <div className="flex-1 flex gap-4 min-h-0">
        {/* Sidebar - Template List */}
        <div className="w-64 flex-shrink-0 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 overflow-y-auto">
          <div className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-3">
            Templates ({environment})
          </div>
          {loading ? (
            <div className="text-sm text-gray-500 dark:text-gray-400">Loading...</div>
          ) : (
            <div className="space-y-2">
              {templates.map((template) => (
                <div
                  key={template.path}
                  onClick={() => setSelectedTemplate(template.path)}
                  className={`p-3 rounded-lg cursor-pointer transition-colors ${
                    selectedTemplate === template.path
                      ? 'bg-blue-100 dark:bg-blue-900 border-2 border-blue-500 dark:border-blue-600'
                      : 'bg-gray-50 dark:bg-gray-700 border-2 border-transparent hover:bg-gray-100 dark:hover:bg-gray-600'
                  }`}
                >
                  <div className="flex items-center gap-2 mb-1">
                    <span className="text-lg">{getFileIcon(template.type)}</span>
                    <span className="text-sm font-medium text-gray-900 dark:text-white">
                      {template.name}
                    </span>
                  </div>
                  <p className="text-xs text-gray-600 dark:text-gray-400 mb-2">
                    {template.description}
                  </p>
                  <div className="flex gap-1">
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        handleCopyTemplate(template);
                      }}
                      className="px-2 py-1 text-xs bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-500 text-gray-700 dark:text-gray-300 rounded"
                      title="Copy template"
                    >
                      📋 Copy
                    </button>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        // TODO: Implement edit functionality
                        alert('Edit functionality coming soon');
                      }}
                      className="px-2 py-1 text-xs bg-blue-200 dark:bg-blue-800 hover:bg-blue-300 dark:hover:bg-blue-700 text-blue-700 dark:text-blue-300 rounded"
                      title="Edit template"
                    >
                      ✏️ Edit
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Main Content - Template Viewer */}
        <div className="flex-1 min-w-0 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          {selectedTemplate ? (
            <TemplateContentViewer filePath={selectedTemplate} />
          ) : (
            <div className="h-full flex items-center justify-center text-gray-500 dark:text-gray-400">
              Select a template to view
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default TemplateInspector;

