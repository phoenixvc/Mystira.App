import { useState } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import {
  Settings,
  Shield,
  RefreshCcw,
  Link2Off,
  Key,
  Copy,
  Check,
  Loader2,
  AlertCircle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
import type { CommandResponse } from '../../../../types';

interface ConfigurationPanelProps {
  resourceGroup: string;
  region: string;
  swaName?: string;
  apiName?: string;
  adminApiName?: string;
  disabled?: boolean;
}

export function ConfigurationPanel({
  resourceGroup,
  region,
  swaName,
  apiName,
  adminApiName,
  disabled = false,
}: ConfigurationPanelProps) {
  const [expandedSections, setExpandedSections] = useState({
    cors: false,
    api: false,
    swa: false,
  });
  const [isUpdating, setIsUpdating] = useState<string | null>(null);
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [operationResult, setOperationResult] = useState<{
    type: string;
    success: boolean;
    message: string;
  } | null>(null);
  const [deploymentToken, setDeploymentToken] = useState<string | null>(null);

  const handleCopy = async (value: string, field: string) => {
    await navigator.clipboard.writeText(value);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const toggleSection = (section: keyof typeof expandedSections) => {
    setExpandedSections(prev => ({
      ...prev,
      [section]: !prev[section],
    }));
  };

  // CORS update
  const handleUpdateCors = async () => {
    if (!apiName) return;

    setIsUpdating('cors');
    setOperationResult(null);

    try {
      const response = await invoke<CommandResponse>('update_cors_settings', {
        resourceGroup,
        apiName,
        adminApiName,
      });

      setOperationResult({
        type: 'cors',
        success: response.success,
        message: response.success
          ? 'CORS settings updated successfully'
          : response.error || 'Failed to update CORS',
      });
    } catch (err) {
      setOperationResult({
        type: 'cors',
        success: false,
        message: err instanceof Error ? err.message : 'Failed to update CORS',
      });
    } finally {
      setIsUpdating(null);
    }
  };

  // API restart
  const handleRestartApis = async () => {
    if (!apiName) return;

    setIsUpdating('api');
    setOperationResult(null);

    try {
      const response = await invoke<CommandResponse>('restart_api_services', {
        resourceGroup,
        apiName,
        adminApiName,
      });

      setOperationResult({
        type: 'api',
        success: response.success,
        message: response.success
          ? 'API services restarted successfully'
          : response.error || 'Failed to restart APIs',
      });
    } catch (err) {
      setOperationResult({
        type: 'api',
        success: false,
        message: err instanceof Error ? err.message : 'Failed to restart APIs',
      });
    } finally {
      setIsUpdating(null);
    }
  };

  // Disconnect SWA CI/CD
  const handleDisconnectSwaCicd = async () => {
    if (!swaName) return;

    setIsUpdating('disconnect');
    setOperationResult(null);

    try {
      const response = await invoke<CommandResponse>('disconnect_swa_cicd', {
        resourceGroup,
        swaName,
      });

      setOperationResult({
        type: 'swa',
        success: response.success,
        message: response.success
          ? 'SWA CI/CD disconnected. You can now use GitHub Actions.'
          : response.error || 'Failed to disconnect SWA CI/CD',
      });
    } catch (err) {
      setOperationResult({
        type: 'swa',
        success: false,
        message: err instanceof Error ? err.message : 'Failed to disconnect',
      });
    } finally {
      setIsUpdating(null);
    }
  };

  // Get deployment token
  const handleGetDeploymentToken = async () => {
    if (!swaName) return;

    setIsUpdating('token');
    setOperationResult(null);

    try {
      const response = await invoke<CommandResponse>('get_swa_deployment_token', {
        resourceGroup,
        swaName,
      });

      if (response.success && response.result) {
        const token = response.result as string;
        setDeploymentToken(token);
        setOperationResult({
          type: 'token',
          success: true,
          message: 'Deployment token retrieved. Add it to GitHub Secrets.',
        });
      } else {
        setOperationResult({
          type: 'token',
          success: false,
          message: response.error || 'Failed to get deployment token',
        });
      }
    } catch (err) {
      setOperationResult({
        type: 'token',
        success: false,
        message: err instanceof Error ? err.message : 'Failed to get token',
      });
    } finally {
      setIsUpdating(null);
    }
  };

  const inferredApiName = apiName || `dev-${region === 'southafricanorth' ? 'san' : 'eus2'}-app-mystira-api`;
  const inferredAdminApiName = adminApiName || `dev-${region === 'southafricanorth' ? 'san' : 'eus2'}-app-mystira-admin-api`;
  const inferredSwaName = swaName || `dev-${region === 'southafricanorth' ? 'san' : 'eus2'}-swa-mystira-app`;

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 bg-gray-50 dark:bg-gray-700/50">
        <div className="flex items-center gap-3">
          <Settings className="w-4 h-4 text-purple-500" />
          <span className="font-medium text-gray-900 dark:text-white text-sm">
            Configuration & Management
          </span>
        </div>
      </div>

      {/* Content */}
      <div className="p-4 space-y-3">
        {/* CORS Settings */}
        <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <button
            onClick={() => toggleSection('cors')}
            className="w-full flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700/50 text-left"
          >
            <div className="flex items-center gap-2">
              <Shield className="w-4 h-4 text-blue-500" />
              <span className="text-sm font-medium text-gray-900 dark:text-white">
                CORS Settings
              </span>
            </div>
            {expandedSections.cors ? (
              <ChevronDown className="w-4 h-4 text-gray-500" />
            ) : (
              <ChevronRight className="w-4 h-4 text-gray-500" />
            )}
          </button>
          {expandedSections.cors && (
            <div className="p-3 space-y-3">
              <p className="text-xs text-gray-500 dark:text-gray-400">
                Update CORS allowed origins for API services
              </p>
              <div className="text-xs font-mono text-gray-600 dark:text-gray-300 bg-gray-50 dark:bg-gray-700 p-2 rounded">
                <div>API: {inferredApiName}</div>
                <div>Admin API: {inferredAdminApiName}</div>
              </div>
              <button
                onClick={handleUpdateCors}
                disabled={isUpdating !== null || disabled}
                className="w-full px-3 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {isUpdating === 'cors' ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Shield className="w-4 h-4" />
                )}
                Update CORS Settings
              </button>
            </div>
          )}
        </div>

        {/* API Services */}
        <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <button
            onClick={() => toggleSection('api')}
            className="w-full flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700/50 text-left"
          >
            <div className="flex items-center gap-2">
              <RefreshCcw className="w-4 h-4 text-green-500" />
              <span className="text-sm font-medium text-gray-900 dark:text-white">
                API Services
              </span>
            </div>
            {expandedSections.api ? (
              <ChevronDown className="w-4 h-4 text-gray-500" />
            ) : (
              <ChevronRight className="w-4 h-4 text-gray-500" />
            )}
          </button>
          {expandedSections.api && (
            <div className="p-3 space-y-3">
              <p className="text-xs text-gray-500 dark:text-gray-400">
                Restart API services to apply configuration changes
              </p>
              <button
                onClick={handleRestartApis}
                disabled={isUpdating !== null || disabled}
                className="w-full px-3 py-2 text-sm font-medium text-white bg-green-600 rounded-lg hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {isUpdating === 'api' ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <RefreshCcw className="w-4 h-4" />
                )}
                Restart API Services
              </button>
            </div>
          )}
        </div>

        {/* Static Web App Settings */}
        <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <button
            onClick={() => toggleSection('swa')}
            className="w-full flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700/50 text-left"
          >
            <div className="flex items-center gap-2">
              <Link2Off className="w-4 h-4 text-orange-500" />
              <span className="text-sm font-medium text-gray-900 dark:text-white">
                Static Web App
              </span>
            </div>
            {expandedSections.swa ? (
              <ChevronDown className="w-4 h-4 text-gray-500" />
            ) : (
              <ChevronRight className="w-4 h-4 text-gray-500" />
            )}
          </button>
          {expandedSections.swa && (
            <div className="p-3 space-y-3">
              <p className="text-xs text-gray-500 dark:text-gray-400">
                Manage Static Web App deployment settings
              </p>
              <div className="text-xs font-mono text-gray-600 dark:text-gray-300 bg-gray-50 dark:bg-gray-700 p-2 rounded">
                SWA: {inferredSwaName}
              </div>

              <div className="flex gap-2">
                <button
                  onClick={handleDisconnectSwaCicd}
                  disabled={isUpdating !== null || disabled}
                  className="flex-1 px-3 py-2 text-sm font-medium text-white bg-orange-600 rounded-lg hover:bg-orange-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                >
                  {isUpdating === 'disconnect' ? (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  ) : (
                    <Link2Off className="w-4 h-4" />
                  )}
                  Disconnect CI/CD
                </button>
                <button
                  onClick={handleGetDeploymentToken}
                  disabled={isUpdating !== null || disabled}
                  className="flex-1 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                >
                  {isUpdating === 'token' ? (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  ) : (
                    <Key className="w-4 h-4" />
                  )}
                  Get Token
                </button>
              </div>

              {deploymentToken && (
                <div className="mt-3 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-xs font-medium text-yellow-800 dark:text-yellow-200">
                      Deployment Token
                    </span>
                    <button
                      onClick={() => handleCopy(deploymentToken, 'token')}
                      className="p-1 text-yellow-600 hover:text-yellow-800 dark:text-yellow-400 dark:hover:text-yellow-200"
                    >
                      {copiedField === 'token' ? (
                        <Check className="w-4 h-4" />
                      ) : (
                        <Copy className="w-4 h-4" />
                      )}
                    </button>
                  </div>
                  <div className="font-mono text-xs text-yellow-700 dark:text-yellow-300 break-all bg-yellow-100 dark:bg-yellow-900/30 p-2 rounded">
                    {deploymentToken.substring(0, 50)}...
                  </div>
                  <p className="mt-2 text-xs text-yellow-600 dark:text-yellow-400">
                    Add this to GitHub Secrets as: AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_MYSTIRA_APP
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Operation Result */}
        {operationResult && (
          <div
            className={`p-3 rounded-lg border ${
              operationResult.success
                ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800'
                : 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800'
            }`}
          >
            <div className="flex items-center gap-2 text-sm">
              {operationResult.success ? (
                <CheckCircle2 className="w-4 h-4 text-green-500" />
              ) : (
                <AlertCircle className="w-4 h-4 text-red-500" />
              )}
              <span
                className={
                  operationResult.success
                    ? 'text-green-700 dark:text-green-300'
                    : 'text-red-700 dark:text-red-300'
                }
              >
                {operationResult.message}
              </span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default ConfigurationPanel;
