#!/bin/bash
# create-github-repos.sh - Create GitHub repos and push scaffolding
# Prerequisites: gh CLI authenticated (gh auth login)

set -e

GITHUB_ORG="phoenixvc"
PARENT_DIR="/home/user"

echo "=========================================="
echo "Create Mystira GitHub Repositories"
echo "=========================================="
echo ""

# Check gh CLI is available
if ! command -v gh &> /dev/null; then
    echo "Error: GitHub CLI (gh) not installed"
    echo "Install: https://cli.github.com/"
    exit 1
fi

# Check authentication
if ! gh auth status &> /dev/null; then
    echo "Error: Not authenticated with GitHub"
    echo "Run: gh auth login"
    exit 1
fi

echo "Authenticated as: $(gh auth status 2>&1 | grep 'Logged in' | head -1)"
echo ""

# ─────────────────────────────────────────────
# 1. Mystira.workspace
# ─────────────────────────────────────────────
echo "[1/3] Creating Mystira.workspace..."

if gh repo view "$GITHUB_ORG/Mystira.workspace" &> /dev/null; then
    echo "  [EXISTS] Repo already exists"
else
    gh repo create "$GITHUB_ORG/Mystira.workspace" \
        --private \
        --description "Unified workspace for Mystira multi-repo development - VS Code workspace, docs, and tooling" \
        --clone=false
    echo "  [CREATED] Repository created"
fi

# Push local scaffolding if exists
if [ -d "$PARENT_DIR/Mystira.workspace" ]; then
    cd "$PARENT_DIR/Mystira.workspace"
    if [ ! -d ".git" ]; then
        git init
        git branch -M main
    fi
    git add -A
    git commit -m "Initial commit: Mystira.workspace setup" 2>/dev/null || echo "  [SKIP] No changes to commit"
    git remote remove origin 2>/dev/null || true
    git remote add origin "https://github.com/$GITHUB_ORG/Mystira.workspace.git"
    git push -u origin main --force
    echo "  [PUSHED] Scaffolding pushed"
fi

# ─────────────────────────────────────────────
# 2. Mystira.Chain
# ─────────────────────────────────────────────
echo ""
echo "[2/3] Creating Mystira.Chain..."

if gh repo view "$GITHUB_ORG/Mystira.Chain" &> /dev/null; then
    echo "  [EXISTS] Repo already exists"
else
    gh repo create "$GITHUB_ORG/Mystira.Chain" \
        --private \
        --description "Blockchain integration service for Story Protocol IP registration and royalties" \
        --clone=false
    echo "  [CREATED] Repository created"
fi

# Push local scaffolding if exists
if [ -d "$PARENT_DIR/Mystira.Chain" ]; then
    cd "$PARENT_DIR/Mystira.Chain"
    if [ ! -d ".git" ]; then
        git init
        git branch -M main
    fi
    git add -A
    git commit -m "Initial commit: Mystira.Chain FastAPI service" 2>/dev/null || echo "  [SKIP] No changes to commit"
    git remote remove origin 2>/dev/null || true
    git remote add origin "https://github.com/$GITHUB_ORG/Mystira.Chain.git"
    git push -u origin main --force
    echo "  [PUSHED] Scaffolding pushed"
fi

# ─────────────────────────────────────────────
# 3. Mystira.Infra (optional - per ADR-0012)
# ─────────────────────────────────────────────
echo ""
echo "[3/3] Creating Mystira.Infra..."
echo "  Note: Per ADR-0012, this is for future migration"
echo "  Infrastructure currently lives in Mystira.App/infrastructure/"

read -p "  Create Mystira.Infra repo now? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    if gh repo view "$GITHUB_ORG/Mystira.Infra" &> /dev/null; then
        echo "  [EXISTS] Repo already exists"
    else
        gh repo create "$GITHUB_ORG/Mystira.Infra" \
            --private \
            --description "Infrastructure as Code for Mystira platform - Azure Bicep, networking, and deployment" \
            --clone=false
        echo "  [CREATED] Repository created"
    fi

    # Push local scaffolding if exists
    if [ -d "$PARENT_DIR/Mystira.Infra" ]; then
        cd "$PARENT_DIR/Mystira.Infra"
        if [ ! -d ".git" ]; then
            git init
            git branch -M main
        fi
        git add -A
        git commit -m "Initial commit: Mystira.Infra scaffolding" 2>/dev/null || echo "  [SKIP] No changes to commit"
        git remote remove origin 2>/dev/null || true
        git remote add origin "https://github.com/$GITHUB_ORG/Mystira.Infra.git"
        git push -u origin main --force
        echo "  [PUSHED] Scaffolding pushed"
    fi
else
    echo "  [SKIPPED] Mystira.Infra will be created during migration"
fi

# ─────────────────────────────────────────────
# Summary
# ─────────────────────────────────────────────
echo ""
echo "=========================================="
echo "Summary"
echo "=========================================="
echo ""
echo "GitHub Repositories:"
gh repo list "$GITHUB_ORG" --limit 10 | grep -E "Mystira\." || echo "  (check manually)"
echo ""
echo "Next steps:"
echo "  1. Clone workspace: git clone https://github.com/$GITHUB_ORG/Mystira.workspace.git .workspace"
echo "  2. Run setup: cd .workspace && ./scripts/setup.sh"
echo "  3. Open VS Code: code mystira.code-workspace"
