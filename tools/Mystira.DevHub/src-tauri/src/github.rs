//! GitHub workflow operations module.
//!
//! This module provides commands for interacting with GitHub workflows and deployments:
//! - Listing workflow runs and deployment history
//! - Dispatching workflow runs
//! - Checking workflow status
//! - Retrieving workflow logs
//! - Listing available workflows
//!
//! Some commands use GitHub CLI (`gh`) directly for better integration, while others
//! route through the DevHub CLI tool.

use crate::cli::execute_devhub_cli;
use crate::types::CommandResponse;
use crate::cache::{GITHUB_DEPLOYMENTS_CACHE, get_cache_ttl};
use crate::rate_limit::wait_github_rate_limit;
use std::process::Command;
use tracing::debug;

/// Get GitHub workflow deployment history
#[tauri::command]
pub async fn get_github_deployments(repository: String, limit: Option<i32>) -> Result<CommandResponse, String> {
    let limit_value = limit.unwrap_or(20);
    
    // Build cache key
    let cache_key = format!("github_deployments:{}:{}", repository, limit_value);
    
    // Try cache first
    let ttl = get_cache_ttl("github_deployments");
    if let Some(cached) = GITHUB_DEPLOYMENTS_CACHE.get(&cache_key) {
        debug!("Cache hit for GitHub deployments: {}", cache_key);
        match serde_json::from_str::<CommandResponse>(&cached) {
            Ok(response) => return Ok(response),
            Err(_) => {
                GITHUB_DEPLOYMENTS_CACHE.invalidate(&cache_key);
            }
        }
    }
    
    // Apply rate limiting
    wait_github_rate_limit().await;
    
    let args = serde_json::json!({
        "repository": repository,
        "limit": limit_value
    });
    
    // First try the CLI command
    let cli_result = execute_devhub_cli("github.list-deployments".to_string(), args.clone()).await;
    
    // If CLI command fails with "Unknown command", try direct GitHub CLI
    if let Err(ref e) = cli_result {
        if e.contains("Unknown command") || e.contains("command") {
            // Fallback to direct GitHub CLI call
            let repo_parts: Vec<&str> = repository.split('/').collect();
            if repo_parts.len() != 2 {
                return Err(format!("Invalid repository format: {}. Expected format: owner/repo", repository));
            }
            
            let output = Command::new("gh")
                .arg("api")
                .arg("-X")
                .arg("GET")
                .arg(format!("/repos/{}/actions/runs?per_page={}", repository, limit_value))
                .arg("--jq")
                .arg(".[\"workflow_runs\"]")
                .output();
                
            match output {
                Ok(result) => {
                    if result.status.success() {
                        let stdout = String::from_utf8_lossy(&result.stdout);
                        let runs: Result<serde_json::Value, _> = serde_json::from_str(&stdout);
                        match runs {
                            Ok(workflow_runs) => {
                                let response = CommandResponse {
                                    success: true,
                                    result: Some(workflow_runs),
                                    message: None,
                                    error: None,
                                };
                                
                                // Cache the response
                                if let Ok(cached_json) = serde_json::to_string(&response) {
                                    GITHUB_DEPLOYMENTS_CACHE.set(cache_key.clone(), cached_json, ttl);
                                }
                                
                                Ok(response)
                            }
                            Err(e) => Err(format!("Failed to parse GitHub API response: {}", e))
                        }
                    } else {
                        let stderr = String::from_utf8_lossy(&result.stderr);
                        Err(format!("GitHub CLI command failed: {}", stderr))
                    }
                }
                Err(e) => Err(format!("Failed to execute GitHub CLI: {}. Make sure GitHub CLI (gh) is installed and authenticated.", e))
            }
        } else {
            cli_result
        }
    } else {
        cli_result
    }
}

/// Dispatch a GitHub workflow
#[tauri::command]
pub async fn github_dispatch_workflow(workflow_file: String, inputs: Option<serde_json::Value>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "inputs": inputs.unwrap_or(serde_json::json!({}))
    });
    execute_devhub_cli("github.dispatch-workflow".to_string(), args).await
}

/// Get GitHub workflow status
#[tauri::command]
pub async fn github_workflow_status(run_id: i64) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "runId": run_id
    });
    execute_devhub_cli("github.workflow-status".to_string(), args).await
}

/// Get GitHub workflow logs
#[tauri::command]
pub async fn github_workflow_logs(run_id: i64) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "runId": run_id
    });
    execute_devhub_cli("github.workflow-logs".to_string(), args).await
}

/// List available GitHub workflows
#[tauri::command]
pub async fn list_github_workflows(environment: Option<String>) -> Result<CommandResponse, String> {
    use crate::helpers::find_repo_root;
    use std::fs;
    
    let repo_root = find_repo_root()?;
    let workflows_dir = repo_root.join(".github").join("workflows");
    
    if !workflows_dir.exists() {
        return Ok(CommandResponse {
            success: false,
            result: None,
            message: None,
            error: Some("Workflows directory not found".to_string()),
        });
    }
    
    let mut workflows = Vec::new();
    
    match fs::read_dir(&workflows_dir) {
        Ok(entries) => {
            for entry in entries {
                if let Ok(entry) = entry {
                    let path = entry.path();
                    if path.is_file() {
                        if let Some(ext) = path.extension() {
                            if ext == "yml" || ext == "yaml" {
                                if let Some(file_name) = path.file_name() {
                                    if let Some(file_name_str) = file_name.to_str() {
                                        if let Some(env_filter) = &environment {
                                            if file_name_str.to_lowercase().contains(&env_filter.to_lowercase()) {
                                                workflows.push(file_name_str.to_string());
                                            }
                                        } else {
                                            workflows.push(file_name_str.to_string());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Err(e) => {
            return Ok(CommandResponse {
                success: false,
                result: None,
                message: None,
                error: Some(format!("Failed to read workflows directory: {}", e)),
            });
        }
    }
    
    workflows.sort();
    
    Ok(CommandResponse {
        success: true,
        result: Some(serde_json::json!(workflows)),
        message: None,
        error: None,
    })
}

