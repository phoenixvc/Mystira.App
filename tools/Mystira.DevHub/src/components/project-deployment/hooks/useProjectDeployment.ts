import { invoke } from '@tauri-apps/api/tauri';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { CommandResponse } from '../../../types';
import type { ProjectInfo } from '../ProjectDeploymentPlanner';
import type { ProjectPipeline, WorkflowRun } from '../types';

interface UseProjectDeploymentProps {
  environment: string;
  projects: ProjectInfo[];
}

export interface DeploymentError {
  type: 'validation' | 'dispatch' | 'workflow' | 'network';
  message: string;
  projectId?: string;
  retryable?: boolean;
}

export interface WorkflowDiscoveryStatus {
  loading: boolean;
  usingFallback: boolean;
  error: string | null;
}

export function useProjectDeployment({ environment, projects }: UseProjectDeploymentProps) {
  const [selectedProjects, setSelectedProjects] = useState<Set<string>>(new Set());
  const [projectPipelines, setProjectPipelines] = useState<Record<string, ProjectPipeline>>({});
  const [deploying, setDeploying] = useState(false);
  const [workflowRuns, setWorkflowRuns] = useState<Record<string, WorkflowRun>>({});
  const [workflowLogs, setWorkflowLogs] = useState<Record<string, string[]>>({});
  const [showLogs, setShowLogs] = useState<Record<string, boolean>>({});
  const [availableWorkflows, setAvailableWorkflows] = useState<string[]>([]);
  const logsEndRefs = useRef<Record<string, HTMLDivElement | null>>({});

  // New UX states
  const [deploymentErrors, setDeploymentErrors] = useState<DeploymentError[]>([]);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [workflowDiscoveryStatus, setWorkflowDiscoveryStatus] = useState<WorkflowDiscoveryStatus>({
    loading: true,
    usingFallback: false,
    error: null,
  });
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [failedProjects, setFailedProjects] = useState<Set<string>>(new Set());

  useEffect(() => {
    loadAvailableWorkflows();
  }, [environment]);

  const loadAvailableWorkflows = async () => {
    setWorkflowDiscoveryStatus({ loading: true, usingFallback: false, error: null });

    const fallbackWorkflows = [
      `mystira-app-api-cicd-${environment}.yml`,
      `mystira-app-admin-api-cicd-${environment}.yml`,
      `mystira-app-pwa-cicd-${environment}.yml`,
      `infrastructure-deploy-${environment}.yml`,
    ];

    try {
      const response = await invoke<CommandResponse<string[]>>('list_github_workflows', {
        environment,
      });

      if (response.success && response.result) {
        setAvailableWorkflows(response.result);
        setWorkflowDiscoveryStatus({ loading: false, usingFallback: false, error: null });
      } else {
        setAvailableWorkflows(fallbackWorkflows);
        setWorkflowDiscoveryStatus({
          loading: false,
          usingFallback: true,
          error: `Could not discover workflows: ${response.error || 'Unknown error'}. Using default workflow list.`
        });
      }
    } catch (error) {
      setAvailableWorkflows(fallbackWorkflows);
      setWorkflowDiscoveryStatus({
        loading: false,
        usingFallback: true,
        error: `Failed to connect to GitHub API. Using default workflow list.`
      });
    }
  };

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

  // Clear validation error when selection changes
  useEffect(() => {
    if (selectedProjects.size > 0) {
      setValidationError(null);
    }
  }, [selectedProjects]);

  const clearErrors = useCallback(() => {
    setDeploymentErrors([]);
    setValidationError(null);
  }, []);

  const dismissError = useCallback((index: number) => {
    setDeploymentErrors(prev => prev.filter((_, i) => i !== index));
  }, []);

  const requestDeploy = useCallback(() => {
    // Clear previous errors
    clearErrors();

    // Validation
    if (selectedProjects.size === 0) {
      setValidationError('Please select at least one project to deploy');
      return;
    }

    // Check if all selected projects have workflows configured
    const missingWorkflows: string[] = [];
    for (const projectId of selectedProjects) {
      const pipeline = projectPipelines[projectId];
      if (!pipeline?.workflowFile) {
        const project = projects.find(p => p.id === projectId);
        missingWorkflows.push(project?.name || projectId);
      }
    }

    if (missingWorkflows.length > 0) {
      setValidationError(`Please select a workflow for: ${missingWorkflows.join(', ')}`);
      return;
    }

    // Show confirmation dialog for multiple projects
    if (selectedProjects.size > 1) {
      setShowConfirmDialog(true);
      return;
    }

    // Single project - deploy directly
    handleDeployProjects();
  }, [selectedProjects, projectPipelines, projects]);

  const confirmDeploy = useCallback(() => {
    setShowConfirmDialog(false);
    handleDeployProjects();
  }, []);

  const cancelDeploy = useCallback(() => {
    setShowConfirmDialog(false);
  }, []);

  const handleDeployProjects = async () => {
    setDeploying(true);
    clearErrors();
    const newWorkflowRuns: Record<string, WorkflowRun> = {};
    const newWorkflowLogs: Record<string, string[]> = {};
    const newErrors: DeploymentError[] = [];
    const newFailedProjects = new Set<string>();

    try {
      for (const projectId of selectedProjects) {
        const pipeline = projectPipelines[projectId];
        if (!pipeline) {
          const project = projects.find(p => p.id === projectId);
          newErrors.push({
            type: 'validation',
            message: `No pipeline configured for ${project?.name || projectId}`,
            projectId,
            retryable: false,
          });
          continue;
        }

        try {
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

            const lastDeployedKey = `lastDeployed_${projectId}_${environment}`;
            localStorage.setItem(lastDeployedKey, Date.now().toString());
          } else {
            const project = projects.find(p => p.id === projectId);
            newWorkflowLogs[projectId] = [`❌ Failed to dispatch: ${dispatchResponse.error || 'Unknown error'}`];
            newErrors.push({
              type: 'dispatch',
              message: `Failed to deploy ${project?.name || projectId}: ${dispatchResponse.error || 'Unknown error'}`,
              projectId,
              retryable: true,
            });
            newFailedProjects.add(projectId);
          }
        } catch (error) {
          const project = projects.find(p => p.id === projectId);
          const errorMessage = error instanceof Error ? error.message : 'Network error';
          newWorkflowLogs[projectId] = [`❌ Network error: ${errorMessage}`];
          newErrors.push({
            type: 'network',
            message: `Network error deploying ${project?.name || projectId}: ${errorMessage}`,
            projectId,
            retryable: true,
          });
          newFailedProjects.add(projectId);
        }
      }

      setWorkflowRuns(prev => ({ ...prev, ...newWorkflowRuns }));
      setWorkflowLogs(prev => ({ ...prev, ...newWorkflowLogs }));
      setDeploymentErrors(newErrors);
      setFailedProjects(newFailedProjects);

      if (Object.keys(newWorkflowRuns).length > 0) {
        pollWorkflowStatus(Object.keys(newWorkflowRuns));
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      setDeploymentErrors([{
        type: 'network',
        message: `Deployment failed: ${errorMessage}`,
        retryable: true,
      }]);
    } finally {
      setDeploying(false);
    }
  };

  const retryFailedProjects = useCallback(() => {
    // Keep only failed projects selected for retry
    setSelectedProjects(failedProjects);
    setFailedProjects(new Set());
    // Clear errors for failed projects
    setDeploymentErrors(prev => prev.filter(e => !e.projectId || !failedProjects.has(e.projectId)));
  }, [failedProjects]);

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
            
            if (status.status === 'completed' && status.conclusion === 'success') {
              const lastDeployedKey = `lastDeployed_${projectId}_${environment}`;
              localStorage.setItem(lastDeployedKey, Date.now().toString());
            }

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
    }, 3000);

    setTimeout(() => clearInterval(interval), 600000);
  };

  return {
    // State
    selectedProjects,
    projectPipelines,
    deploying,
    workflowRuns,
    workflowLogs,
    showLogs,
    availableWorkflows,
    logsEndRefs,

    // New UX states
    deploymentErrors,
    validationError,
    workflowDiscoveryStatus,
    showConfirmDialog,
    failedProjects,

    // Actions
    toggleProjectSelection,
    updatePipeline,
    setShowLogs,

    // New UX actions
    requestDeploy,
    confirmDeploy,
    cancelDeploy,
    clearErrors,
    dismissError,
    retryFailedProjects,
    refreshWorkflows: loadAvailableWorkflows,
  };
}

