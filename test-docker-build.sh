#!/bin/bash
# test-docker-build.sh - Test script to verify Docker build configuration

set -e

echo "========================================"
echo "Docker Build Configuration Test"
echo "========================================"
echo ""

# Check if running from repository root
if [ ! -f "Mystira.App.sln" ]; then
    echo "❌ ERROR: This script must be run from the repository root"
    exit 1
fi

echo "✅ Running from repository root"
echo ""

# Verify .dockerignore exists
if [ ! -f ".dockerignore" ]; then
    echo "❌ ERROR: .dockerignore not found"
    exit 1
fi
echo "✅ .dockerignore found"

# Verify Dockerfiles exist
if [ ! -f "src/Mystira.App.Api/Dockerfile" ]; then
    echo "❌ ERROR: src/Mystira.App.Api/Dockerfile not found"
    exit 1
fi
echo "✅ src/Mystira.App.Api/Dockerfile found"

if [ ! -f "src/Mystira.App.Admin.Api/Dockerfile" ]; then
    echo "❌ ERROR: src/Mystira.App.Admin.Api/Dockerfile not found"
    exit 1
fi
echo "✅ src/Mystira.App.Admin.Api/Dockerfile found"

# Verify appsettings files exist
if [ ! -f "src/Mystira.App.Api/appsettings.json" ]; then
    echo "❌ ERROR: src/Mystira.App.Api/appsettings.json not found"
    exit 1
fi
echo "✅ src/Mystira.App.Api/appsettings.json found"

if [ ! -f "src/Mystira.App.Admin.Api/appsettings.json" ]; then
    echo "❌ ERROR: src/Mystira.App.Admin.Api/appsettings.json not found"
    exit 1
fi
echo "✅ src/Mystira.App.Admin.Api/appsettings.json found"

# Verify docker-compose.yml exists
if [ ! -f "docker-compose.yml" ]; then
    echo "❌ ERROR: docker-compose.yml not found"
    exit 1
fi
echo "✅ docker-compose.yml found"

echo ""
echo "========================================"
echo "Configuration Check: PASSED ✅"
echo "========================================"
echo ""
echo "To build Docker images, run:"
echo "  docker build -f src/Mystira.App.Api/Dockerfile -t mystira-api:latest ."
echo "  docker build -f src/Mystira.App.Admin.Api/Dockerfile -t mystira-admin-api:latest ."
echo ""
echo "Or use docker-compose:"
echo "  docker-compose build"
echo "  docker-compose up"
echo ""
