import { EnvironmentStatus, ServiceConfig } from './types';

interface EnvironmentBannerProps {
  serviceConfigs: ServiceConfig[];
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  environmentStatus: Record<string, EnvironmentStatus>;
  getEnvironmentInfo: (serviceName: string) => {
    environment: 'local' | 'dev' | 'prod';
    url: string;
  };
  onResetAll: () => void;
}

export function EnvironmentBanner({
  serviceConfigs,
  serviceEnvironments: _serviceEnvironments,
  environmentStatus,
  getEnvironmentInfo,
  onResetAll,
}: EnvironmentBannerProps) {
  const environments = serviceConfigs.map(config => {
    const envInfo = getEnvironmentInfo(config.name);
    const status = environmentStatus[config.name]?.[envInfo.environment as 'dev' | 'prod'];
    return { 
      name: config.name, 
      displayName: config.displayName, 
      environment: envInfo.environment,
      status,
      url: envInfo.url,
    };
  });
  
  const hasProd = environments.some(e => e.environment === 'prod');
  const hasDev = environments.some(e => e.environment === 'dev');
  const allLocal = environments.every(e => e.environment === 'local');
  
  const bannerColor = hasProd 
    ? 'border-red-500 bg-red-50 dark:bg-red-900/20 shadow-red-200 dark:shadow-red-900/50' 
    : hasDev 
    ? 'border-yellow-400 bg-yellow-50 dark:bg-yellow-900/20 shadow-yellow-200 dark:shadow-yellow-900/50'
    : 'border-green-400 bg-green-50 dark:bg-green-900/20 shadow-green-200 dark:shadow-green-900/50';
  const bannerTextColor = hasProd
    ? 'text-red-800 dark:text-red-200'
    : hasDev
    ? 'text-yellow-800 dark:text-yellow-200'
    : 'text-green-800 dark:text-green-200';
  
  // Compact version for header
  const isCompact = true;
  
  if (isCompact) {
    return (
      <div className="flex items-center gap-2">
        {environments.map(env => {
          const envColors = {
            local: 'bg-green-500 text-white',
            dev: 'bg-blue-500 text-white',
            prod: 'bg-red-600 text-white',
          };
          const statusIcon = env.status === 'online' ? '🟢' : 
                            env.status === 'offline' ? '🔴' : 
                            env.status === 'checking' ? '🟡' : '';
          const statusText = env.status === 'online' ? ' (Online)' : 
                            env.status === 'offline' ? ' (Offline)' : 
                            env.status === 'checking' ? ' (Checking...)' : '';
          return (
            <div key={env.name} className="flex items-center gap-1.5">
              <span className="text-xs font-medium text-gray-600 dark:text-gray-400">{env.displayName}:</span>
              <span className={`px-1.5 py-0.5 rounded text-[10px] font-bold uppercase ${envColors[env.environment]} ${env.environment === 'prod' ? 'animate-pulse' : ''}`} title={env.environment !== 'local' ? `${env.url}${statusText}` : 'Local environment'}>
                {env.environment === 'local' ? '🏠' : env.environment === 'dev' ? '🧪' : '⚠️'} {env.environment === 'local' ? 'LOCAL' : env.environment === 'dev' ? 'DEV' : 'PROD'} {statusIcon}
              </span>
            </div>
          );
        })}
        {!allLocal && (
          <button
            onClick={onResetAll}
            className="px-1.5 py-0.5 text-[10px] bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-600 font-medium transition-colors"
            title="Reset all services to local environment"
          >
            🔄 Reset
          </button>
        )}
      </div>
    );
  }
  
  return (
    <div className={`mb-6 p-4 rounded-lg border-2 ${bannerColor} sticky top-0 z-10 shadow-lg backdrop-blur-sm`}>
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-4 flex-wrap">
          <div className={`text-2xl font-bold ${bannerTextColor} flex items-center gap-2`}>
            🌍 ENVIRONMENT STATUS {hasProd ? '⚠️' : ''}
          </div>
          <div className="flex gap-3 flex-wrap">
            {environments.map(env => {
              const envColors = {
                local: 'bg-green-500 text-white shadow-green-600/50',
                dev: 'bg-blue-500 text-white shadow-blue-600/50',
                prod: 'bg-red-600 text-white shadow-red-600/50 animate-pulse',
              };
              const statusIcon = env.status === 'online' ? '🟢' : 
                                env.status === 'offline' ? '🔴' : 
                                env.status === 'checking' ? '🟡' : '';
              const statusText = env.status === 'online' ? ' (Online)' : 
                                env.status === 'offline' ? ' (Offline)' : 
                                env.status === 'checking' ? ' (Checking...)' : '';
              return (
                <div key={env.name} className="flex items-center gap-2">
                  <span className="text-sm font-semibold text-gray-700 dark:text-gray-300">{env.displayName}:</span>
                  <span className={`px-3 py-1.5 rounded font-bold text-sm shadow-md ${envColors[env.environment]}`} title={env.environment !== 'local' ? `${env.url}${statusText}` : 'Local environment'}>
                    {env.environment === 'local' ? '🏠' : env.environment === 'dev' ? '🧪' : '⚠️'} {env.environment.toUpperCase()} {statusIcon}
                  </span>
                </div>
              );
            })}
          </div>
          {!allLocal && (
            <button
              onClick={onResetAll}
              className="px-3 py-1.5 text-sm bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-600 font-medium transition-colors"
              title="Reset all services to local environment"
            >
              🔄 Reset All to Local
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

