import { useState, useEffect } from 'react';
import { invoke } from '@tauri-apps/api/tauri';

interface AccountScenarioStatistics {
  accountId: string;
  accountEmail: string;
  accountAlias: string;
  sessionCount: number;
  completedSessions: number;
}

interface ScenarioStatistics {
  scenarioId: string;
  scenarioName: string;
  totalSessions: number;
  completedSessions: number;
  accountStatistics: AccountScenarioStatistics[];
}

interface CommandResponse {
  success: boolean;
  result?: ScenarioStatistics[];
  message?: string;
  error?: string;
}

function StatisticsPanel() {
  const [loading, setLoading] = useState(false);
  const [statistics, setStatistics] = useState<ScenarioStatistics[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [expandedScenario, setExpandedScenario] = useState<string | null>(null);

  const fetchStatistics = async () => {
    setLoading(true);
    setError(null);

    try {
      const response: CommandResponse = await invoke('cosmos_stats');

      if (response.success && response.result) {
        setStatistics(response.result);
      } else {
        setError(response.error || 'Failed to load statistics');
      }
    } catch (err) {
      setError(String(err));
    } finally {
      setLoading(false);
    }
  };

  const calculateCompletionRate = (completed: number, total: number): number => {
    if (total === 0) return 0;
    return Math.round((completed / total) * 100);
  };

  const toggleScenario = (scenarioId: string) => {
    setExpandedScenario(expandedScenario === scenarioId ? null : scenarioId);
  };

  useEffect(() => {
    fetchStatistics();
  }, []);

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
          Scenario Completion Statistics
        </h3>
        <button
          onClick={fetchStatistics}
          disabled={loading}
          className="px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center"
        >
          {loading ? (
            <>
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
              Loading...
            </>
          ) : (
            <>🔄 Refresh</>
          )}
        </button>
      </div>

      {/* Loading State */}
      {loading && statistics.length === 0 && (
        <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-8 text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400 mx-auto mb-4"></div>
          <p className="text-blue-800 dark:text-blue-200">Loading statistics...</p>
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-6 mb-6">
          <h3 className="text-lg font-semibold text-red-900 dark:text-red-300 mb-2">❌ Error</h3>
          <p className="text-red-800 dark:text-red-200">{error}</p>
          <button
            onClick={fetchStatistics}
            className="mt-3 px-4 py-2 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
          >
            Retry
          </button>
        </div>
      )}

      {/* Empty State */}
      {!loading && !error && statistics.length === 0 && (
        <div className="bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-8 text-center">
          <div className="text-6xl mb-4">📊</div>
          <p className="text-gray-600 dark:text-gray-400 text-lg mb-2">No Statistics Available</p>
          <p className="text-gray-500 dark:text-gray-500 text-sm">
            No game sessions found in the database.
          </p>
        </div>
      )}

      {/* Statistics Cards */}
      {!loading && statistics.length > 0 && (
        <div className="space-y-4">
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
            <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <div className="text-sm text-gray-500 dark:text-gray-400 mb-1">Total Scenarios</div>
              <div className="text-3xl font-bold text-gray-900 dark:text-white">
                {statistics.length}
              </div>
            </div>
            <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <div className="text-sm text-gray-500 dark:text-gray-400 mb-1">Total Sessions</div>
              <div className="text-3xl font-bold text-blue-600 dark:text-blue-400">
                {statistics.reduce((sum, s) => sum + s.totalSessions, 0)}
              </div>
            </div>
            <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
              <div className="text-sm text-gray-500 dark:text-gray-400 mb-1">Completed Sessions</div>
              <div className="text-3xl font-bold text-green-600 dark:text-green-400">
                {statistics.reduce((sum, s) => sum + s.completedSessions, 0)}
              </div>
            </div>
          </div>

          {/* Scenario Details */}
          {statistics.map((scenario) => {
            const completionRate = calculateCompletionRate(
              scenario.completedSessions,
              scenario.totalSessions
            );
            const isExpanded = expandedScenario === scenario.scenarioId;

            return (
              <div
                key={scenario.scenarioId}
                className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden"
              >
                {/* Scenario Header */}
                <button
                  onClick={() => toggleScenario(scenario.scenarioId)}
                  className="w-full p-6 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                >
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                        {scenario.scenarioName || 'Unnamed Scenario'}
                      </h4>
                      <div className="flex items-center gap-6 text-sm">
                        <span className="text-gray-600 dark:text-gray-400">
                          <strong>{scenario.totalSessions}</strong> total sessions
                        </span>
                        <span className="text-green-600 dark:text-green-400">
                          <strong>{scenario.completedSessions}</strong> completed
                        </span>
                        <span
                          className={`font-semibold ${
                            completionRate >= 80
                              ? 'text-green-600'
                              : completionRate >= 50
                              ? 'text-yellow-600'
                              : 'text-red-600'
                          }`}
                        >
                          {completionRate}% completion rate
                        </span>
                      </div>
                    </div>
                    <div className="ml-4 text-gray-400 dark:text-gray-500">
                      {isExpanded ? '▼' : '▶'}
                    </div>
                  </div>

                  {/* Progress Bar */}
                  <div className="mt-4 bg-gray-200 dark:bg-gray-700 rounded-full h-2 overflow-hidden">
                    <div
                      className="bg-green-500 h-full transition-all duration-300"
                      style={{ width: `${completionRate}%` }}
                    ></div>
                  </div>
                </button>

                {/* Account Breakdown (Expandable) */}
                {isExpanded && scenario.accountStatistics.length > 0 && (
                  <div className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50 p-6">
                    <h5 className="font-semibold text-gray-900 dark:text-white mb-4">
                      Account Breakdown
                    </h5>
                    <div className="space-y-3">
                      {scenario.accountStatistics.map((account) => {
                        const accountCompletionRate = calculateCompletionRate(
                          account.completedSessions,
                          account.sessionCount
                        );

                        return (
                          <div
                            key={account.accountId}
                            className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4"
                          >
                            <div className="flex justify-between items-start mb-2">
                              <div>
                                <div className="font-medium text-gray-900 dark:text-white">
                                  {account.accountAlias || 'Unknown User'}
                                </div>
                                <div className="text-sm text-gray-500 dark:text-gray-400">
                                  {account.accountEmail}
                                </div>
                              </div>
                              <div className="text-right">
                                <div className="text-sm text-gray-600 dark:text-gray-400">
                                  {account.completedSessions} / {account.sessionCount}
                                </div>
                                <div
                                  className={`text-sm font-semibold ${
                                    accountCompletionRate >= 80
                                      ? 'text-green-600'
                                      : accountCompletionRate >= 50
                                      ? 'text-yellow-600'
                                      : 'text-red-600'
                                  }`}
                                >
                                  {accountCompletionRate}%
                                </div>
                              </div>
                            </div>
                            <div className="bg-gray-200 dark:bg-gray-700 rounded-full h-1.5 overflow-hidden">
                              <div
                                className={`h-full transition-all duration-300 ${
                                  accountCompletionRate >= 80
                                    ? 'bg-green-500'
                                    : accountCompletionRate >= 50
                                    ? 'bg-yellow-500'
                                    : 'bg-red-500'
                                }`}
                                style={{ width: `${accountCompletionRate}%` }}
                              ></div>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}

                {isExpanded && scenario.accountStatistics.length === 0 && (
                  <div className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50 p-6 text-center text-gray-500 dark:text-gray-400">
                    No account data available for this scenario
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Info Box */}
      {statistics.length > 0 && (
        <div className="mt-6 bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
          <h4 className="font-semibold text-blue-900 dark:text-blue-300 mb-2">ℹ️ Legend</h4>
          <div className="text-sm text-blue-800 dark:text-blue-200 space-y-1">
            <div className="flex items-center gap-2">
              <span className="text-green-600 font-semibold">Green (80%+):</span>
              <span>High completion rate</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-yellow-600 font-semibold">Yellow (50-79%):</span>
              <span>Medium completion rate</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-red-600 font-semibold">Red (&lt;50%):</span>
              <span>Low completion rate</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default StatisticsPanel;
