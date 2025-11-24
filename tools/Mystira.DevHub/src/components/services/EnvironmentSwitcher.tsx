import { EnvironmentStatus, EnvironmentUrls } from './types';

interface EnvironmentSwitcherProps {
  serviceName: string;
  currentEnv: 'local' | 'dev' | 'prod';
  envUrls: EnvironmentUrls;
  environmentStatus?: EnvironmentStatus;
  isRunning: boolean;
  onSwitch: (environment: 'local' | 'dev' | 'prod') => void;
}

export function EnvironmentSwitcher({
  serviceName: _serviceName,
  currentEnv,
  envUrls,
  environmentStatus,
  isRunning,
  onSwitch,
}: EnvironmentSwitcherProps) {
  const devStatus = environmentStatus?.dev;
  const prodStatus = environmentStatus?.prod;
  
  return (
    <div className="flex items-center gap-1.5">
      <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">Environment:</span>
      <div className="flex items-center gap-0 border border-gray-300 dark:border-gray-600 rounded-md overflow-hidden shadow-sm">
        <button
          onClick={() => onSwitch('local')}
          disabled={isRunning}
          className={`px-2 py-1 text-xs font-semibold transition-all flex items-center gap-1 ${
            currentEnv === 'local'
              ? 'bg-green-500 text-white shadow-md'
              : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-green-50 dark:hover:bg-green-900/20'
          } ${isRunning ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
          title={isRunning ? 'Stop service to switch environment' : '🏠 Switch to local environment (localhost)'}
        >
          <span>🏠</span>
          <span>Local</span>
          {currentEnv === 'local' && <span className="text-[8px] ml-0.5">🟢</span>}
        </button>
        {envUrls.dev && (
          <>
            <div className="w-px h-4 bg-gray-300 dark:bg-gray-600"></div>
            <button
              onClick={() => {
                if (devStatus === 'offline') {
                  if (!window.confirm(`Dev environment appears to be offline.\n\nURL: ${envUrls.dev}\n\nContinue anyway?`)) {
                    return;
                  }
                }
                onSwitch('dev');
              }}
              disabled={isRunning}
              className={`px-2 py-1 text-xs font-semibold transition-all flex items-center gap-1 ${
                currentEnv === 'dev'
                  ? 'bg-blue-500 text-white shadow-md'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-blue-50 dark:hover:bg-blue-900/20'
              } ${isRunning ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'} ${
                devStatus === 'offline' ? 'ring-1 ring-red-400' : ''
              }`}
              title={
                isRunning 
                  ? 'Stop service to switch environment' 
                  : `🧪 Switch to dev environment\n${envUrls.dev}\nStatus: ${devStatus === 'online' ? '🟢 Online' : devStatus === 'offline' ? '🔴 Offline' : devStatus === 'checking' ? '🟡 Checking...' : '⚪ Unknown'}`
              }
            >
              <span>🧪</span>
              <span>Dev</span>
              <span className={`text-[8px] ml-0.5 ${devStatus === 'online' ? 'text-green-400' : devStatus === 'offline' ? 'text-red-400' : devStatus === 'checking' ? 'text-yellow-400' : 'text-gray-400'}`}>
                {devStatus === 'online' ? '🟢' : devStatus === 'offline' ? '🔴' : devStatus === 'checking' ? '🟡' : '⚪'}
              </span>
            </button>
          </>
        )}
        {envUrls.prod && (
          <>
            <div className="w-px h-4 bg-gray-300 dark:bg-gray-600"></div>
            <button
              onClick={() => {
                if (prodStatus === 'offline') {
                  if (!window.confirm(`⚠️ PRODUCTION environment appears to be offline!\n\nURL: ${envUrls.prod}\n\nThis is dangerous. Continue anyway?`)) {
                    return;
                  }
                }
                onSwitch('prod');
              }}
              disabled={isRunning}
              className={`px-2 py-1 text-xs font-semibold transition-all flex items-center gap-1 ${
                currentEnv === 'prod'
                  ? 'bg-red-600 text-white shadow-md animate-pulse'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-red-50 dark:hover:bg-red-900/20'
              } ${isRunning ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'} ${
                prodStatus === 'offline' ? 'ring-1 ring-red-500' : ''
              }`}
              title={
                isRunning 
                  ? 'Stop service to switch environment' 
                  : `⚠️ Switch to PRODUCTION environment (WARNING: Shows danger dialog)\n${envUrls.prod}\nStatus: ${prodStatus === 'online' ? '🟢 Online' : prodStatus === 'offline' ? '🔴 Offline' : prodStatus === 'checking' ? '🟡 Checking...' : '⚪ Unknown'}`
              }
            >
              <span>⚠️</span>
              <span>PROD</span>
              <span className={`text-[8px] ml-0.5 ${prodStatus === 'online' ? 'text-green-400' : prodStatus === 'offline' ? 'text-red-400' : prodStatus === 'checking' ? 'text-yellow-400' : 'text-gray-400'}`}>
                {prodStatus === 'online' ? '🟢' : prodStatus === 'offline' ? '🔴' : prodStatus === 'checking' ? '🟡' : '⚪'}
              </span>
            </button>
          </>
        )}
      </div>
    </div>
  );
}

