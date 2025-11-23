import { useState, useEffect } from 'react';
import Dashboard from './components/Dashboard';
import InfrastructurePanel from './components/InfrastructurePanel';
import CosmosExplorer from './components/CosmosExplorer';
import MigrationManager from './components/MigrationManager';
import ServiceManager from './components/ServiceManager';
import './App.css';

type View = 'dashboard' | 'cosmos' | 'migration' | 'infrastructure' | 'services';

function App() {
  const [currentView, setCurrentView] = useState<View>('services');

  // Wrapper function for type compatibility
  const handleNavigate = (view: string) => {
    setCurrentView(view as View);
  };

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="w-64 bg-white border-r border-gray-200 p-4">
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Mystira DevHub</h1>
          <p className="text-sm text-gray-500">Development Operations</p>
        </div>

        <nav className="space-y-1">
          <button
            onClick={() => setCurrentView('dashboard')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'dashboard'
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            Dashboard
          </button>

          <button
            onClick={() => setCurrentView('cosmos')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'cosmos'
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            Cosmos Explorer
          </button>

          <button
            onClick={() => setCurrentView('migration')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'migration'
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            Migration Manager
          </button>

          <button
            onClick={() => setCurrentView('infrastructure')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'infrastructure'
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            Infrastructure
          </button>

          <button
            onClick={() => setCurrentView('services')}
            className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
              currentView === 'services'
                ? 'bg-blue-50 text-blue-700'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            Services
          </button>
        </nav>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
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
