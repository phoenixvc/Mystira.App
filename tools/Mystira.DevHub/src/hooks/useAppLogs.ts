import { useEffect, useState } from 'react';
import { EVENTS } from '../types';

interface GlobalLog {
  timestamp: Date;
  message: string;
  type: 'info' | 'error' | 'warn';
}

interface Problem {
  id: string;
  timestamp: Date;
  severity: 'error' | 'warning' | 'info';
  message: string;
  source?: string;
  details?: string;
}

export function useAppLogs() {
  const [globalLogs, setGlobalLogs] = useState<GlobalLog[]>([]);
  const [deploymentLogs, setDeploymentLogs] = useState<string | null>(null);
  const [problems, setProblems] = useState<Problem[]>([]);

  // Listen for global log events
  useEffect(() => {
    const handleGlobalLog = (event: CustomEvent<{ message: string; type: 'info' | 'error' | 'warn' }>) => {
      setGlobalLogs(prev => [...prev.slice(-500), { timestamp: new Date(), ...event.detail }]);
    };

    window.addEventListener(EVENTS.GLOBAL_LOG as any, handleGlobalLog);
    return () => {
      window.removeEventListener(EVENTS.GLOBAL_LOG as any, handleGlobalLog);
    };
  }, []);

  // Listen for deployment logs
  useEffect(() => {
    const handleDeploymentLogs = (event: CustomEvent<{ logs: string }>) => {
      setDeploymentLogs(event.detail.logs);
    };

    window.addEventListener(EVENTS.DEPLOYMENT_LOGS as any, handleDeploymentLogs);
    return () => {
      window.removeEventListener(EVENTS.DEPLOYMENT_LOGS as any, handleDeploymentLogs);
    };
  }, []);

  // Listen for infrastructure problems
  useEffect(() => {
    const handleInfrastructureProblem = (event: CustomEvent<{ 
      severity: 'error' | 'warning' | 'info'; 
      message: string; 
      source?: string; 
      details?: string;
      clear?: boolean;
    }>) => {
      if (event.detail.clear) {
        setProblems([]);
      } else {
        const problem: Problem = {
          id: `problem-${Date.now()}-${Math.random().toString(36).slice(2)}`,
          timestamp: new Date(),
          severity: event.detail.severity,
          message: event.detail.message,
          source: event.detail.source || 'Infrastructure',
          details: event.detail.details,
        };
        setProblems(prev => {
          // Remove duplicates with same message
          const filtered = prev.filter(p => p.message !== problem.message);
          return [...filtered, problem].slice(-100); // Keep last 100 problems
        });
      }
    };

    window.addEventListener(EVENTS.INFRASTRUCTURE_PROBLEM as any, handleInfrastructureProblem);
    return () => {
      window.removeEventListener(EVENTS.INFRASTRUCTURE_PROBLEM as any, handleInfrastructureProblem);
    };
  }, []);

  const clearAllLogs = () => {
    setGlobalLogs([]);
    setDeploymentLogs(null);
  };

  return {
    globalLogs,
    deploymentLogs,
    problems,
    clearAllLogs,
  };
}

