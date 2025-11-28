//! DevHub CLI execution wrapper.
//!
//! This module provides a bridge to execute commands via the DevHub CLI tool.
//! It handles process spawning, stdin/stdout communication, and response parsing.
//!
//! # Architecture
//!
//! The CLI tool is a separate executable that accepts JSON-formatted command requests
//! via stdin and returns JSON-formatted responses via stdout. This module handles
//! the communication protocol.
//!
//! # Error Handling
//!
//! Errors are returned as `String` messages. For "Unknown command" errors, specific
//! error messages are provided to help with debugging.

use crate::helpers::get_cli_executable_path;
use crate::types::CommandRequest;
use crate::types::CommandResponse;
use std::process::{Command, Stdio};
use std::io::Write;

/// Execute a command via the DevHub CLI tool
pub async fn execute_devhub_cli(command: String, args: serde_json::Value) -> Result<CommandResponse, String> {
    // Validate command is not empty
    let command_trimmed = command.trim();
    if command_trimmed.is_empty() {
        return Err(format!("Command cannot be empty. Received command: '{}'", command));
    }

    let request = CommandRequest {
        command: command_trimmed.to_string(),
        args,
    };

    let request_json = serde_json::to_string(&request)
        .map_err(|e| format!("Failed to serialize request: {}. Command was: '{}'", e, command_trimmed))?;

    // Get the CLI executable path
    let cli_exe_path = get_cli_executable_path()?;
    
    // Validate the executable exists
    if !cli_exe_path.exists() {
        return Err(format!(
            "CLI executable not found at: {}\n\n\
             Please build the CLI first:\n\
             1. Open a terminal\n\
             2. Navigate to: tools/Mystira.DevHub.CLI\n\
             3. Run: dotnet build",
            cli_exe_path.display()
        ));
    }

    // Spawn the .NET process
    let mut child = Command::new(&cli_exe_path)
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| {
            let error_msg = if e.kind() == std::io::ErrorKind::NotFound {
                format!(
                    "Program not found: {}\n\n\
                     The Mystira.DevHub.CLI executable was not found at the expected location.\n\
                     Please build the CLI first:\n\
                     1. Open a terminal\n\
                     2. Navigate to: tools/Mystira.DevHub.CLI\n\
                     3. Run: dotnet build",
                    cli_exe_path.display()
                )
            } else {
                format!("Failed to spawn process at {}: {}", cli_exe_path.display(), e)
            };
            error_msg
        })?;

    // Write request JSON to stdin
    if let Some(mut stdin) = child.stdin.take() {
        stdin
            .write_all(request_json.as_bytes())
            .map_err(|e| format!("Failed to write to CLI stdin: {}", e))?;
        stdin
            .write_all(b"\n")
            .map_err(|e| format!("Failed to write newline to CLI stdin: {}", e))?;
    }

    // Wait for the process to complete
    let output = child
        .wait_with_output()
        .map_err(|e| format!("Failed to wait for CLI process: {}", e))?;

    // Parse the response
    if output.status.success() {
        let stdout = String::from_utf8_lossy(&output.stdout);
        let response: CommandResponse = serde_json::from_str(&stdout)
            .map_err(|e| {
                format!(
                    "Failed to parse CLI response as JSON: {}. Raw output: {}",
                    e, stdout
                )
            })?;
        Ok(response)
    } else {
        let stderr = String::from_utf8_lossy(&output.stderr);
        let stdout = String::from_utf8_lossy(&output.stdout);
        Err(format!(
            "CLI process failed with exit code: {}\nStderr: {}\nStdout: {}",
            output.status.code().unwrap_or(-1),
            stderr,
            stdout
        ))
    }
}

