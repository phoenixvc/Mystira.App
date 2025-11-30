import { ENVIRONMENTS, VIEWS, type Environment, type View } from '../types/constants';
import type { ServiceConfig } from './services/types';

interface AppSidebarProps {
  currentView: View;
  serviceConfigs: ServiceConfig[];
  serviceEnvironments: Record<string, Environment>;
  activityBarItems: Array<{ id: string; icon: string; title: string }>;
}

export function AppSidebar({
  currentView,
  serviceConfigs,
  serviceEnvironments,
  activityBarItems,
}: AppSidebarProps) {
  const currentItem = activityBarItems.find(a => a.id === currentView);

  const getViewDescription = () => {
    switch (currentView) {
      case VIEWS.SERVICES:
        return 'Manage local and deployed services';
      case VIEWS.DASHBOARD:
        return 'Overview and quick actions';
      case VIEWS.COSMOS:
        return 'Explore Azure Cosmos DB';
      case VIEWS.MIGRATION:
        return 'Database migration tools';
      case VIEWS.INFRASTRUCTURE:
        return 'Deploy and manage Azure resources';
      case VIEWS.TEST:
        return 'Run and view test results';
      default:
        return '';
    }
  };

  return (
    <div className="text-xs text-gray-300">
      {/* View-specific sidebar content */}
      <div className="p-3 border-b border-gray-700">
        <div className="flex items-center gap-2 mb-2">
          <span className="text-lg">{currentItem?.icon}</span>
          <span className="font-semibold text-white uppercase tracking-wide">
            {currentItem?.title}
          </span>
        </div>
        <p className="text-gray-400 text-[10px]">
          {getViewDescription()}
        </p>
      </div>

      {/* Quick actions */}
      <div className="p-3">
        <div className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mb-2">
          Quick Actions
        </div>
        <div className="space-y-1">
          {currentView === VIEWS.SERVICES && (
            <>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>▶</span> Start All Services
              </button>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>⏹</span> Stop All Services
              </button>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>🔄</span> Refresh Status
              </button>
            </>
          )}
          {currentView === VIEWS.INFRASTRUCTURE && (
            <>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>☁️</span> Deploy to Azure
              </button>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>🔍</span> View Resources
              </button>
            </>
          )}
          {currentView === VIEWS.COSMOS && (
            <>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>🔌</span> Connect Database
              </button>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>📝</span> New Query
              </button>
            </>
          )}
          {currentView === VIEWS.MIGRATION && (
            <>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>➕</span> New Migration
              </button>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>▶</span> Run Pending
              </button>
            </>
          )}
          {currentView === VIEWS.DASHBOARD && (
            <>
              <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                <span>🔄</span> Refresh Data
              </button>
            </>
          )}
        </div>
      </div>

      {/* Service list (only on services view) */}
      {currentView === VIEWS.SERVICES && serviceConfigs.length > 0 && (
        <div className="p-3 border-t border-gray-700">
          <div className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mb-2">
            Services
          </div>
          <div className="space-y-0.5">
            {serviceConfigs.map((config) => {
              const env = serviceEnvironments[config.name] || ENVIRONMENTS.LOCAL;
              return (
                <div
                  key={config.name}
                  className="flex items-center justify-between px-2 py-1 rounded hover:bg-gray-700"
                >
                  <span className="truncate">{config.displayName || config.name}</span>
                  <span className={`text-[9px] px-1.5 py-0.5 rounded ${
                    env === ENVIRONMENTS.PROD ? 'bg-red-900/50 text-red-300' :
                    env === ENVIRONMENTS.DEV ? 'bg-blue-900/50 text-blue-300' :
                    'bg-gray-700 text-gray-400'
                  }`}>
                    {env.toUpperCase()}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

