//! Cosmos DB operations module.
//!
//! This module provides commands for managing Cosmos DB:
//! - Data export to CSV
//! - Statistics and metrics
//! - Migration operations between Cosmos DB instances
//! - Fetching connection strings from Azure
//!
//! All operations are executed via the DevHub CLI tool.

use crate::cli::execute_devhub_cli;
use crate::types::CommandResponse;
use std::process::Command;
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct EnvironmentConnectionStrings {
    pub cosmos_connection: Option<String>,
    pub storage_connection: Option<String>,
    pub error: Option<String>,
}

/// Export Cosmos DB data to CSV
#[tauri::command]
pub async fn cosmos_export(output_path: String) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "outputPath": output_path
    });
    execute_devhub_cli("cosmos.export".to_string(), args).await
}

/// Get Cosmos DB statistics
#[tauri::command]
pub async fn cosmos_stats() -> Result<CommandResponse, String> {
    execute_devhub_cli("cosmos.stats".to_string(), serde_json::json!({})).await
}

/// Run a migration between Cosmos DB instances
#[tauri::command]
pub async fn migration_run(
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

/// Fetch connection strings from Azure for a given environment
#[tauri::command]
pub async fn fetch_environment_connections(
    resource_group: String,
    cosmos_account_name: String,
    storage_account_name: String,
) -> Result<EnvironmentConnectionStrings, String> {
    let mut result = EnvironmentConnectionStrings {
        cosmos_connection: None,
        storage_connection: None,
        error: None,
    };

    // Fetch Cosmos DB connection string
    if !cosmos_account_name.is_empty() {
        let cosmos_output = Command::new("az")
            .args([
                "cosmosdb",
                "keys",
                "list",
                "--name",
                &cosmos_account_name,
                "--resource-group",
                &resource_group,
                "--type",
                "connection-strings",
                "--query",
                "connectionStrings[0].connectionString",
                "-o",
                "tsv",
            ])
            .output();

        match cosmos_output {
            Ok(output) => {
                if output.status.success() {
                    let conn_str = String::from_utf8_lossy(&output.stdout).trim().to_string();
                    if !conn_str.is_empty() {
                        result.cosmos_connection = Some(conn_str);
                    }
                } else {
                    let error = String::from_utf8_lossy(&output.stderr).to_string();
                    tracing::warn!("Failed to fetch Cosmos DB connection string: {}", error);
                }
            }
            Err(e) => {
                tracing::error!("Error executing az cosmosdb command: {}", e);
            }
        }
    }

    // Fetch Storage connection string
    if !storage_account_name.is_empty() {
        let storage_output = Command::new("az")
            .args([
                "storage",
                "account",
                "show-connection-string",
                "--name",
                &storage_account_name,
                "--resource-group",
                &resource_group,
                "--query",
                "connectionString",
                "-o",
                "tsv",
            ])
            .output();

        match storage_output {
            Ok(output) => {
                if output.status.success() {
                    let conn_str = String::from_utf8_lossy(&output.stdout).trim().to_string();
                    if !conn_str.is_empty() {
                        result.storage_connection = Some(conn_str);
                    }
                } else {
                    let error = String::from_utf8_lossy(&output.stderr).to_string();
                    tracing::warn!("Failed to fetch Storage connection string: {}", error);
                }
            }
            Err(e) => {
                tracing::error!("Error executing az storage command: {}", e);
            }
        }
    }

    // Set error if both connections failed
    if result.cosmos_connection.is_none() && result.storage_connection.is_none() {
        result.error = Some("Failed to fetch connection strings. Make sure you're logged in to Azure CLI and have access to the resources.".to_string());
    }

    Ok(result)
}

