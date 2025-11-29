import { VIEWS, type View } from '../types/constants';
import CosmosExplorer from './CosmosExplorer';
import Dashboard from './Dashboard';
import InfrastructurePanel from './InfrastructurePanel';
import MigrationManager from './MigrationManager';
import ServiceManager from './ServiceManager';

interface AppContentProps {
  currentView: View;
  onNavigate: (view: string) => void;
}

export function AppContent({ currentView, onNavigate }: AppContentProps) {
  switch (currentView) {
    case VIEWS.SERVICES:
      return <ServiceManager />;
    case VIEWS.DASHBOARD:
      return <Dashboard onNavigate={onNavigate} />;
    case VIEWS.COSMOS:
      return <CosmosExplorer />;
    case VIEWS.MIGRATION:
      return <MigrationManager />;
    case VIEWS.INFRASTRUCTURE:
      return <InfrastructurePanel />;
    case VIEWS.TEST:
      return (
        <div className="p-6">
          <h2 className="text-xl font-bold mb-4">Test Panel</h2>
          <p className="text-gray-600 dark:text-gray-400">Test runner and test results will be displayed here.</p>
        </div>
      );
    default:
      return <ServiceManager />;
  }
}

