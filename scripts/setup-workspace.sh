#!/bin/bash
# setup-workspace.sh - Clone all Mystira repositories as siblings
# Run this script from within Mystira.App directory

set -e

PARENT_DIR=$(dirname $(pwd))
GITHUB_ORG="phoenixvc"

# All Mystira repositories
repos=(
  "Mystira.Chain"
  "Mystira.Infra"
  "Mystira.StoryGenerator"
  "Mystira.workspace"
)

echo "=========================================="
echo "Mystira Workspace Setup"
echo "=========================================="
echo "Parent directory: $PARENT_DIR"
echo ""

# Check if we're in Mystira.App
if [[ ! -f "Mystira.App.sln" ]]; then
    echo "Warning: Run this script from within the Mystira.App directory"
    echo "Current directory: $(pwd)"
fi

echo "Cloning sibling repositories..."
echo ""

for repo in "${repos[@]}"; do
    repo_path="$PARENT_DIR/$repo"

    if [ -d "$repo_path" ]; then
        echo "[OK] $repo already exists"
    else
        echo "[CLONE] Cloning $repo..."
        if git clone "https://github.com/$GITHUB_ORG/$repo.git" "$repo_path" 2>/dev/null; then
            echo "  [OK] Cloned successfully"
        else
            echo "  [SKIP] Repository not accessible or doesn't exist yet"
        fi
    fi
done

echo ""
echo "=========================================="
echo "Setup complete!"
echo "=========================================="
echo ""
echo "Directory structure:"
ls -la "$PARENT_DIR"
echo ""
echo "To open workspace in VS Code:"
echo "  code $PARENT_DIR/Mystira.workspace/mystira.code-workspace"
echo ""
echo "Or if workspace not yet created, open individual repos:"
echo "  code $PARENT_DIR/Mystira.App"
