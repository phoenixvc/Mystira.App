//! Service lifecycle management module.
//!
//! This module provides complete service management functionality:
//! - Lifecycle operations (start, stop, prebuild) in `lifecycle.rs`
//! - Status and health checks in `status.rs`
//! - Port management in `ports.rs`
//! - Shared utilities in `helpers.rs`

pub mod lifecycle;
pub mod status;
pub mod ports;
pub mod helpers;

// Note: Functions are imported directly from sub-modules in main.rs
// Re-exports are not needed since main.rs imports from specific paths

