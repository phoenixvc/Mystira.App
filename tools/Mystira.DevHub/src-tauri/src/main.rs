// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use serde::{Deserialize, Serialize};
use std::process::{Command, Stdio};
use std::io::Write;
use std::path::PathBuf;
use std::env;
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

// Get the path to the built .NET CLI executable
fn get_cli_executable_path() -> Result<PathBuf, String> {
    // Try to find the executable relative to the Tauri app
    let base_path = env::current_dir()
        .map_err(|e| format!("Failed to get current directory: {}", e))?;

    // Check for pre-built executable in multiple locations
    let possible_paths = vec![
        // Development: relative to Tauri project
        base_path.join("../../Mystira.DevHub.CLI/bin/Debug/net9.0/Mystira.DevHub.CLI"),
        base_path.join("../../Mystira.DevHub.CLI/bin/Release/net9.0/Mystira.DevHub.CLI"),
        // Production: bundled with app
        base_path.join("Mystira.DevHub.CLI"),
        base_path.join("bin/Mystira.DevHub.CLI"),
    ];

    // Add .exe extension on Windows
    #[cfg(target_os = "windows")]
    let possible_paths: Vec<PathBuf> = possible_paths
        .iter()
        .map(|p| p.with_extension("exe"))
        .collect();

    // Find the first path that exists
    for path in &possible_paths {
        if path.exists() {
            return Ok(path.clone());
        }
    }

    // If no built executable found, provide helpful error
    Err(format!(
        "Could not find Mystira.DevHub.CLI executable. Please build it first:\n\
         cd tools/Mystira.DevHub.CLI\n\
         dotnet build\n\
         \n\
         Searched in:\n{}",
        possible_paths
            .iter()
            .map(|p| format!("  - {}", p.display()))
            .collect::<Vec<_>>()
            .join("\n")
    ))
}

// Execute .NET CLI wrapper and return response
async fn execute_devhub_cli(command: String, args: serde_json::Value) -> Result<CommandResponse, String> {
    let request = CommandRequest {
        command: command.clone(),
        args,
    };

    let request_json = serde_json::to_string(&request)
        .map_err(|e| format!("Failed to serialize request: {}", e))?;

    // Get the CLI executable path
    let cli_exe_path = get_cli_executable_path()?;

    // Spawn the .NET process
    let mut child = Command::new(&cli_exe_path)
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Failed to spawn process at {}: {}", cli_exe_path.display(), e))?;

    // Write JSON to stdin
    if let Some(mut stdin) = child.stdin.take() {
        stdin.write_all(request_json.as_bytes())
            .map_err(|e| format!("Failed to write to stdin: {}", e))?;
        // Close stdin to signal we're done writing
        drop(stdin);
    }

    // Wait for the process to complete and read output
    let output = child.wait_with_output()
        .map_err(|e| format!("Failed to wait for process: {}", e))?;

    if !output.status.success() {
        let stderr = String::from_utf8_lossy(&output.stderr);
        let stdout = String::from_utf8_lossy(&output.stdout);
        return Err(format!(
            "Process failed with exit code: {:?}\nStderr: {}\nStdout: {}",
            output.status.code(),
            stderr,
            stdout
        ));
    }

    // Parse the JSON response
    let stdout = String::from_utf8_lossy(&output.stdout);

    // Handle empty response
    if stdout.trim().is_empty() {
        return Err("Process returned empty response".to_string());
    }

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

// New commands for Wave 1: Real integrations

#[tauri::command]
async fn get_azure_resources(subscription_id: Option<String>) -> Result<CommandResponse, String> {
    let args = if let Some(sub_id) = subscription_id {
        serde_json::json!({ "subscriptionId": sub_id })
    } else {
        serde_json::json!({})
    };
    execute_devhub_cli("azure.list-resources".to_string(), args).await
}

#[tauri::command]
async fn get_github_deployments(repository: String, limit: Option<i32>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "repository": repository,
        "limit": limit.unwrap_or(10)
    });
    execute_devhub_cli("github.list-deployments".to_string(), args).await
}

#[tauri::command]
async fn test_connection(connection_type: String, connection_string: Option<String>) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "type": connection_type,
        "connectionString": connection_string
    });
    execute_devhub_cli("connection.test".to_string(), args).await
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
            get_azure_resources,
            get_github_deployments,
            test_connection,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
