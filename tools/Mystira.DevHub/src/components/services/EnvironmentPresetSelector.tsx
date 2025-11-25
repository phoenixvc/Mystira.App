import { useState } from 'react';
import { DEFAULT_PRESETS, EnvironmentPreset, deletePreset, getAllPresets, savePreset } from './EnvironmentPresets';

interface EnvironmentPresetSelectorProps {
  currentEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  onApplyPreset: (preset: EnvironmentPreset) => void;
  onSaveCurrent: () => void;
}

export function EnvironmentPresetSelector({
  currentEnvironments,
  onApplyPreset,
  onSaveCurrent: _onSaveCurrent,
}: EnvironmentPresetSelectorProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [presetName, setPresetName] = useState('');
  const [presetDescription, setPresetDescription] = useState('');
  const allPresets = getAllPresets();
  
  const handleSavePreset = () => {
    if (!presetName.trim()) {
      alert('Please enter a name for the preset');
      return;
    }
    
    const newPreset: EnvironmentPreset = {
      id: `custom-${Date.now()}`,
      name: presetName.trim(),
      description: presetDescription.trim() || `Custom preset saved at ${new Date().toLocaleString()}`,
      environments: { ...currentEnvironments },
    };
    
    savePreset(newPreset);
    setShowSaveDialog(false);
    setPresetName('');
    setPresetDescription('');
    alert(`Preset "${newPreset.name}" saved successfully!`);
  };
  
  const handleDeletePreset = (preset: EnvironmentPreset, e: React.MouseEvent) => {
    e.stopPropagation();
    if (window.confirm(`Delete preset "${preset.name}"?`)) {
      deletePreset(preset.id);
    }
  };
  
  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="px-3 py-1.5 text-sm bg-purple-500 text-white rounded hover:bg-purple-600 font-medium transition-colors"
        title="Environment presets"
      >
        📋 Presets
      </button>
      
      {isOpen && (
        <>
          <div 
            className="fixed inset-0 z-40" 
            onClick={() => setIsOpen(false)}
          />
          <div className="absolute top-full left-0 mt-2 w-80 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-xl z-50 max-h-96 overflow-y-auto">
            <div className="p-3 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center justify-between mb-2">
                <h3 className="font-semibold text-gray-900 dark:text-white">Environment Presets</h3>
                <button
                  onClick={() => setIsOpen(false)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                >
                  ✕
                </button>
              </div>
              <button
                onClick={() => setShowSaveDialog(true)}
                className="w-full px-3 py-2 text-sm bg-green-500 text-white rounded hover:bg-green-600"
              >
                💾 Save Current Configuration
              </button>
            </div>
            
            <div className="p-2 space-y-1">
              {allPresets.map(preset => {
                const isDefault = DEFAULT_PRESETS.some(dp => dp.id === preset.id);
                return (
                  <div
                    key={preset.id}
                    className="p-2 rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer group"
                    onClick={() => {
                      onApplyPreset(preset);
                      setIsOpen(false);
                    }}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="font-medium text-gray-900 dark:text-white">
                          {preset.name}
                          {isDefault && <span className="ml-2 text-xs text-gray-500">(Default)</span>}
                        </div>
                        <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                          {preset.description}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-500 mt-1 flex gap-2 flex-wrap">
                          {Object.entries(preset.environments).map(([service, env]) => (
                            <span key={service} className={`px-1.5 py-0.5 rounded ${
                              env === 'local' ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300' :
                              env === 'dev' ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300' :
                              'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300'
                            }`}>
                              {service}: {env.toUpperCase()}
                            </span>
                          ))}
                        </div>
                      </div>
                      {!isDefault && (
                        <button
                          onClick={(e) => handleDeletePreset(preset, e)}
                          className="ml-2 text-red-500 hover:text-red-700 opacity-0 group-hover:opacity-100 transition-opacity"
                          title="Delete preset"
                        >
                          🗑️
                        </button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </>
      )}
      
      {showSaveDialog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 w-96 shadow-xl">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Save Environment Preset
            </h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Preset Name *
                </label>
                <input
                  type="text"
                  value={presetName}
                  onChange={(e) => setPresetName(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                  placeholder="e.g., My Custom Setup"
                  autoFocus
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Description
                </label>
                <textarea
                  value={presetDescription}
                  onChange={(e) => setPresetDescription(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                  placeholder="Optional description"
                  rows={2}
                />
              </div>
              <div className="flex gap-2 justify-end">
                <button
                  onClick={() => {
                    setShowSaveDialog(false);
                    setPresetName('');
                    setPresetDescription('');
                  }}
                  className="px-4 py-2 text-sm bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-600"
                >
                  Cancel
                </button>
                <button
                  onClick={handleSavePreset}
                  className="px-4 py-2 text-sm bg-green-500 text-white rounded hover:bg-green-600"
                >
                  Save
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

