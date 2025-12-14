@echo off
REM DevHub launcher - Windows batch file wrapper for start.ps1
REM This allows running DevHub from the repository root
REM DevHub is now a git submodule at tools/Mystira.DevHub

powershell.exe -ExecutionPolicy Bypass -File "%~dp0tools\Mystira.DevHub\start.ps1"

