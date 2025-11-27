import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useRef, useState } from 'react';
import type { CommandResponse } from '../types';
import type { ProjectInfo } from './ProjectDeploymentPlanner';

export interface ProjectPipeline {
  projectId: string;
  workflowFile: string;
  environment: string;
  inputs?: Record<string, string>;
}

interface ProjectDeploymentProps {
  environment: string;
  projects: ProjectInfo[];
  hasDeployedInfrastructure: boolean;
}

interface WorkflowRun {
  id: number;
  name: string;
  status: 'queued' | 'in_progress' | 'completed' | 'cancelled';
  conclusion: 'success' | 'failure' | 'cancelled' | null;
  html_url: string;
  created_at: string;
  updated_at: string;
}

function ProjectDeployment({
  environment,
  projects,
  hasDeployedInfrastructure,
}: ProjectDeploymentProps) {
  const [selectedProjects, setSelectedProjects] = useState<Set<string>>(new Set());
  const [projectPipelines, setProjectPipelines] = useState<Record<string, ProjectPipeline>>({});
  const [deploying, setDeploying] = useState(false);
  const [workflowRuns, setWorkflowRuns] = useState<Record<string, WorkflowRun>>({});
  const [workflowLogs, setWorkflowLogs] = useState<Record<string, string[]>>({});
  const [showLogs, setShowLogs] = useState<Record<string, boolean>>({});
  const [availableWorkflows, setAvailableWorkflows] = useState<string[]>([]);
  const logsEndRefs = useRef<Record<string, HTMLDivElement | null>>({});

  // Load available workflows
  useEffect(() => {
    loadAvailableWorkflows();
  }, [environment]);

  const loadAvailableWorkflows = async () => {
    try {
      // Dynamically discover workflows from .github/workflows directory
      const response = await invoke<CommandResponse<string[]>>('list_github_workflows', {
        environment,
      });
      
      if (response.success && response.result) {
        setAvailableWorkflows(response.result);
      } else {
        // Fallback to hardcoded list if discovery fails
        console.warn('Workflow discovery failed, using fallback list:', response.error);
        const workflows = [
          `mystira-app-api-cicd-${environment}.yml`,
          `mystira-app-admin-api-cicd-${environment}.yml`,
          `mystira-app-pwa-cicd-${environment}.yml`,
          `infrastructure-deploy-${environment}.yml`,
        ];
        setAvailableWorkflows(workflows);
      }
    } catch (error) {
      console.error('Failed to load workflows:', error);
      // Fallback to hardcoded list
      const workflows = [
        `mystira-app-api-cicd-${environment}.yml`,
        `mystira-app-admin-api-cicd-${environment}.yml`,
        `mystira-app-pwa-cicd-${environment}.yml`,
        `infrastructure-deploy-${environment}.yml`,
      ];
      setAvailableWorkflows(workflows);
    }
  };

  // Load saved pipeline associations
  useEffect(() => {
    const saved = localStorage.getItem(`projectPipelines_${environment}`);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        setProjectPipelines(parsed);
      } catch (e) {
        console.error('Failed to parse saved pipelines:', e);
      }
        } else {
          // Set defaults based on project names
          const defaults: Record<string, ProjectPipeline> = {};
          projects.forEach(project => {
            if (project.id === 'mystira-api') {
              defaults[project.id] = {
                projectId: project.id,
                workflowFile: `mystira-app-api-cicd-${environment}.yml`,
                environment,
              };
            } else if (project.id === 'mystira-admin-api') {
              defaults[project.id] = {
                projectId: project.id,
                workflowFile: `mystira-app-admin-api-cicd-${environment}.yml`,
                environment,
              };
            } else if (project.id === 'mystira-pwa') {
              defaults[project.id] = {
                projectId: project.id,
                workflowFile: `mystira-app-pwa-cicd-${environment}.yml`,
                environment,
              };
            }
          });
          setProjectPipelines(defaults);
        }
  }, [environment, projects]);

  // Save pipeline associations
  useEffect(() => {
    localStorage.setItem(`projectPipelines_${environment}`, JSON.stringify(projectPipelines));
  }, [projectPipelines, environment]);

  const toggleProjectSelection = (projectId: string) => {
    const newSelected = new Set(selectedProjects);
    if (newSelected.has(projectId)) {
      newSelected.delete(projectId);
    } else {
      newSelected.add(projectId);
    }
    setSelectedProjects(newSelected);
  };

  const updatePipeline = (projectId: string, workflowFile: string) => {
    setProjectPipelines(prev => ({
      ...prev,
      [projectId]: {
        ...prev[projectId],
        projectId,
        workflowFile,
        environment,
      },
    }));
  };

  const handleDeployProjects = async () => {
    if (selectedProjects.size === 0) {
      alert('Please select at least one project to deploy');
      return;
    }

    setDeploying(true);
    const newWorkflowRuns: Record<string, WorkflowRun> = {};
    const newWorkflowLogs: Record<string, string[]> = {};

    try {
      for (const projectId of selectedProjects) {
        const pipeline = projectPipelines[projectId];
        if (!pipeline) {
          console.error(`No pipeline configured for project ${projectId}`);
          continue;
        }

        // Dispatch workflow
        const dispatchResponse = await invoke<CommandResponse>('github_dispatch_workflow', {
          workflowFile: pipeline.workflowFile,
          inputs: pipeline.inputs || {},
        });

        if (dispatchResponse.success && dispatchResponse.result) {
          const run = dispatchResponse.result as any;
          newWorkflowRuns[projectId] = {
            id: run.id,
            name: run.name || pipeline.workflowFile,
            status: 'queued',
            conclusion: null,
            html_url: run.html_url || '',
            created_at: run.created_at || new Date().toISOString(),
            updated_at: run.updated_at || new Date().toISOString(),
          };
          newWorkflowLogs[projectId] = [`🚀 Dispatched workflow: ${pipeline.workflowFile}`];
          setShowLogs(prev => ({ ...prev, [projectId]: true }));
          
          // Store deployment time in localStorage for InfrastructureStatus component
          const lastDeployedKey = `lastDeployed_${projectId}_${environment}`;
          localStorage.setItem(lastDeployedKey, Date.now().toString());
        } else {
          newWorkflowLogs[projectId] = [`❌ Failed to dispatch: ${dispatchResponse.error || 'Unknown error'}`];
        }
      }

      setWorkflowRuns(newWorkflowRuns);
      setWorkflowLogs(newWorkflowLogs);

      // Start polling for workflow status
      if (Object.keys(newWorkflowRuns).length > 0) {
        pollWorkflowStatus(Object.keys(newWorkflowRuns));
      }
    } catch (error) {
      console.error('Failed to deploy projects:', error);
    } finally {
      setDeploying(false);
    }
  };

  const pollWorkflowStatus = async (projectIds: string[]) => {
    const interval = setInterval(async () => {
      let allCompleted = true;

      for (const projectId of projectIds) {
        const run = workflowRuns[projectId];
        if (!run || (run.status === 'completed' || run.status === 'cancelled')) {
          continue;
        }

        allCompleted = false;

        try {
          const statusResponse = await invoke<CommandResponse>('github_workflow_status', {
            runId: run.id,
          });

            if (statusResponse.success && statusResponse.result) {
              const status = statusResponse.result as any;
              setWorkflowRuns(prev => ({
                ...prev,
                [projectId]: {
                  ...prev[projectId],
                  status: status.status,
                  conclusion: status.conclusion,
                  updated_at: status.updated_at || prev[projectId].updated_at,
                },
              }));
              
              // Update deployment time when workflow completes successfully
              if (status.status === 'completed' && status.conclusion === 'success') {
                const lastDeployedKey = `lastDeployed_${projectId}_${environment}`;
                localStorage.setItem(lastDeployedKey, Date.now().toString());
              }

            // Fetch logs if workflow is running
            if (status.status === 'in_progress' || status.status === 'queued') {
              const logsResponse = await invoke<CommandResponse>('github_workflow_logs', {
                runId: run.id,
              });

              if (logsResponse.success && logsResponse.result) {
                const logs = logsResponse.result as string[];
                setWorkflowLogs(prev => ({
                  ...prev,
                  [projectId]: logs,
                }));
              }
            }
          }
        } catch (error) {
          console.error(`Failed to get status for ${projectId}:`, error);
        }
      }

      if (allCompleted) {
        clearInterval(interval);
      }
    }, 3000); // Poll every 3 seconds

    // Cleanup after 10 minutes
    setTimeout(() => clearInterval(interval), 600000);
  };

  const getProjectTypeIcon = (type: string) => {
    switch (type) {
      case 'api': return '🌐';
      case 'admin-api': return '🔧';
      case 'pwa': return '📱';
      case 'service': return '⚙️';
      default: return '📦';
    }
  };

  const getStatusColor = (status: string, conclusion: string | null) => {
    if (status === 'completed') {
      return conclusion === 'success' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400';
    }
    if (status === 'in_progress' || status === 'queued') {
      return 'text-blue-600 dark:text-blue-400';
    }
    return 'text-gray-600 dark:text-gray-400';
  };

  const getStatusIcon = (status: string, conclusion: string | null) => {
    if (status === 'completed') {
      return conclusion === 'success' ? '✅' : '❌';
    }
    if (status === 'in_progress') {
      return '🔄';
    }
    if (status === 'queued') {
      return '⏳';
    }
    return '⚪';
  };

  return (
    <div className="mb-8">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">
            Step 3: Deploy Projects
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Deploy selected projects using GitHub Actions workflows
          </p>
        </div>
        <button
          onClick={handleDeployProjects}
          disabled={!hasDeployedInfrastructure || deploying || selectedProjects.size === 0}
          className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {deploying ? '🚀 Deploying...' : '🚀 Deploy Projects'}
        </button>
      </div>

      {!hasDeployedInfrastructure && (
        <div className="mb-4 p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
          <p className="text-sm text-yellow-800 dark:text-yellow-200">
            ⚠️ Please deploy infrastructure first before deploying projects.
          </p>
        </div>
      )}

      <div className="space-y-4">
        {projects.map((project) => {
          const pipeline = projectPipelines[project.id];
          const run = workflowRuns[project.id];
          const logs = workflowLogs[project.id] || [];
          const showLog = showLogs[project.id];

          return (
            <div
              key={project.id}
              className={`border-2 rounded-lg p-4 transition-all ${
                selectedProjects.has(project.id)
                  ? 'border-green-500 dark:border-green-600 bg-green-50 dark:bg-green-900/20'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800'
              }`}
            >
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0">
                  <div className="w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-900 flex items-center justify-center text-2xl">
                    {getProjectTypeIcon(project.type)}
                  </div>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <input
                      type="checkbox"
                      checked={selectedProjects.has(project.id)}
                      onChange={() => toggleProjectSelection(project.id)}
                      className="w-4 h-4 text-green-600 border-gray-300 rounded focus:ring-green-500"
                      aria-label={`Select ${project.name} for deployment`}
                    />
                    <h4 className="font-semibold text-gray-900 dark:text-white">{project.name}</h4>
                    {run && (
                      <span className={`text-sm font-medium ${getStatusColor(run.status, run.conclusion)}`}>
                        {getStatusIcon(run.status, run.conclusion)} {run.status}
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{project.description}</p>

                  {/* Pipeline Configuration */}
                  <div className="mb-3">
                    <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">
                      GitHub Workflow:
                    </label>
                    <select
                      value={pipeline?.workflowFile || ''}
                      onChange={(e) => updatePipeline(project.id, e.target.value)}
                      className="w-full px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-green-500 focus:border-green-500 dark:bg-gray-700 dark:text-white"
                      aria-label={`GitHub workflow for ${project.name}`}
                    >
                      <option value="">Select workflow...</option>
                      {availableWorkflows.map(workflow => (
                        <option key={workflow} value={workflow}>
                          {workflow}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Workflow Run Link */}
                  {run && run.html_url && (
                    <div className="mb-3">
                      <a
                        href={run.html_url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-xs text-blue-600 dark:text-blue-400 hover:underline"
                      >
                        🔗 View on GitHub
                      </a>
                    </div>
                  )}

                  {/* Logs Toggle */}
                  {logs.length > 0 && (
                    <div className="mb-2">
                      <button
                        onClick={() => setShowLogs(prev => ({ ...prev, [project.id]: !prev[project.id] }))}
                        className="text-xs text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
                      >
                        {showLog ? '📋 Hide Logs' : '📋 Show Logs'}
                      </button>
                    </div>
                  )}

                  {/* Logs Viewer */}
                  {showLog && logs.length > 0 && (
                    <div className="mt-3 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                      <div className="bg-gray-900 text-green-400 font-mono text-xs p-3 max-h-64 overflow-y-auto">
                        {logs.map((line, index) => (
                          <div key={index} className="whitespace-pre-wrap">
                            {line}
                          </div>
                        ))}
                        <div 
                          ref={(el) => { 
                            logsEndRefs.current[project.id] = el;
                            // Auto-scroll to bottom when new logs arrive
                            if (el && (run?.status === 'in_progress' || run?.status === 'queued')) {
                              setTimeout(() => el.scrollIntoView({ behavior: 'smooth' }), 100);
                            }
                          }} 
                        />
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default ProjectDeployment;

