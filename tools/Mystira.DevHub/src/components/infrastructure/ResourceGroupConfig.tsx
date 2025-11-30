import { useEffect, useState } from 'react';
import type { ResourceGroupConvention } from '../../types';

interface ResourceGroupConfigProps {
  environment: string;
  onSave: (config: ResourceGroupConvention) => void;
  onClose: () => void;
}

const AVAILABLE_VARIABLES = [
  { key: '{env}', label: 'Environment', description: 'dev, prod, staging', example: 'dev' },
    { key: '{region}', label: 'Region', description: 'san, euw, eus, wus', example: 'san' },
  { key: '{project}', label: 'Project Name', description: 'mystira-app', example: 'mystira-app' },
  { key: '{resource}', label: 'Resource Type', description: 'storage, cosmos, app', example: 'storage' },
  { key: '{rg}', label: 'Resource Group', description: 'rg', example: 'rg' },
  { key: '{subscription}', label: 'Subscription', description: 'subscription-id', example: 'sub-001' },
  { key: '{app}', label: 'Application', description: 'app name', example: 'mystira' },
];

function ResourceGroupConfig({ environment, onSave, onClose }: ResourceGroupConfigProps) {
  const [config, setConfig] = useState<ResourceGroupConvention>({
    pattern: '{env}-{region}-rg-{project}',
    defaultResourceGroup: `${environment}-san-rg-mystira-app`,
    resourceTypeMappings: {},
    environment: environment,
    region: 'san',
    projectName: 'mystira-app',
  });
  const [editingMapping, setEditingMapping] = useState<string | null>(null);
  const [newResourceType, setNewResourceType] = useState('');
  const [newResourceGroup, setNewResourceGroup] = useState('');
  const [previewPattern, setPreviewPattern] = useState('');

  useEffect(() => {
    // Load saved config from localStorage
    const saved = localStorage.getItem(`resourceGroupConfig_${environment}`);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        setConfig(parsed);
      } catch (e) {
        console.error('Failed to parse saved resource group config:', e);
      }
    }
  }, [environment]);

  // Update preview when pattern or variables change
  useEffect(() => {
    let preview = config.pattern;
    preview = preview.replace(/{env}/g, config.environment || environment);
    preview = preview.replace(/{region}/g, config.region || 'san');
    preview = preview.replace(/{project}/g, config.projectName || 'mystira-app');
    preview = preview.replace(/{resource}/g, 'storage');
    preview = preview.replace(/{rg}/g, 'rg');
    preview = preview.replace(/{subscription}/g, 'sub-001');
    preview = preview.replace(/{app}/g, 'mystira');
    setPreviewPattern(preview);
    
    // Update default resource group based on pattern
    if (config.pattern) {
      let defaultRG = config.pattern;
      defaultRG = defaultRG.replace(/{env}/g, config.environment || environment);
      defaultRG = defaultRG.replace(/{region}/g, config.region || 'san');
      defaultRG = defaultRG.replace(/{project}/g, config.projectName || 'mystira-app');
      defaultRG = defaultRG.replace(/{resource}/g, 'mystira-app');
      defaultRG = defaultRG.replace(/{rg}/g, 'rg');
      defaultRG = defaultRG.replace(/{subscription}/g, '');
      defaultRG = defaultRG.replace(/{app}/g, 'mystira');
      setConfig(prev => ({ ...prev, defaultResourceGroup: defaultRG }));
    }
  }, [config.pattern, config.environment, config.region, config.projectName, environment]);

  const handleSave = () => {
    localStorage.setItem(`resourceGroupConfig_${environment}`, JSON.stringify(config));
    onSave(config);
    onClose();
  };

  const addResourceTypeMapping = () => {
    if (newResourceType && newResourceGroup) {
      setConfig({
        ...config,
        resourceTypeMappings: {
          ...config.resourceTypeMappings,
          [newResourceType]: newResourceGroup,
        },
      });
      setNewResourceType('');
      setNewResourceGroup('');
    }
  };

  const removeResourceTypeMapping = (resourceType: string) => {
    const newMappings = { ...config.resourceTypeMappings };
    delete newMappings[resourceType];
    setConfig({
      ...config,
      resourceTypeMappings: newMappings,
    });
  };

  const updateResourceTypeMapping = (resourceType: string, resourceGroup: string) => {
    setConfig({
      ...config,
      resourceTypeMappings: {
        ...config.resourceTypeMappings,
        [resourceType]: resourceGroup,
      },
    });
    setEditingMapping(null);
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="p-6 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
            Resource Group Configuration
          </h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Configure resource group naming conventions for {environment} environment
          </p>
        </div>

        <div className="p-6 space-y-6">
          {/* Default Resource Group */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Default Resource Group
            </label>
            <input
              type="text"
              value={config.defaultResourceGroup}
              onChange={(e) =>
                setConfig({ ...config, defaultResourceGroup: e.target.value })
              }
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
              placeholder="dev-san-rg-mystira-app"
            />
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Default resource group used when no specific mapping exists
            </p>
          </div>

          {/* Environment and Region Selectors */}
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Environment
              </label>
              <select
                value={config.environment || environment}
                onChange={(e) => setConfig({ ...config, environment: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
                aria-label="Select environment"
              >
                <option value="dev">dev</option>
                <option value="staging">staging</option>
                <option value="prod">prod</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Region
              </label>
              <select
                value={config.region || 'san'}
                onChange={(e) => setConfig({ ...config, region: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
                aria-label="Select region"
              >
                <option value="san">san (South Africa North)</option>
                <option value="euw">euw (West Europe)</option>
                <option value="eus">eus (East US)</option>
                <option value="wus">wus (West US)</option>
                <option value="uks">uks (UK South)</option>
                <option value="sea">sea (Southeast Asia)</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Project Name
              </label>
              <input
                type="text"
                value={config.projectName || 'mystira-app'}
                onChange={(e) => setConfig({ ...config, projectName: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
                placeholder="mystira-app"
              />
            </div>
          </div>

          {/* Naming Pattern with Variable Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Naming Pattern
            </label>
            <div className="flex gap-2 mb-2">
              <input
                type="text"
                value={config.pattern}
                onChange={(e) => setConfig({ ...config, pattern: e.target.value })}
                className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white font-mono"
                placeholder="{env}-{region}-rg-{project}"
              />
              <div className="relative">
                <select
                  onChange={(e) => {
                    const selectedVar = e.target.value;
                    if (selectedVar) {
                      const newPattern = config.pattern + selectedVar;
                      setConfig({ ...config, pattern: newPattern });
                      e.target.value = '';
                    }
                  }}
                  className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white text-sm"
                  defaultValue=""
                  aria-label="Select variable to insert into pattern"
                >
                  <option value="" disabled>Insert Variable</option>
                  {AVAILABLE_VARIABLES.map((v) => (
                    <option key={v.key} value={v.key}>
                      {v.key} - {v.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="mb-2">
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
                Preview: <span className="font-mono text-blue-600 dark:text-blue-400">{previewPattern}</span>
              </p>
            </div>
            <div className="bg-gray-50 dark:bg-gray-700 rounded-md p-3">
              <p className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-2">Available Variables:</p>
              <div className="grid grid-cols-2 gap-2">
                {AVAILABLE_VARIABLES.map((v) => (
                  <div key={v.key} className="text-xs">
                    <span className="font-mono text-blue-600 dark:text-blue-400">{v.key}</span>
                    <span className="text-gray-600 dark:text-gray-400 ml-1">- {v.label}</span>
                    <span className="text-gray-500 dark:text-gray-500 ml-1">({v.description})</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Resource Type Mappings */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Resource Type to Resource Group Mappings
            </label>
            <div className="space-y-2 mb-3">
              {Object.entries(config.resourceTypeMappings || {}).map(([type, rg]) => (
                <div
                  key={type}
                  className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-700 rounded border border-gray-200 dark:border-gray-600"
                >
                  {editingMapping === type ? (
                    <>
                      <input
                        type="text"
                        value={rg}
                        onChange={(e) =>
                          updateResourceTypeMapping(type, e.target.value)
                        }
                        className="flex-1 px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded dark:bg-gray-600 dark:text-white"
                        autoFocus
                      />
                      <button
                        onClick={() => setEditingMapping(null)}
                        className="px-2 py-1 text-xs bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-500 rounded"
                      >
                        Done
                      </button>
                    </>
                  ) : (
                    <>
                      <div className="flex-1">
                        <div className="text-xs font-mono text-gray-600 dark:text-gray-300">
                          {type}
                        </div>
                        <div className="text-sm text-gray-900 dark:text-white">{rg}</div>
                      </div>
                      <button
                        onClick={() => setEditingMapping(type)}
                        className="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 rounded"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => removeResourceTypeMapping(type)}
                        className="px-2 py-1 text-xs bg-red-100 dark:bg-red-900 hover:bg-red-200 dark:hover:bg-red-800 rounded"
                      >
                        Remove
                      </button>
                    </>
                  )}
                </div>
              ))}
            </div>

            {/* Add new mapping */}
            <div className="flex gap-2">
              <input
                type="text"
                value={newResourceType}
                onChange={(e) => setNewResourceType(e.target.value)}
                placeholder="Microsoft.Storage/storageAccounts"
                className="flex-1 px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white"
              />
              <input
                type="text"
                value={newResourceGroup}
                onChange={(e) => setNewResourceGroup(e.target.value)}
                placeholder="dev-san-rg-storage"
                className="flex-1 px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white"
              />
              <button
                onClick={addResourceTypeMapping}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md text-sm"
              >
                Add
              </button>
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Map specific resource types to custom resource groups
            </p>
          </div>
        </div>

        <div className="p-6 border-t border-gray-200 dark:border-gray-700 flex justify-end gap-3">
          <button
            onClick={onClose}
            className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md"
          >
            Save Configuration
          </button>
        </div>
      </div>
    </div>
  );
}

export default ResourceGroupConfig;

