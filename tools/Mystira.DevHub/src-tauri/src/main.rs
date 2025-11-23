// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use serde::{Deserialize, Serialize};
use std::process::{Command, Stdio};
use std::io::Write;
use tauri::Manager;

#[derive(Debug, Serialize, Deserialize)]
struct CommandRequest {
    command: String,
    args: serde_json::Value,
}

#[derive(Debug, Serialize, Deserialize)]
struct CommandResponse {
    success: bool,
    result: Option<serde_json::Value>,
    message: Option<String>,
    error: Option<String>,
}

// Execute .NET CLI wrapper and return response
async fn execute_devhub_cli(command: String, args: serde_json::Value) -> Result<CommandResponse, String> {
    let request = CommandRequest {
        command: command.clone(),
        args,
    };

    let request_json = serde_json::to_string(&request)
        .map_err(|e| format!("Failed to serialize request: {}", e))?;

    // Get the path to the .NET CLI project
    let cli_project_path = "../../Mystira.DevHub.CLI/Mystira.DevHub.CLI.csproj";

    // Spawn the .NET process
    let mut child = Command::new("dotnet")
        .arg("run")
        .arg("--project")
        .arg(cli_project_path)
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to spawn dotnet process: {}", e))?;

    // Write JSON to stdin
    if let Some(mut stdin) = child.stdin.take() {
        stdin.write_all(request_json.as_bytes())
            .map_err(|e| format!("Failed to write to stdin: {}", e))?;
    }

    // Wait for the process to complete and read output
    let output = child.wait_with_output()
        .map_err(|e| format!("Failed to wait for process: {}", e))?;

    if !output.status.success() {
        let stderr = String::from_utf8_lossy(&output.stderr);
        return Err(format!("Process failed with stderr: {}", stderr));
    }

    // Parse the JSON response
    let stdout = String::from_utf8_lossy(&output.stdout);
    let response: CommandResponse = serde_json::from_str(&stdout)
        .map_err(|e| format!("Failed to parse response JSON: {}. Output was: {}", e, stdout))?;

    Ok(response)
}

// Cosmos Commands
#[tauri::command]
async fn cosmos_export(output_path: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "outputPath": output_path
    });
    execute_devhub_cli("cosmos.export".to_string(), args).await
}

#[tauri::command]
async fn cosmos_stats() -> Result<CommandResponse, String> {
    execute_devhub_cli("cosmos.stats".to_string(), serde_json::json!({})).await
}

// Migration Commands
#[tauri::command]
async fn migration_run(
    migration_type: String,
    source_cosmos: Option<String>,
    dest_cosmos: Option<String>,
    source_storage: Option<String>,
    dest_storage: Option<String>,
    database_name: String,
    container_name: String,
) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "type": migration_type,
        "sourceCosmosConnection": source_cosmos,
        "destCosmosConnection": dest_cosmos,
        "sourceStorageConnection": source_storage,
        "destStorageConnection": dest_storage,
        "databaseName": database_name,
        "containerName": container_name
    });
    execute_devhub_cli("migration.run".to_string(), args).await
}

// Infrastructure Commands
#[tauri::command]
async fn infrastructure_validate(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.validate".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_preview(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.preview".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_deploy(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.deploy".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_destroy(workflow_file: String, repository: String, confirm: bool) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository,
        "confirm": confirm
    });
    execute_devhub_cli("infrastructure.destroy".to_string(), args).await
}

#[tauri::command]
async fn infrastructure_status(workflow_file: String, repository: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "workflowFile": workflow_file,
        "repository": repository
    });
    execute_devhub_cli("infrastructure.status".to_string(), args).await
}

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            cosmos_export,
            cosmos_stats,
            migration_run,
            infrastructure_validate,
            infrastructure_preview,
            infrastructure_deploy,
            infrastructure_destroy,
            infrastructure_status,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
