//! Infrastructure workflow management module.
//!
//! This module provides commands for managing infrastructure deployments via GitHub workflows:
//! - Validation of infrastructure templates
//! - Preview of changes before deployment
//! - Deployment execution
//! - Infrastructure destruction
//! - Status checking
//!
//! These commands trigger GitHub Actions workflows rather than executing deployments directly.

use crate::cli::execute_devhub_cli;
use crate::types::CommandResponse;

/// Validate infrastructure via GitHub workflow
#[tauri::command]
pub async fn infrastructure_validate(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.validate".to_string(), args).await
}

/// Preview infrastructure changes via GitHub workflow
#[tauri::command]
pub async fn infrastructure_preview(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.preview".to_string(), args).await
}

/// Deploy infrastructure via GitHub workflow
#[tauri::command]
pub async fn infrastructure_deploy(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.deploy".to_string(), args).await
}

/// Destroy infrastructure via GitHub workflow
#[tauri::command]
pub async fn infrastructure_destroy(workflow_file: String, repository: String, confirm: bool) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository,
        "confirm": confirm
    });
    execute_devhub_cli("infrastructure.destroy".to_string(), args).await
}

/// Get infrastructure deployment status via GitHub workflow
#[tauri::command]
pub async fn infrastructure_status(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.status".to_string(), args).await
}

