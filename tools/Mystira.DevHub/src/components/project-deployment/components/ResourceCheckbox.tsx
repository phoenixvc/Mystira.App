import type { DeploymentStatus } from '../types';

interface ResourceCheckboxProps {
  label: string;
  checked: boolean;
  onChange: () => void;
  status?: DeploymentStatus['resources'][keyof DeploymentStatus['resources']];
  projectName: string;
}

export function ResourceCheckbox({
  label,
  checked,
  onChange,
  status,
  projectName,
}: ResourceCheckboxProps) {
  return (
    <div className="flex items-center gap-2">
      <input
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500 flex-shrink-0"
        aria-label={`Select ${label} for ${projectName}`}
      />
      <span className="text-xs text-gray-700 dark:text-gray-300 flex-1">{label}</span>
      {status?.deployed ? (
        <span className="text-xs text-green-600 dark:text-green-400" title={status.name}>
          ✓
        </span>
      ) : (
        <span className="text-xs text-red-600 dark:text-red-400">
          ✗ Not Deployed
        </span>
      )}
    </div>
  );
}

