@echo off
REM DevHub launcher - Windows batch file wrapper for start.ps1
REM This allows running DevHub from the repository root

powershell.exe -ExecutionPolicy Bypass -File "%~dp0tools\Mystira.DevHub\start.ps1"

