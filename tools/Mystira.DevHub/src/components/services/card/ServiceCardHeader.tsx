import { ServiceConfig } from '../types';

interface ServiceCardHeaderProps {
  config: ServiceConfig;
  isCollapsed: boolean;
  isBuilding: boolean;
  buildFailed: boolean;
  logsCount: number;
  currentEnv: 'local' | 'dev' | 'prod';
  environmentStatus?: {
    dev?: 'online' | 'offline' | 'checking';
    prod?: 'online' | 'offline' | 'checking';
  };
  onToggleCollapse: () => void;
}

export function ServiceCardHeader({
  config,
  isCollapsed,
  isBuilding,
  buildFailed,
  logsCount,
  currentEnv,
  environmentStatus,
  onToggleCollapse,
}: ServiceCardHeaderProps) {
  return (
    <button
      onClick={onToggleCollapse}
      className="flex items-center gap-2 flex-1 text-left hover:opacity-80 transition-opacity group"
      title={isCollapsed ? 'Expand service details' : 'Collapse service details'}
    >
      <span className="text-gray-500 dark:text-gray-400 text-xs group-hover:text-gray-700 dark:group-hover:text-gray-300 transition-colors">
        {isCollapsed ? '▶' : '▼'}
      </span>
      <div className="flex items-center gap-1.5">
        <h3 className="text-sm font-bold text-gray-900 dark:text-gray-100 tracking-tight">
          {config.displayName}
        </h3>
        {isCollapsed && (isBuilding || buildFailed || logsCount > 0) && (
          <span className="relative">
            <span className="absolute -top-1 -right-1 w-2 h-2 bg-blue-500 rounded-full animate-pulse"></span>
            <span className="w-2 h-2 bg-blue-500/50 rounded-full animate-ping"></span>
          </span>
        )}
        <span
          className={`px-1.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${
            currentEnv === 'local'
              ? 'bg-green-600 text-white'
              : currentEnv === 'dev'
              ? 'bg-blue-600 text-white'
              : 'bg-red-600 text-white'
          }`}
        >
          {currentEnv === 'local' ? 'LOCAL' : currentEnv === 'dev' ? 'DEV' : 'PROD'}
        </span>
        {currentEnv !== 'local' && environmentStatus && (
          <span
            className="text-xs"
            title={`Environment status: ${environmentStatus[currentEnv] || 'unknown'}`}
          >
            {environmentStatus[currentEnv] === 'online'
              ? '🟢'
              : environmentStatus[currentEnv] === 'offline'
              ? '🔴'
              : environmentStatus[currentEnv] === 'checking'
              ? '🟡'
              : '⚪'}
          </span>
        )}
      </div>
    </button>
  );
}

