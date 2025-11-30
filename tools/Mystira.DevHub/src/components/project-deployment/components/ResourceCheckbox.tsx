import type { DeploymentStatus } from '../types';

interface ResourceCheckboxProps {
  label: string;
  checked: boolean;
  onChange: () => void;
  status?: DeploymentStatus['resources'][keyof DeploymentStatus['resources']];
  projectName: string;
}

const resourceDescriptions: Record<string, string> = {
  'Storage': 'Azure Blob Storage for file storage and static content',
  'Cosmos DB': 'Azure Cosmos DB for NoSQL database storage',
  'App Service': 'Azure App Service for hosting web applications',
  'Key Vault': 'Azure Key Vault for secure secrets management',
};

export function ResourceCheckbox({
  label,
  checked,
  onChange,
  status,
  projectName,
}: ResourceCheckboxProps) {
  return (
    <label
      className={`flex items-center gap-2 p-2 rounded-md cursor-pointer transition-all duration-150
        hover:bg-gray-100 dark:hover:bg-gray-700/50
        focus-within:ring-2 focus-within:ring-blue-500 focus-within:ring-offset-1 dark:focus-within:ring-offset-gray-800
        ${checked ? 'bg-blue-50 dark:bg-blue-900/30' : ''}
      `}
      title={resourceDescriptions[label] || label}
    >
      <input
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className="w-4 h-4 text-blue-600 border-gray-300 dark:border-gray-600 rounded
          focus:ring-2 focus:ring-blue-500 focus:ring-offset-0
          transition-all duration-150 cursor-pointer flex-shrink-0
          checked:bg-blue-600 checked:border-blue-600"
        aria-label={`Select ${label} for ${projectName}`}
      />
      <span className={`text-xs flex-1 transition-colors ${checked ? 'text-blue-700 dark:text-blue-300 font-medium' : 'text-gray-700 dark:text-gray-300'}`}>
        {label}
      </span>
      {status?.deployed ? (
        <span
          className="text-xs text-green-600 dark:text-green-400 flex items-center gap-1"
          title={`Deployed: ${status.name || 'Resource deployed successfully'}`}
        >
          <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
          <span className="hidden sm:inline">Deployed</span>
        </span>
      ) : (
        <span
          className="text-xs text-amber-600 dark:text-amber-400 flex items-center gap-1"
          title="This resource has not been deployed yet. Select and deploy to create it."
        >
          <span className="w-2 h-2 rounded-full bg-amber-500" />
          <span className="hidden sm:inline">Not Deployed</span>
        </span>
      )}
    </label>
  );
}

