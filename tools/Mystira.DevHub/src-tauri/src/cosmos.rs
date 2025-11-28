//! Cosmos DB operations module.
//!
//! This module provides commands for managing Cosmos DB:
//! - Data export to CSV
//! - Statistics and metrics
//! - Migration operations between Cosmos DB instances
//!
//! All operations are executed via the DevHub CLI tool.

use crate::cli::execute_devhub_cli;
use crate::types::CommandResponse;

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

