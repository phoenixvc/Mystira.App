//! Azure cloud resource management module.
//!
//! This module provides functionality for managing Azure resources, deployments,
//! and CLI interactions. It's organized into sub-modules:
//!
//! - [`cli`] - Azure CLI installation and availability checks
//! - [`deployment`] - Infrastructure deployment operations (deploy, validate, preview, status)
//! - [`resources`] - Resource management (list, delete, permissions)
//!
//! # Examples
//!
//! ## Checking Azure CLI
//! ```rust
//! use crate::azure::cli::check_azure_cli;
//! let status = check_azure_cli().await?;
//! ```
//!
//! ## Deploying Infrastructure
//! ```rust
//! use crate::azure::deployment::azure_deploy_infrastructure;
//! let result = azure_deploy_infrastructure(
//!     repo_root,
//!     "dev".to_string(),
//!     None, None, None, None, None
//! ).await?;
//! ```

pub mod cli;
pub mod deployment;
pub mod deploy_now;
pub mod resources;

