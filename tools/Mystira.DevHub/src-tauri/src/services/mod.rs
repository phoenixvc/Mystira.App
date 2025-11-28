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

// Re-export all public functions
pub use lifecycle::{prebuild_service, start_service, stop_service};
pub use status::{get_service_status, check_service_health};
pub use ports::{check_port_available, get_service_port, update_service_port, find_available_port};

