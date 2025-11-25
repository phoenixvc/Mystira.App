#!/bin/bash
# Mystira DevHub Launcher Script
# Usage: ./devhub.sh [command]
# Commands: dev, build, test, launch, help

set -e

DEVHUB_DIR="tools/Mystira.DevHub"

show_help() {
    echo "Mystira DevHub Launcher"
    echo "======================="
    echo ""
    echo "Usage: ./devhub.sh [command]"
    echo ""
    echo "Commands:"
    echo "  dev     - Launch DevHub in development mode (hot reload)"
    echo "  build   - Build DevHub for production"
    echo "  test    - Run DevHub test suite"
    echo "  launch  - Build and launch DevHub (default)"
    echo "  help    - Show this help message"
    echo ""
}

ensure_deps() {
    echo "📦 Ensuring dependencies are installed..."
    cd "$DEVHUB_DIR" && npm install && cd ../..
}

dev_mode() {
    echo "🔧 Launching Mystira DevHub in development mode..."
    ensure_deps
    cd "$DEVHUB_DIR" && npm run tauri:dev
}

build_app() {
    echo "📦 Building Mystira DevHub..."
    ensure_deps
    cd "$DEVHUB_DIR" && npm run build
    echo "✅ DevHub build complete!"
}

run_tests() {
    echo "🧪 Running Mystira DevHub tests..."
    ensure_deps
    cd "$DEVHUB_DIR" && npm test -- --run
    echo "✅ All tests passed!"
}

launch_app() {
    echo "🚀 Building and launching Mystira DevHub..."
    ensure_deps
    build_app
    echo "✅ Build complete! Launching application..."
    cd "$DEVHUB_DIR" && npm run tauri:build
}

# Main command dispatcher
case "${1:-launch}" in
    dev)
        dev_mode
        ;;
    build)
        build_app
        ;;
    test)
        run_tests
        ;;
    launch)
        launch_app
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        echo "❌ Unknown command: $1"
        echo ""
        show_help
        exit 1
        ;;
esac
