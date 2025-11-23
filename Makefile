.PHONY: help devhub devhub-dev devhub-build devhub-test clean

# Default target
help:
	@echo "Mystira.App - Available Commands"
	@echo "================================="
	@echo ""
	@echo "DevHub Commands:"
	@echo "  make devhub        - Build and launch DevHub in production mode"
	@echo "  make devhub-dev    - Launch DevHub in development mode (hot reload)"
	@echo "  make devhub-build  - Build DevHub for production"
	@echo "  make devhub-test   - Run DevHub test suite"
	@echo "  make clean         - Clean DevHub build artifacts"
	@echo ""

# Build and launch DevHub in production mode
devhub:
	@echo "🚀 Building and launching Mystira DevHub..."
	@cd tools/Mystira.DevHub && npm install && npm run build
	@echo "✅ Build complete! Launching application..."
	@cd tools/Mystira.DevHub && npm run tauri:build && echo "DevHub built successfully!"

# Launch DevHub in development mode
devhub-dev:
	@echo "🔧 Launching Mystira DevHub in development mode..."
	@cd tools/Mystira.DevHub && npm install && npm run tauri:dev

# Build DevHub for production
devhub-build:
	@echo "📦 Building Mystira DevHub for production..."
	@cd tools/Mystira.DevHub && npm install && npm run build
	@echo "✅ DevHub build complete!"

# Run DevHub tests
devhub-test:
	@echo "🧪 Running Mystira DevHub tests..."
	@cd tools/Mystira.DevHub && npm install && npm test -- --run
	@echo "✅ All tests passed!"

# Clean build artifacts
clean:
	@echo "🧹 Cleaning DevHub build artifacts..."
	@cd tools/Mystira.DevHub && rm -rf dist/ src-tauri/target/ node_modules/.vite/
	@echo "✅ Clean complete!"
