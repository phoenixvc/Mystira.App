import { useState } from 'react';
import ExportPanel from './ExportPanel';
import StatisticsPanel from './StatisticsPanel';

type Tab = 'export' | 'statistics';

function CosmosExplorer() {
  const [activeTab, setActiveTab] = useState<Tab>('export');

  return (
    <div className="p-8">
      <div className="max-w-6xl mx-auto">
        <div className="mb-8">
          <h2 className="text-3xl font-bold text-gray-900 mb-2">
            Cosmos Explorer
          </h2>
          <p className="text-gray-600">
            Export game sessions and view scenario statistics
          </p>
        </div>

        {/* Tabs */}
        <div className="border-b border-gray-200 mb-6">
          <nav className="flex space-x-8">
            <button
              onClick={() => setActiveTab('export')}
              className={`pb-4 px-1 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 'export'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              📤 Export Sessions
            </button>
            <button
              onClick={() => setActiveTab('statistics')}
              className={`pb-4 px-1 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 'statistics'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              📊 Statistics
            </button>
          </nav>
        </div>

        {/* Tab Content */}
        {activeTab === 'export' && <ExportPanel />}
        {activeTab === 'statistics' && <StatisticsPanel />}
      </div>
    </div>
  );
}

export default CosmosExplorer;
