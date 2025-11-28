//! Azure infrastructure deployment operations.
//!
//! This module handles the complete deployment lifecycle:
//! - [`deploy`] - Deploy infrastructure using Bicep templates
//! - [`validate`] - Validate Bicep templates before deployment
//! - [`preview`] - Preview changes using Azure what-if
//! - [`status`] - Check infrastructure existence and status
//! - [`helpers`] - Shared utility functions for deployment operations
//!
//! All operations use Azure CLI and follow the incremental deployment pattern
//! to prevent accidental resource deletion.

pub mod deploy;
pub mod validate;
pub mod preview;
pub mod status;
pub mod helpers;

// Re-export all public functions
pub use deploy::azure_deploy_infrastructure;
pub use validate::azure_validate_infrastructure;
pub use preview::azure_preview_infrastructure;
pub use status::{check_infrastructure_exists, check_infrastructure_status};

