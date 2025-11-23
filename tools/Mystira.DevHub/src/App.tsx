import { useState } from 'react';
import './App.css';
import CosmosExplorer from './components/CosmosExplorer';
import Dashboard from './components/Dashboard';
import InfrastructurePanel from './components/InfrastructurePanel';
import MigrationManager from './components/MigrationManager';
import ServiceManager from './components/ServiceManager';
import { useDarkMode } from './hooks/useDarkMode';

type View = 'dashboard' | 'cosmos' | 'migration' | 'infrastructure' | 'services';

function App() {
  const [currentView, setCurrentView] = useState<View>('services');
  const { isDark, toggleDarkMode } = useDarkMode();

  // Wrapper function for type compatibility
  const handleNavigate = (view: string) => {
    setCurrentView(view as View);
  };

  return (
    <div className="flex h-screen bg-gray-50 dark:bg-gray-900">
      {/* Sidebar */}
      <aside className="w-64 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 p-4">
        <div className="mb-8">
          <div className="flex items-center justify-between mb-2">
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Mystira DevHub</h1>
            <button
              onClick={toggleDarkMode}
              className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
            >
              {isDark ? '☀️' : '🌙'}
            </button>
          </div>
          <p className="text-sm text-gray-500 dark:text-gray-400">Development Operations</p>
        </div>

        <nav className="space-y-1">
          <button
            onClick={() => setCurrentView('dashboard')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'dashboard'
                ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
            }`}
          >
            Dashboard
          </button>

          <button
            onClick={() => setCurrentView('cosmos')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'cosmos'
                ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
            }`}
          >
            Cosmos Explorer
          </button>

          <button
            onClick={() => setCurrentView('migration')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'migration'
                ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
            }`}
          >
            Migration Manager
          </button>

          <button
            onClick={() => setCurrentView('infrastructure')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'infrastructure'
                ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
            }`}
          >
            Infrastructure
          </button>

          <button
            onClick={() => setCurrentView('services')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'services'
                ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
            }`}
          >
            Services
          </button>
        </nav>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto bg-gray-50 dark:bg-gray-900">
        {currentView === 'dashboard' && <Dashboard onNavigate={handleNavigate} />}

        {currentView === 'cosmos' && <CosmosExplorer />}

        {currentView === 'migration' && <MigrationManager />}

        {currentView === 'infrastructure' && <InfrastructurePanel />}

        {currentView === 'services' && <ServiceManager />}
      </main>
    </div>
  );
}

export default App;
