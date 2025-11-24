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
    <div className="flex items-center gap-2">
      <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">Environment:</span>
      <div className="flex items-center gap-0.5 border-2 border-gray-300 dark:border-gray-600 rounded-lg overflow-hidden shadow-sm">
        <button
          onClick={() => onSwitch('local')}
          disabled={isRunning}
          className={`px-3 py-1.5 text-xs font-semibold transition-all ${
            currentEnv === 'local'
              ? 'bg-green-500 text-white shadow-md'
              : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-green-50 dark:hover:bg-green-900/20'
          } ${isRunning ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
          title={isRunning ? 'Stop service to switch environment' : '🏠 Switch to local environment (localhost)'}
        >
          🏠 Local
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
              className={`px-3 py-1.5 text-xs font-semibold transition-all ${
                currentEnv === 'dev'
                  ? 'bg-blue-500 text-white shadow-md'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-blue-50 dark:hover:bg-blue-900/20'
              } ${isRunning ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'} ${
                devStatus === 'offline' ? 'ring-2 ring-red-400' : ''
              }`}
              title={
                isRunning 
                  ? 'Stop service to switch environment' 
                  : `🧪 Switch to dev environment\n${envUrls.dev}\nStatus: ${devStatus === 'online' ? '🟢 Online' : devStatus === 'offline' ? '🔴 Offline' : devStatus === 'checking' ? '🟡 Checking...' : '⚪ Unknown'}`
              }
            >
              🧪 Dev {devStatus === 'online' ? '🟢' : devStatus === 'offline' ? '🔴' : devStatus === 'checking' ? '🟡' : ''}
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
              className={`px-3 py-1.5 text-xs font-semibold transition-all ${
                currentEnv === 'prod'
                  ? 'bg-red-600 text-white shadow-md animate-pulse'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-red-50 dark:hover:bg-red-900/20'
              } ${isRunning ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'} ${
                prodStatus === 'offline' ? 'ring-2 ring-red-500' : ''
              }`}
              title={
                isRunning 
                  ? 'Stop service to switch environment' 
                  : `⚠️ Switch to PRODUCTION environment (WARNING: Shows danger dialog)\n${envUrls.prod}\nStatus: ${prodStatus === 'online' ? '🟢 Online' : prodStatus === 'offline' ? '🔴 Offline' : prodStatus === 'checking' ? '🟡 Checking...' : '⚪ Unknown'}`
              }
            >
              ⚠️ PROD {prodStatus === 'online' ? '🟢' : prodStatus === 'offline' ? '🔴' : prodStatus === 'checking' ? '🟡' : ''}
            </button>
          </>
        )}
      </div>
    </div>
  );
}

