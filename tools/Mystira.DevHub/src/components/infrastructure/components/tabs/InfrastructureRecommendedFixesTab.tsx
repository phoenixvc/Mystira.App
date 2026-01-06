import { invoke } from '@tauri-apps/api/tauri';
import type { CommandResponse } from '../../../../types';

interface InfrastructureRecommendedFixesTabProps {
  environment: string;
}

export default function InfrastructureRecommendedFixesTab({ environment }: InfrastructureRecommendedFixesTabProps) {
  return (
    <div>
      <div className="mb-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
          🔧 Recommended Fixes & Improvements
        </h3>
        <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
          Security and safety improvements to prevent accidental actions and improve resource management
        </p>
      </div>

      <div className="space-y-4">
        {/* Delete Button Protection */}
        <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg border border-yellow-200 dark:border-yellow-800">
          <h4 className="font-semibold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
            <span>🛡️</span>
            Delete Button Protection
          </h4>
          <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
            Add filters and confirmation requirements to prevent accidental deletion of resources
          </p>
          <div className="flex flex-wrap gap-2">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Require resource name confirmation before delete</span>
            </label>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Hide delete buttons by default (toggle to show)</span>
            </label>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Filter by environment prefix before allowing delete</span>
            </label>
          </div>
        </div>

        {/* Environment Switch Security */}
        <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
          <h4 className="font-semibold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
            <span>🔒</span>
            Environment Switch Security
          </h4>
          <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
            Require subscription owner permissions for production environment operations
          </p>
          <div className="space-y-2">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={true}
                readOnly
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Require subscription owner role for prod-* resources</span>
            </label>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Auto-detect subscription owner and validate before operations</span>
            </label>
          </div>
        </div>

        {/* Resource Tagging */}
        <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-200 dark:border-green-800">
          <h4 className="font-semibold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
            <span>🏷️</span>
            Resource Tagging Script
          </h4>
          <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
            Automatically add required tags to Azure resources for better organization and compliance
          </p>
          <div className="space-y-3">
            <button
              onClick={async () => {
                try {
                  const response = await invoke<CommandResponse>('run_resource_tagging_script', {
                    environment: environment === 'prod' ? 'prod' : 'dev',
                    dryRun: true,
                  });
                  if (response.success) {
                    alert('Tagging script ready. Preview mode will show what tags would be added.');
                  } else {
                    alert(`Error: ${response.error}`);
                  }
                } catch (error) {
                  console.error('Failed to run tagging script:', error);
                  alert('Tagging script feature is not yet implemented in the backend.');
                }
              }}
              className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors text-sm font-medium"
            >
              🔍 Preview Tags (Dry Run)
            </button>
            <button
              onClick={async () => {
                if (!confirm(`Are you sure you want to add tags to all ${environment} resources?`)) {
                  return;
                }
                try {
                  const response = await invoke<CommandResponse>('run_resource_tagging_script', {
                    environment: environment === 'prod' ? 'prod' : 'dev',
                    dryRun: false,
                  });
                  if (response.success) {
                    alert('Tags have been successfully added to resources.');
                  } else {
                    alert(`Error: ${response.error}`);
                  }
                } catch (error) {
                  console.error('Failed to run tagging script:', error);
                  alert('Tagging script feature is not yet implemented in the backend.');
                }
              }}
              className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors text-sm font-medium ml-2"
            >
              ✏️ Apply Tags to Resources
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

