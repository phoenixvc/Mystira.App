//! Common data types and structures used throughout the application.
//!
//! This module defines:
//! - Request/Response types for command execution
//! - Service management types
//! - Error types for centralized error handling
//!
//! # Examples
//!
//! ## Using CommandResponse
//! ```rust
//! use crate::types::CommandResponse;
//!
//! let response = CommandResponse {
//!     success: true,
//!     result: Some(serde_json::json!({"data": "example"})),
//!     message: Some("Operation completed".to_string()),
//!     error: None,
//! };
//! ```
//!
//! ## Using AppError
//! ```rust
//! use crate::types::AppError;
//!
//! let error = AppError::CommandFailed {
//!     command: "example".to_string(),
//!     details: "Something went wrong".to_string(),
//! };
//! ```

use serde::{Deserialize, Serialize};
use std::fmt;

/// Centralized error types for the application
/// 
/// Note: Currently defined but not yet fully integrated across all modules.
/// Functions can gradually migrate from `Result<T, String>` to `Result<T, AppError>`.
#[allow(dead_code)]
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum AppError {
    /// Azure CLI is not installed or not available
    AzureCliMissing {
        winget_available: bool,
    },
    /// Command execution failed
    CommandFailed {
        command: String,
        details: String,
    },
    /// Invalid file or directory path
    InvalidPath(String),
    /// Network/HTTP request failed
    NetworkError(String),
    /// Resource not found
    ResourceNotFound(String),
    /// Permission denied or unauthorized
    PermissionDenied(String),
    /// Configuration error
    ConfigurationError(String),
    /// Generic error with message
    Other(String),
}

impl fmt::Display for AppError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            AppError::AzureCliMissing { winget_available } => {
                if *winget_available {
                    write!(f, "Azure CLI is not installed. You can install it automatically using winget.")
                } else {
                    write!(f, "Azure CLI is not installed. Please install it from https://aka.ms/installazurecliwindows")
                }
            }
            AppError::CommandFailed { command, details } => {
                write!(f, "Command '{}' failed: {}", command, details)
            }
            AppError::InvalidPath(path) => {
                write!(f, "Invalid path: {}", path)
            }
            AppError::NetworkError(msg) => {
                write!(f, "Network error: {}", msg)
            }
            AppError::ResourceNotFound(resource) => {
                write!(f, "Resource not found: {}", resource)
            }
            AppError::PermissionDenied(msg) => {
                write!(f, "Permission denied: {}", msg)
            }
            AppError::ConfigurationError(msg) => {
                write!(f, "Configuration error: {}", msg)
            }
            AppError::Other(msg) => {
                write!(f, "{}", msg)
            }
        }
    }
}

impl std::error::Error for AppError {}

// Convenience conversions
impl From<String> for AppError {
    fn from(s: String) -> Self {
        AppError::Other(s)
    }
}

impl From<&str> for AppError {
    fn from(s: &str) -> Self {
        AppError::Other(s.to_string())
    }
}

#[derive(Debug, Serialize, Deserialize)]
pub struct CommandRequest {
    pub command: String,
    pub args: serde_json::Value,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct CommandResponse {
    pub success: bool,
    pub result: Option<serde_json::Value>,
    pub message: Option<String>,
    pub error: Option<String>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ServiceStatus {
    pub name: String,
    pub running: bool,
    pub port: Option<u16>,
    pub url: Option<String>,
}

// Service info with process and log channel
#[derive(Clone)]
pub struct ServiceInfo {
    pub name: String,
    pub port: u16,
    pub url: Option<String>,
    pub pid: Option<u32>, // Store process ID for killing
}

// Global service manager - store service info
pub type ServiceManager = std::sync::Arc<std::sync::Mutex<std::collections::HashMap<String, ServiceInfo>>>;

