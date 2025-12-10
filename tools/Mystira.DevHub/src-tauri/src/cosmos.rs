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
    pub resource_group: Option<String>,
    pub storage_account: Option<String>,
    pub error: Option<String>,
}

/// Discover resource group from Cosmos DB account name
fn discover_resource_group(cosmos_account_name: &str) -> Option<String> {
    let output = Command::new("az")
        .args([
            "cosmosdb",
            "list",
            "--query",
            &format!("[?name=='{}'].resourceGroup | [0]", cosmos_account_name),
            "-o",
            "tsv",
        ])
        .output();

    match output {
        Ok(output) => {
            if output.status.success() {
                let rg = String::from_utf8_lossy(&output.stdout).trim().to_string();
                if !rg.is_empty() && rg != "null" {
                    return Some(rg);
                }
            }
        }
        Err(e) => {
            tracing::error!("Failed to discover resource group: {}", e);
        }
    }
    None
}

/// Find storage account in a resource group
fn find_storage_account_in_rg(resource_group: &str) -> Option<String> {
    let output = Command::new("az")
        .args([
            "storage",
            "account",
            "list",
            "--resource-group",
            resource_group,
            "--query",
            "[0].name",
            "-o",
            "tsv",
        ])
        .output();

    match output {
        Ok(output) => {
            if output.status.success() {
                let name = String::from_utf8_lossy(&output.stdout).trim().to_string();
                if !name.is_empty() && name != "null" {
                    return Some(name);
                }
            }
        }
        Err(e) => {
            tracing::error!("Failed to find storage account: {}", e);
        }
    }
    None
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
    source_database_name: String,
    dest_database_name: String,
    container_name: String,
) -> Result<CommandResponse, String> {
    let args = serde_json::json!({
        "type": migration_type,
        "sourceCosmosConnection": source_cosmos,
        "destCosmosConnection": dest_cosmos,
        "sourceStorageConnection": source_storage,
        "destStorageConnection": dest_storage,
        "sourceDatabaseName": source_database_name,
        "destDatabaseName": dest_database_name,
        "containerName": container_name
    });
    execute_devhub_cli("migration.run".to_string(), args).await
}

/// Fetch connection strings from Azure for a given environment
/// Auto-discovers resource group and storage account from Cosmos account name
#[tauri::command]
pub async fn fetch_environment_connections(
    cosmos_account_name: String,
) -> Result<EnvironmentConnectionStrings, String> {
    let mut result = EnvironmentConnectionStrings {
        cosmos_connection: None,
        storage_connection: None,
        resource_group: None,
        storage_account: None,
        error: None,
    };

    if cosmos_account_name.is_empty() {
        result.error = Some("Cosmos account name is required".to_string());
        return Ok(result);
    }

    // Step 1: Discover resource group from Cosmos account
    let resource_group = match discover_resource_group(&cosmos_account_name) {
        Some(rg) => {
            tracing::info!("Discovered resource group: {} for Cosmos account: {}", rg, cosmos_account_name);
            result.resource_group = Some(rg.clone());
            rg
        }
        None => {
            result.error = Some(format!(
                "Could not find Cosmos DB account '{}'. Make sure you're logged in to the correct Azure subscription and have access.",
                cosmos_account_name
            ));
            return Ok(result);
        }
    };

    // Step 2: Fetch Cosmos DB connection string
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

    // Step 3: Find storage account in the same resource group
    if let Some(storage_name) = find_storage_account_in_rg(&resource_group) {
        result.storage_account = Some(storage_name.clone());

        // Fetch storage connection string
        let storage_output = Command::new("az")
            .args([
                "storage",
                "account",
                "show-connection-string",
                "--name",
                &storage_name,
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
    } else {
        tracing::warn!("No storage account found in resource group: {}", resource_group);
    }

    // Set error if cosmos connection failed (storage is optional)
    if result.cosmos_connection.is_none() {
        result.error = Some(format!(
            "Failed to fetch Cosmos DB connection string for '{}'. Check your Azure CLI permissions.",
            cosmos_account_name
        ));
    }

    Ok(result)
}

