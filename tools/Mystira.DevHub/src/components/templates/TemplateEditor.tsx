import { useEffect, useState } from 'react';
import type { TemplateConfig } from './TemplateSelector';

interface TemplateEditorProps {
  template: TemplateConfig | null;
  onSave: (template: TemplateConfig, saveAsNew: boolean) => void;
  onClose: () => void;
}

function TemplateEditor({ template, onSave, onClose }: TemplateEditorProps) {
  const [editedTemplate, setEditedTemplate] = useState<TemplateConfig | null>(null);
  const [saveAsNew, setSaveAsNew] = useState(false);

  useEffect(() => {
    if (template) {
      setEditedTemplate({ ...template });
      setSaveAsNew(false);
    }
  }, [template]);

  if (!template || !editedTemplate) {
    return null;
  }

  const handleSave = () => {
    onSave(editedTemplate, saveAsNew);
    onClose();
  };

  const updateParameter = (key: string, value: any) => {
    setEditedTemplate({
      ...editedTemplate,
      parameters: {
        ...editedTemplate.parameters,
        [key]: value,
      },
    });
  };

  const addParameter = () => {
    const key = prompt('Parameter name:');
    if (key) {
      updateParameter(key, '');
    }
  };

  const removeParameter = (key: string) => {
    const newParams = { ...editedTemplate.parameters };
    delete newParams[key];
    setEditedTemplate({
      ...editedTemplate,
      parameters: newParams,
    });
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-3xl w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="p-6 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
            Edit Template: {template.name}
          </h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {template.file}
          </p>
        </div>

        <div className="p-6 space-y-6">
          {/* Template Name */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Template Name
            </label>
            <input
              type="text"
              value={editedTemplate.name}
              onChange={(e) => setEditedTemplate({ ...editedTemplate, name: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
              aria-label="Template name"
            />
          </div>

          {/* Resource Group */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Resource Group
            </label>
            <input
              type="text"
              value={editedTemplate.resourceGroup}
              onChange={(e) => setEditedTemplate({ ...editedTemplate, resourceGroup: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white font-mono text-sm"
              aria-label="Resource group"
            />
          </div>

          {/* Parameters */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Parameters
              </label>
              <button
                onClick={addParameter}
                className="px-3 py-1 text-xs bg-blue-600 hover:bg-blue-700 text-white rounded"
              >
                + Add Parameter
              </button>
            </div>
            <div className="space-y-2">
              {Object.entries(editedTemplate.parameters).map(([key, value]) => (
                <div key={key} className="flex gap-2 items-center">
                  <input
                    type="text"
                    value={key}
                    readOnly
                    className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white font-mono text-sm"
                    aria-label={`Parameter ${key} name`}
                  />
                  <input
                    type="text"
                    value={typeof value === 'string' ? value : JSON.stringify(value)}
                    onChange={(e) => {
                      try {
                        const parsed = JSON.parse(e.target.value);
                        updateParameter(key, parsed);
                      } catch {
                        updateParameter(key, e.target.value);
                      }
                    }}
                    className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white text-sm"
                    aria-label={`Parameter ${key} value`}
                  />
                  <button
                    onClick={() => removeParameter(key)}
                    className="px-3 py-1 text-xs bg-red-100 dark:bg-red-900 hover:bg-red-200 dark:hover:bg-red-800 text-red-700 dark:text-red-300 rounded"
                  >
                    Remove
                  </button>
                </div>
              ))}
              {Object.keys(editedTemplate.parameters).length === 0 && (
                <p className="text-sm text-gray-500 dark:text-gray-400 italic">
                  No parameters configured. Click "Add Parameter" to add one.
                </p>
              )}
            </div>
          </div>

          {/* Save As New Option */}
          <div className="flex items-center">
            <input
              type="checkbox"
              id="saveAsNew"
              checked={saveAsNew}
              onChange={(e) => setSaveAsNew(e.target.checked)}
              className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              aria-label="Save as new template"
            />
            <label htmlFor="saveAsNew" className="ml-2 text-sm text-gray-700 dark:text-gray-300">
              Save as new template configuration
            </label>
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
            {saveAsNew ? 'Save As New' : 'Save Changes'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default TemplateEditor;

