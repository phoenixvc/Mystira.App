# Mystira DevHub - Quick Start Guide

## 🚀 Launch Commands

Mystira DevHub can be launched from the root of the repository using either `make` or the `devhub.sh` script.

### Using Make (Recommended)

```bash
# From repository root
make devhub-dev    # Launch in development mode (hot reload)
make devhub-build  # Build for production
make devhub-test   # Run test suite
make devhub        # Build and launch production version
make help          # Show all available commands
```

### Using Shell Script

```bash
# From repository root
./devhub.sh dev    # Launch in development mode (hot reload)
./devhub.sh build  # Build for production
./devhub.sh test   # Run test suite
./devhub.sh launch # Build and launch (default)
./devhub.sh help   # Show help
```

## 📋 Prerequisites

Before launching DevHub, ensure you have:

1. **Node.js 18+** and **npm**
   ```bash
   node --version  # Should be 18.0.0 or higher
   npm --version
   ```

2. **Rust** (for Tauri)
   ```bash
   rustc --version
   cargo --version
   ```

3. **System Dependencies** (for Tauri on Linux)
   ```bash
   # Ubuntu/Debian
   sudo apt install libwebkit2gtk-4.0-dev \
     build-essential \
     curl \
     wget \
     libssl-dev \
     libgtk-3-dev \
     libayatana-appindicator3-dev \
     librsvg2-dev
   ```

## 🛠️ Development Workflow

### First Time Setup

```bash
# From repository root
cd tools/Mystira.DevHub
npm install
cd ../..
```

### Development Mode

Development mode provides hot reload for rapid development:

```bash
# Option 1: Using make
make devhub-dev

# Option 2: Using script
./devhub.sh dev

# Option 3: Directly from DevHub directory
cd tools/Mystira.DevHub
npm run tauri:dev
```

### Running Tests

```bash
# Option 1: Using make
make devhub-test

# Option 2: Using script
./devhub.sh test

# Option 3: Directly from DevHub directory
cd tools/Mystira.DevHub
npm test
```

### Production Build

```bash
# Option 1: Using make
make devhub-build

# Option 2: Using script
./devhub.sh build

# Option 3: Directly from DevHub directory
cd tools/Mystira.DevHub
npm run build
```

## 🔒 Pre-Commit Hook

The repository includes a pre-commit hook that automatically builds DevHub before each commit to ensure code quality. This hook:

- Runs `dotnet format` for C# code
- Builds the DevHub application
- Prevents commits if the build fails

### Bypassing Pre-Commit Hook (Not Recommended)

If you need to bypass the hook for a specific commit:

```bash
git commit --no-verify -m "your message"
```

## 🐛 Troubleshooting

### Build Fails in Pre-Commit Hook

If the pre-commit hook fails:

1. **Check the error message** - it will indicate what failed
2. **Run the build manually** to see detailed errors:
   ```bash
   cd tools/Mystira.DevHub
   npm run build
   ```
3. **Fix the errors** and try committing again

### Dependencies Not Found

If you get dependency errors:

```bash
cd tools/Mystira.DevHub
rm -rf node_modules package-lock.json
npm install
```

### Tauri Build Errors

If Tauri build fails:

```bash
# Check Rust installation
rustc --version
cargo --version

# Update Rust
rustup update

# Clean Tauri cache
cd tools/Mystira.DevHub
rm -rf src-tauri/target
```

## 📖 More Information

- **Full Documentation:** [README.md](README.md)
- **Configuration Guide:** [CONFIGURATION.md](CONFIGURATION.md)
- **Security Best Practices:** [SECURITY.md](SECURITY.md)

## 🎯 Quick Links

- **Repository Root:** `/home/user/Mystira.App`
- **DevHub Directory:** `/home/user/Mystira.App/tools/Mystira.DevHub`
- **Pre-Commit Hook:** `/home/user/Mystira.App/.husky/pre-commit`

---

**Need help?** Check the main README or open an issue on GitHub.
