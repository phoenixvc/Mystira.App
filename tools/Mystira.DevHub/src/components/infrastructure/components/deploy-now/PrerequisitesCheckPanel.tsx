import { useState, useEffect } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import {
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Loader2,
  RefreshCw,
  ExternalLink,
  Copy,
  Check,
  Terminal,
  Github,
  Cloud,
  Package,
} from 'lucide-react';
import type { CommandResponse } from '../../../../types';

interface PrerequisiteStatus {
  name: string;
  status: 'checking' | 'available' | 'missing' | 'warning';
  message?: string;
  helpUrl?: string;
  helpCommand?: string;
}

interface PrerequisitesCheckPanelProps {
  onAllReady?: (ready: boolean) => void;
  onAzureAccountChange?: (account: { name: string; id: string } | null) => void;
}

export function PrerequisitesCheckPanel({
  onAllReady,
  onAzureAccountChange,
}: PrerequisitesCheckPanelProps) {
  const [prerequisites, setPrerequisites] = useState<PrerequisiteStatus[]>([
    { name: 'Azure CLI', status: 'checking' },
    { name: 'Azure Login', status: 'checking' },
    { name: 'GitHub PAT', status: 'checking' },
    { name: 'SWA CLI', status: 'checking' },
    { name: 'Node.js/npm', status: 'checking' },
  ]);
  const [isChecking, setIsChecking] = useState(true);
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [azureAccount, setAzureAccount] = useState<{ name: string; id: string } | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  const handleCopy = async (value: string, field: string) => {
    await navigator.clipboard.writeText(value);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const checkPrerequisites = async () => {
    setIsChecking(true);
    const newPrereqs: PrerequisiteStatus[] = [];

    // Check Azure CLI
    try {
      const azureCliResponse = await invoke<CommandResponse>('check_azure_cli');
      if (azureCliResponse.success) {
        newPrereqs.push({
          name: 'Azure CLI',
          status: 'available',
          message: 'Azure CLI is installed',
        });
      } else {
        newPrereqs.push({
          name: 'Azure CLI',
          status: 'missing',
          message: 'Azure CLI not found',
          helpUrl: 'https://docs.microsoft.com/en-us/cli/azure/install-azure-cli',
        });
      }
    } catch {
      newPrereqs.push({
        name: 'Azure CLI',
        status: 'missing',
        message: 'Failed to check Azure CLI',
        helpUrl: 'https://docs.microsoft.com/en-us/cli/azure/install-azure-cli',
      });
    }

    // Check Azure Login
    try {
      const loginResponse = await invoke<CommandResponse>('check_azure_login');
      if (loginResponse.success && loginResponse.result) {
        const account = loginResponse.result as { name: string; id: string };
        setAzureAccount(account);
        onAzureAccountChange?.(account);
        newPrereqs.push({
          name: 'Azure Login',
          status: 'available',
          message: `Logged in as: ${account.name}`,
        });
      } else {
        setAzureAccount(null);
        onAzureAccountChange?.(null);
        newPrereqs.push({
          name: 'Azure Login',
          status: 'missing',
          message: 'Not logged in to Azure',
          helpCommand: 'az login --use-device-code',
        });
      }
    } catch {
      setAzureAccount(null);
      onAzureAccountChange?.(null);
      newPrereqs.push({
        name: 'Azure Login',
        status: 'missing',
        message: 'Failed to check Azure login',
        helpCommand: 'az login --use-device-code',
      });
    }

    // Check GitHub PAT
    try {
      const patResponse = await invoke<CommandResponse>('check_github_pat');
      if (patResponse.success) {
        newPrereqs.push({
          name: 'GitHub PAT',
          status: 'available',
          message: 'GitHub PAT is configured',
        });
      } else {
        newPrereqs.push({
          name: 'GitHub PAT',
          status: 'warning',
          message: 'No GitHub PAT found (optional for GitHub integration)',
          helpUrl: 'https://github.com/settings/tokens',
        });
      }
    } catch {
      newPrereqs.push({
        name: 'GitHub PAT',
        status: 'warning',
        message: 'GitHub PAT not configured (optional)',
        helpUrl: 'https://github.com/settings/tokens',
      });
    }

    // Check SWA CLI
    try {
      const swaResponse = await invoke<CommandResponse>('check_swa_cli');
      if (swaResponse.success) {
        newPrereqs.push({
          name: 'SWA CLI',
          status: 'available',
          message: 'SWA CLI is installed',
        });
      } else {
        newPrereqs.push({
          name: 'SWA CLI',
          status: 'warning',
          message: 'SWA CLI not found (optional)',
          helpCommand: 'npm install -g @azure/static-web-apps-cli',
        });
      }
    } catch {
      newPrereqs.push({
        name: 'SWA CLI',
        status: 'warning',
        message: 'SWA CLI not installed (optional)',
        helpCommand: 'npm install -g @azure/static-web-apps-cli',
      });
    }

    // Check npm
    try {
      const npmResponse = await invoke<CommandResponse>('check_npm');
      if (npmResponse.success) {
        const version = npmResponse.result as string;
        newPrereqs.push({
          name: 'Node.js/npm',
          status: 'available',
          message: `npm v${version}`,
        });
      } else {
        newPrereqs.push({
          name: 'Node.js/npm',
          status: 'warning',
          message: 'npm not found (needed for SWA CLI)',
          helpUrl: 'https://nodejs.org/',
        });
      }
    } catch {
      newPrereqs.push({
        name: 'Node.js/npm',
        status: 'warning',
        message: 'npm not installed',
        helpUrl: 'https://nodejs.org/',
      });
    }

    setPrerequisites(newPrereqs);
    setIsChecking(false);

    // Check if all required prerequisites are met (Azure CLI and Login are required)
    const allReady = newPrereqs
      .filter(p => p.name === 'Azure CLI' || p.name === 'Azure Login')
      .every(p => p.status === 'available');
    onAllReady?.(allReady);
  };

  useEffect(() => {
    checkPrerequisites();
  }, []);

  const getStatusIcon = (status: PrerequisiteStatus['status']) => {
    switch (status) {
      case 'checking':
        return <Loader2 className="w-4 h-4 text-blue-500 animate-spin" />;
      case 'available':
        return <CheckCircle2 className="w-4 h-4 text-green-500" />;
      case 'missing':
        return <XCircle className="w-4 h-4 text-red-500" />;
      case 'warning':
        return <AlertTriangle className="w-4 h-4 text-yellow-500" />;
    }
  };

  const getPrereqIcon = (name: string) => {
    switch (name) {
      case 'Azure CLI':
      case 'Azure Login':
        return <Cloud className="w-4 h-4" />;
      case 'GitHub PAT':
        return <Github className="w-4 h-4" />;
      case 'SWA CLI':
        return <Terminal className="w-4 h-4" />;
      case 'Node.js/npm':
        return <Package className="w-4 h-4" />;
      default:
        return null;
    }
  };

  const requiredCount = prerequisites.filter(
    p => (p.name === 'Azure CLI' || p.name === 'Azure Login') && p.status === 'available'
  ).length;
  const totalRequired = 2;
  const allRequiredMet = requiredCount === totalRequired;

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Header */}
      <div
        className="flex items-center justify-between px-4 py-3 bg-gray-50 dark:bg-gray-700/50 cursor-pointer"
        onClick={() => setShowDetails(!showDetails)}
      >
        <div className="flex items-center gap-3">
          <div className={`w-2 h-2 rounded-full ${allRequiredMet ? 'bg-green-500' : 'bg-yellow-500'}`} />
          <span className="font-medium text-gray-900 dark:text-white text-sm">
            Prerequisites
          </span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            ({requiredCount}/{totalRequired} required)
          </span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              checkPrerequisites();
            }}
            disabled={isChecking}
            className="p-1 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 disabled:opacity-50"
            title="Refresh"
          >
            <RefreshCw className={`w-4 h-4 ${isChecking ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* Details */}
      {showDetails && (
        <div className="p-4 space-y-3">
          {prerequisites.map((prereq) => (
            <div
              key={prereq.name}
              className={`flex items-center justify-between p-3 rounded-lg border ${
                prereq.status === 'available'
                  ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800'
                  : prereq.status === 'missing'
                  ? 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800'
                  : prereq.status === 'warning'
                  ? 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800'
                  : 'bg-gray-50 dark:bg-gray-700 border-gray-200 dark:border-gray-600'
              }`}
            >
              <div className="flex items-center gap-3">
                {getStatusIcon(prereq.status)}
                <div className="flex items-center gap-2 text-gray-600 dark:text-gray-300">
                  {getPrereqIcon(prereq.name)}
                  <span className="font-medium text-sm">{prereq.name}</span>
                </div>
              </div>
              <div className="flex items-center gap-2">
                {prereq.message && (
                  <span className="text-xs text-gray-500 dark:text-gray-400 max-w-48 truncate">
                    {prereq.message}
                  </span>
                )}
                {prereq.helpUrl && (
                  <a
                    href={prereq.helpUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="p-1 text-blue-500 hover:text-blue-700"
                    title="Learn more"
                  >
                    <ExternalLink className="w-3 h-3" />
                  </a>
                )}
                {prereq.helpCommand && (
                  <button
                    onClick={() => handleCopy(prereq.helpCommand!, prereq.name)}
                    className="p-1 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
                    title={`Copy: ${prereq.helpCommand}`}
                  >
                    {copiedField === prereq.name ? (
                      <Check className="w-3 h-3 text-green-500" />
                    ) : (
                      <Copy className="w-3 h-3" />
                    )}
                  </button>
                )}
              </div>
            </div>
          ))}

          {/* Azure Account Info */}
          {azureAccount && (
            <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
              <div className="text-sm">
                <span className="text-blue-700 dark:text-blue-300 font-medium">Azure Account: </span>
                <span className="text-blue-900 dark:text-blue-200">{azureAccount.name}</span>
              </div>
              <div className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                Subscription: {azureAccount.id}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Collapsed summary */}
      {!showDetails && !allRequiredMet && (
        <div className="px-4 py-2 text-xs text-yellow-700 dark:text-yellow-300 bg-yellow-50 dark:bg-yellow-900/20">
          Click to expand and see required setup steps
        </div>
      )}
    </div>
  );
}

export default PrerequisitesCheckPanel;
