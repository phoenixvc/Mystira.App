import { useState } from 'react';
import type { ResourceGroupConvention } from '../types';

export interface TemplateConfig {
  id: string;
  name: string;
  file: string;
  description: string;
  selected: boolean;
  resourceGroup: string;
  parameters: Record<string, any>;
}

interface TemplateSelectorProps {
  environment: string;
  resourceGroupConfig: ResourceGroupConvention;
  templates: TemplateConfig[];
  onTemplatesChange: (templates: TemplateConfig[]) => void;
  onEditTemplate: (template: TemplateConfig) => void;
  region?: string;
  projectName?: string;
}

function TemplateSelector({
  environment,
  resourceGroupConfig,
  templates,
  onTemplatesChange,
  onEditTemplate,
  region = 'euw',
  projectName = 'mystira-app',
}: TemplateSelectorProps) {
  const [editingResourceGroup, setEditingResourceGroup] = useState<string | null>(null);
  const [tempResourceGroup, setTempResourceGroup] = useState<string>('');

  const generateResourceGroupName = (templateId: string): string => {
    let rg = resourceGroupConfig.pattern || '{env}-{region}-rg-{project}';
    rg = rg.replace(/{env}/g, resourceGroupConfig.environment || environment);
    rg = rg.replace(/{region}/g, resourceGroupConfig.region || region);
    rg = rg.replace(/{project}/g, resourceGroupConfig.projectName || projectName);
    rg = rg.replace(/{resource}/g, templateId);
    return rg;
  };

  const toggleTemplateSelection = (templateId: string) => {
    const updated = templates.map(t =>
      t.id === templateId ? { ...t, selected: !t.selected } : t
    );
    onTemplatesChange(updated);
  };

  const updateResourceGroup = (templateId: string, resourceGroup: string) => {
    const updated = templates.map(t =>
      t.id === templateId ? { ...t, resourceGroup } : t
    );
    onTemplatesChange(updated);
    setEditingResourceGroup(null);
  };

  const startEditingResourceGroup = (templateId: string, currentResourceGroup: string) => {
    setEditingResourceGroup(templateId);
    setTempResourceGroup(currentResourceGroup);
  };

  const selectAll = () => {
    const updated = templates.map(t => ({ ...t, selected: true }));
    onTemplatesChange(updated);
  };

  const deselectAll = () => {
    const updated = templates.map(t => ({ ...t, selected: false }));
    onTemplatesChange(updated);
  };

  const selectedCount = templates.filter(t => t.selected).length;

  return (
    <div className="mb-8">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">
            Step 1: Select Bicep Templates
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Choose which templates to validate, preview, and deploy
          </p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={selectAll}
            className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
          >
            Select All
          </button>
          <button
            onClick={deselectAll}
            className="px-3 py-1.5 text-xs bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded"
          >
            Deselect All
          </button>
          <span className="px-3 py-1.5 text-xs text-gray-600 dark:text-gray-400">
            {selectedCount} of {templates.length} selected
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {templates.map((template, index) => {
          const isEditing = editingResourceGroup === template.id;
          const resourceGroup = template.resourceGroup || generateResourceGroupName(template.id);

          return (
            <div
              key={template.id}
              className={`border-2 rounded-lg p-4 transition-all ${
                template.selected
                  ? 'border-blue-500 dark:border-blue-600 bg-blue-50 dark:bg-blue-900/20'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800'
              }`}
            >
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0">
                  <div className="w-8 h-8 rounded-full bg-blue-600 dark:bg-blue-500 text-white flex items-center justify-center font-bold text-sm">
                    {index + 1}
                  </div>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-2">
                    <input
                      type="checkbox"
                      checked={template.selected}
                      onChange={() => toggleTemplateSelection(template.id)}
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                      aria-label={`Select ${template.name} template`}
                    />
                    <h4 className="font-semibold text-gray-900 dark:text-white">{template.name}</h4>
                  </div>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">
                    {template.description}
                  </p>
                  <div className="text-xs font-mono text-gray-500 dark:text-gray-400 mb-2">
                    {template.file}
                  </div>
                  
                  {/* Resource Group Display/Edit */}
                  <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700">
                    <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">
                      Resource Group:
                    </label>
                    {isEditing ? (
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={tempResourceGroup}
                      onChange={(e) => setTempResourceGroup(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          updateResourceGroup(template.id, tempResourceGroup);
                        } else if (e.key === 'Escape') {
                          setEditingResourceGroup(null);
                        }
                      }}
                      className="flex-1 px-2 py-1 text-xs border border-gray-300 dark:border-gray-600 rounded dark:bg-gray-700 dark:text-white font-mono"
                      autoFocus
                      aria-label="Resource group name"
                      placeholder="Enter resource group name"
                    />
                        <button
                          onClick={() => updateResourceGroup(template.id, tempResourceGroup)}
                          className="px-2 py-1 text-xs bg-green-600 hover:bg-green-700 text-white rounded"
                        >
                          ✓
                        </button>
                        <button
                          onClick={() => setEditingResourceGroup(null)}
                          className="px-2 py-1 text-xs bg-gray-400 hover:bg-gray-500 text-white rounded"
                        >
                          ✕
                        </button>
                      </div>
                    ) : (
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-mono text-gray-800 dark:text-gray-200 flex-1 truncate">
                          {resourceGroup}
                        </span>
                        <button
                          onClick={() => startEditingResourceGroup(template.id, resourceGroup)}
                          className="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
                          title="Edit resource group"
                        >
                          ✏️
                        </button>
                      </div>
                    )}
                  </div>

                  {/* Edit Template Button */}
                  <div className="mt-3">
                    <button
                      onClick={() => onEditTemplate(template)}
                      className="w-full px-3 py-1.5 text-xs bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded transition-colors"
                    >
                      ⚙️ Edit Configuration
                    </button>
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default TemplateSelector;

