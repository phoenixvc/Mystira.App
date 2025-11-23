import { useState } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import { save } from '@tauri-apps/api/dialog';

interface CommandResponse {
  success: boolean;
  result?: {
    rowCount?: number;
    outputPath?: string;
  };
  message?: string;
  error?: string;
}

function ExportPanel() {
  const [loading, setLoading] = useState(false);
  const [lastResponse, setLastResponse] = useState<CommandResponse | null>(null);
  const [outputPath, setOutputPath] = useState('');

  const handleSelectOutputPath = async () => {
    try {
      const filePath = await save({
        defaultPath: 'game-sessions.csv',
        filters: [{
          name: 'CSV',
          extensions: ['csv']
        }]
      });

      if (filePath) {
        setOutputPath(filePath);
      }
    } catch (error) {
      console.error('Failed to select file:', error);
    }
  };

  const handleExport = async () => {
    if (!outputPath) {
      alert('Please select an output file path');
      return;
    }

    setLoading(true);
    setLastResponse(null);

    try {
      const response: CommandResponse = await invoke('cosmos_export', {
        outputPath,
      });

      setLastResponse(response);
    } catch (error) {
      setLastResponse({
        success: false,
        error: String(error),
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <div className="bg-white border border-gray-200 rounded-lg p-6 mb-6">
        <h3 className="text-xl font-semibold text-gray-900 mb-4">
          Export Game Sessions to CSV
        </h3>

        <div className="space-y-4">
          {/* Output Path Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Output File Path
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={outputPath}
                onChange={(e) => setOutputPath(e.target.value)}
                placeholder="Select or enter output path..."
                className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              <button
                onClick={handleSelectOutputPath}
                className="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 transition-colors"
              >
                Browse...
              </button>
            </div>
            <p className="mt-1 text-sm text-gray-500">
              Choose where to save the CSV file
            </p>
          </div>

          {/* Export Button */}
          <button
            onClick={handleExport}
            disabled={loading || !outputPath}
            className="w-full px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center"
          >
            {loading ? (
              <>
                <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white mr-2"></div>
                Exporting...
              </>
            ) : (
              <>📤 Export Sessions</>
            )}
          </button>
        </div>
      </div>

      {/* Export Information */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
        <h4 className="font-semibold text-blue-900 mb-2">ℹ️ Export Information</h4>
        <ul className="text-sm text-blue-800 space-y-1 list-disc list-inside">
          <li>Exports all game sessions with account information</li>
          <li>Includes: Session ID, Scenario, Account details, Start/End times, Completion status</li>
          <li>Output format: CSV (comma-separated values)</li>
          <li>Can be opened in Excel, Google Sheets, or any spreadsheet software</li>
        </ul>
      </div>

      {/* Loading State */}
      {loading && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
          <div className="flex items-center">
            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 mr-3"></div>
            <span className="text-blue-800">Exporting game sessions...</span>
          </div>
        </div>
      )}

      {/* Response Display */}
      {lastResponse && (
        <div
          className={`rounded-lg p-6 ${
            lastResponse.success
              ? 'bg-green-50 border border-green-200'
              : 'bg-red-50 border border-red-200'
          }`}
        >
          <h3
            className={`text-lg font-semibold mb-2 ${
              lastResponse.success ? 'text-green-900' : 'text-red-900'
            }`}
          >
            {lastResponse.success ? '✅ Export Successful' : '❌ Export Failed'}
          </h3>

          {lastResponse.message && (
            <p
              className={`mb-3 ${
                lastResponse.success ? 'text-green-800' : 'text-red-800'
              }`}
            >
              {lastResponse.message}
            </p>
          )}

          {lastResponse.success && lastResponse.result && (
            <div className="bg-green-100 p-4 rounded">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <div className="text-sm text-green-700">Rows Exported</div>
                  <div className="text-2xl font-bold text-green-900">
                    {lastResponse.result.rowCount || 0}
                  </div>
                </div>
                <div>
                  <div className="text-sm text-green-700">Output File</div>
                  <div className="text-sm font-medium text-green-900 break-all">
                    {lastResponse.result.outputPath || outputPath}
                  </div>
                </div>
              </div>
            </div>
          )}

          {lastResponse.error && (
            <pre className="bg-red-100 p-3 rounded text-sm text-red-900 overflow-auto">
              {lastResponse.error}
            </pre>
          )}

          {lastResponse.result && (
            <details className="mt-3">
              <summary
                className={`cursor-pointer font-medium ${
                  lastResponse.success ? 'text-green-700' : 'text-red-700'
                }`}
              >
                View Raw Response
              </summary>
              <pre
                className={`mt-2 p-3 rounded text-sm overflow-auto ${
                  lastResponse.success
                    ? 'bg-green-100 text-green-900'
                    : 'bg-red-100 text-red-900'
                }`}
              >
                {JSON.stringify(lastResponse.result, null, 2)}
              </pre>
            </details>
          )}
        </div>
      )}
    </div>
  );
}

export default ExportPanel;
