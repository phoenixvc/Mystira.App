import { useState } from 'react';
import InfrastructurePanel from './components/InfrastructurePanel';
import CosmosExplorer from './components/CosmosExplorer';
import './App.css';

type View = 'dashboard' | 'cosmos' | 'migration' | 'infrastructure';

function App() {
  const [currentView, setCurrentView] = useState<View>('cosmos');

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
        </nav>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
        {currentView === 'dashboard' && (
          <div className="p-8">
            <h2 className="text-3xl font-bold mb-4">Dashboard</h2>
            <p className="text-gray-600">Coming soon...</p>
          </div>
        )}

        {currentView === 'cosmos' && <CosmosExplorer />}

        {currentView === 'migration' && (
          <div className="p-8">
            <h2 className="text-3xl font-bold mb-4">Migration Manager</h2>
            <p className="text-gray-600">Coming soon...</p>
          </div>
        )}

        {currentView === 'infrastructure' && <InfrastructurePanel />}
      </main>
    </div>
  );
}

export default App;
