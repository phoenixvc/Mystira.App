# ADR-0011: Unified Workspace Repository (Mystira.workspace)

**Status**: 💭 Proposed

**Date**: 2025-12-10

**Deciders**: Development Team

**Tags**: architecture, monorepo, workspace, developer-experience, ai-tooling

---

## Context

The Mystira ecosystem has grown organically with multiple repositories and applications:

### Current State

- **2 repositories** currently exist:
  - `Mystira.App` - Main application (API, Admin API, PWA)
  - `Mystira.StoryGenerator` - Story generation functionality
- **4+ applications** are running across these repos:
  - Mystira.App (main application - API, Admin API, PWA)
  - Admin UI
  - Sorting UI (v0-generated)
  - Other v0-generated components
- Applications started together in shared repos "for convenience"
- New service planned: `mystira-chain-service` (from ADR-0010)

### Problems Identified

Based on team discussion:

1. **Fragmented Codebase Visibility**
   - Difficult to see all code at once
   - AI assistants (Claude, Copilot) struggle with cross-repo context
   - "jinne copilot is finicky... expand your scope and then address..."

2. **v0-Generated Code Management**
   - v0 can now create GitHub repos directly
   - Previously difficult to edit v0 code outside of v0
   - Issue: v0 doesn't pick up on externally changed code
   - Same issue exists in Replit and Bolt

3. **Documentation Scattered**
   - Docs spread across multiple repos
   - No single source of truth
   - Hard to maintain consistency

4. **Developer Experience**
   - Switching between repos is friction
   - Different setup processes per repo
   - No unified development environment

5. **AI-Assisted Development**
   - AI tools work best with full codebase visibility
   - Current setup limits AI effectiveness
   - "dis net vir my (en die ai) om al die code op een slag sama te kan sien"

### Repository Naming Considerations

Options discussed:
- `Mystira.orchestration` - orchestration/coordination focus
- `Mystira.workspace` - workspace/development focus
- `mystira-workspace` - hyphenated style (GitHub convention)

**Note**: GitHub repository names cannot start with a dot (`.`), so `.workspace`, `.main`, `.entry` are not valid GitHub repo names. However, these can still be used as **local directory names** when cloning.

**Naming strategy**: Use a valid GitHub repo name that can optionally be cloned to a dot-prefixed local directory for sorting benefits.

---

## Decision Drivers

1. **AI Tooling Effectiveness**: Enable AI assistants to see full codebase context
2. **Developer Convenience**: Single place to work on all Mystira code
3. **Documentation Centralization**: Single source for all docs
4. **Flexibility**: Don't force workspace usage on all team members
5. **v0 Compatibility**: Work around v0's code sync limitations
6. **VS Code Workspaces**: Leverage improved multi-root workspace support

---

## Considered Options

### Option 1: True Monorepo Migration

**Description**: Migrate all code into a single repository with proper monorepo tooling (Nx, Turborepo, or similar).

**Pros**:
- ✅ Single source of truth
- ✅ Atomic commits across all projects
- ✅ Simplified CI/CD
- ✅ Full AI visibility

**Cons**:
- ❌ Major migration effort
- ❌ Breaks existing workflows
- ❌ v0-generated code still problematic
- ❌ Forces everyone into same structure
- ❌ Git history complexity

### Option 2: Workspace Repository with Git Submodules

**Description**: Create `.workspace` repo that includes other repos as git submodules.

**Pros**:
- ✅ All code visible in one place
- ✅ Individual repos remain independent
- ✅ Can update submodules selectively
- ✅ Works with VS Code multi-root workspaces

**Cons**:
- ❌ Submodule complexity (detached HEAD, sync issues)
- ❌ Nested git operations confusing
- ❌ CI/CD complications
- ❌ Clone time increases

### Option 3: VS Code Multi-Root Workspace ⭐ **RECOMMENDED**

**Description**: Create `Mystira.workspace` repository (cloned locally as `.workspace`) containing:
- VS Code workspace file (`.code-workspace`)
- Shared documentation
- Cross-repo scripts and tooling
- References to other repos (cloned as siblings)

```
~/mystira/
├── .workspace/                # Mystira.workspace cloned as .workspace (sorts first!)
│   ├── mystira.code-workspace # VS Code workspace file
│   ├── docs/                  # Centralized documentation
│   ├── scripts/               # Cross-repo automation
│   └── README.md
├── Mystira.App/               # Main app (API, Admin API, PWA)
├── Mystira.StoryGenerator/    # Story generation service
├── mystira-chain-service/     # Blockchain service (Python)
└── [future repos]/            # Future repos (siblings)
```

**Pros**:
- ✅ All code visible to developer and AI
- ✅ Individual repos stay independent
- ✅ No submodule complexity
- ✅ Optional usage ("jy hoef dit obviously nie te gebruik nie")
- ✅ VS Code workspaces "werk deesdae darem al baie beter"
- ✅ Easy to add/remove repos
- ✅ Centralized docs without moving code

**Cons**:
- ⚠️ Requires cloning multiple repos
- ⚠️ Not a true monorepo (separate git histories)
- ⚠️ Cross-repo changes need multiple commits

### Option 4: GitHub Codespaces with Dev Container

**Description**: Use Codespaces with a dev container that clones all repos automatically.

**Pros**:
- ✅ Consistent environment
- ✅ Cloud-based development
- ✅ Auto-setup of all repos

**Cons**:
- ❌ Requires GitHub Codespaces subscription
- ❌ Latency for some developers
- ❌ Doesn't solve local development needs

---

## Decision

We will adopt **Option 3: VS Code Multi-Root Workspace** with the following implementation:

### Repository Naming Decision

**Recommended: GitHub repo `Mystira.workspace` → clone locally as `.workspace`**

| GitHub Repo Name | Local Clone | Pros | Cons |
|------------------|-------------|------|------|
| `Mystira.workspace` ⭐ | `.workspace` | Follows naming convention, can clone to dot-dir | Requires clone with rename |
| `mystira-workspace` | `.workspace` | GitHub convention (hyphen) | Inconsistent with `Mystira.App` |
| `Mystira.orchestration` | as-is | Explicit purpose | Long, doesn't sort first |

**Recommendation**:
- **GitHub repo**: `Mystira.workspace` (follows `Mystira.App` naming pattern)
- **Local directory**: Clone as `.workspace` for alphabetical sorting benefit

```bash
# Clone with custom local directory name
git clone https://github.com/phoenixvc/Mystira.workspace.git .workspace
```

### Repository Structure

Create new repository: `Mystira.workspace` (cloned locally as `.workspace`)

```
.workspace/   # Local directory name (GitHub repo: Mystira.workspace)
├── mystira.code-workspace      # VS Code multi-root workspace
├── docs/
│   ├── architecture/           # Centralized architecture docs
│   │   ├── adr/               # All ADRs (moved/synced from repos)
│   │   └── diagrams/          # System diagrams
│   ├── getting-started/       # Onboarding guides
│   ├── api/                   # API documentation
│   └── runbooks/              # Operational guides
├── scripts/
│   ├── setup.sh               # Clone all repos script
│   ├── setup.ps1              # Windows setup script
│   └── update-all.sh          # Pull all repos
├── .vscode/
│   ├── settings.json          # Shared VS Code settings
│   ├── extensions.json        # Recommended extensions
│   └── tasks.json             # Cross-repo tasks
├── .claude/                   # Claude Code configuration
│   └── settings.json          # AI assistant settings
└── README.md                  # Getting started guide
```

### Workspace File

```jsonc
// mystira.code-workspace
{
  "folders": [
    {
      "name": "📋 .workspace",
      "path": "."
    },
    {
      "name": "🎮 Mystira.App",
      "path": "../Mystira.App"
    },
    {
      "name": "📖 Mystira.StoryGenerator",
      "path": "../Mystira.StoryGenerator"
    },
    {
      "name": "🐍 mystira-chain-service",
      "path": "../mystira-chain-service"
    }
    // Add more repos as needed
  ],
  "settings": {
    "files.exclude": {
      "**/bin": true,
      "**/obj": true,
      "**/node_modules": true
    },
    "search.exclude": {
      "**/bin": true,
      "**/obj": true,
      "**/node_modules": true
    }
  },
  "extensions": {
    "recommendations": [
      "ms-dotnettools.csdevkit",
      "dbaeumer.vscode-eslint",
      "esbenp.prettier-vscode"
    ]
  }
}
```

### Setup Script

**Initial clone** (one-time):
```bash
# Clone Mystira.workspace as .workspace for sorting benefit
cd ~/mystira  # or your preferred parent directory
git clone https://github.com/phoenixvc/Mystira.workspace.git .workspace
cd .workspace
./scripts/setup.sh
```

**scripts/setup.sh** - Clone sibling repositories:
```bash
#!/bin/bash
# scripts/setup.sh - Clone all Mystira repositories as siblings

PARENT_DIR=$(dirname $(pwd))
GITHUB_ORG="phoenixvc"

repos=(
  "Mystira.App"
  "Mystira.StoryGenerator"
  "mystira-chain-service"
  # Add more repos here
)

echo "🚀 Setting up Mystira workspace..."

for repo in "${repos[@]}"; do
  if [ -d "$PARENT_DIR/$repo" ]; then
    echo "✅ $repo already exists"
  else
    echo "📥 Cloning $repo..."
    git clone "https://github.com/$GITHUB_ORG/$repo.git" "$PARENT_DIR/$repo"
  fi
done

echo ""
echo "✨ Setup complete! Open mystira.code-workspace in VS Code"
echo "   code mystira.code-workspace"
```

### Documentation Strategy

The `.workspace` repo becomes the **primary source for documentation**:

1. **Architecture docs** - ADRs, diagrams, patterns
2. **Getting started guides** - Onboarding for new developers
3. **Cross-cutting concerns** - Auth, deployment, monitoring
4. **API documentation** - Aggregated API docs

Individual repos keep:
- README with repo-specific setup
- Code comments and inline docs
- Repo-specific configuration docs

### AI Assistant Configuration

Include `.claude/` directory for Claude Code settings:

```json
{
  "context": {
    "include_patterns": [
      "**/*.cs",
      "**/*.ts",
      "**/*.py",
      "**/*.md"
    ],
    "exclude_patterns": [
      "**/bin/**",
      "**/obj/**",
      "**/node_modules/**"
    ]
  }
}
```

---

## Consequences

### Positive Consequences ✅

1. **Full Codebase Visibility**
   - AI assistants can see all code at once
   - Developers can search across all projects
   - Better understanding of system as a whole

2. **Centralized Documentation**
   - Single source of truth for docs
   - Easier to maintain consistency
   - Better discoverability

3. **Optional Adoption**
   - Team members can use or not use workspace
   - Individual repos remain fully functional
   - No forced workflow changes

4. **Improved Developer Experience**
   - Single window for all Mystira code
   - Shared VS Code settings and extensions
   - Cross-repo tasks and scripts

5. **Better AI-Assisted Development**
   - Claude, Copilot, etc. have full context
   - More accurate suggestions
   - Can reason about cross-repo changes

6. **v0 Workaround**
   - v0-generated code stays in its own repo
   - Can edit in workspace without v0 sync issues
   - Accept that v0 won't pick up external changes

### Negative Consequences ❌

1. **Multiple Clones Required**
   - More disk space
   - More repos to keep updated
   - Mitigated by: setup scripts, update scripts

2. **Not True Monorepo**
   - Cross-repo changes need multiple commits
   - No atomic cross-repo commits
   - Mitigated by: clear commit conventions

3. **Potential Sync Issues**
   - Repos can drift apart
   - Mitigated by: CI checks, workspace validation

4. **Initial Setup Overhead**
   - New developers need to run setup script
   - Mitigated by: clear documentation, automation

---

## Implementation Plan

### Phase 1: Repository Creation

1. Create `Mystira.workspace` repository on GitHub
2. Add workspace file referencing Mystira.App
3. Add setup scripts (with clone-as-.workspace instructions)
4. Add initial documentation structure

### Phase 2: Documentation Migration

1. Identify docs to centralize
2. Move/copy architecture docs
3. Create getting-started guides
4. Set up doc generation if needed

### Phase 3: Tooling Integration

1. Add cross-repo VS Code tasks
2. Configure AI assistant settings
3. Add development scripts
4. Set up workspace validation CI

### Phase 4: Team Adoption

1. Document workspace usage
2. Onboard team members
3. Gather feedback
4. Iterate on structure

---

## v0 Code Management Strategy

Given the limitations with v0 (and similar tools like Replit, Bolt):

1. **Accept One-Way Sync**: v0 generates code → we pull into repo
2. **Don't Edit v0 Code in v0**: Once in our repo, treat it as our code
3. **Regenerate if Needed**: If major v0 changes needed, regenerate
4. **Document v0 Origins**: Note which code came from v0

This matches the experience: "dit het nie so lekker gewerk om dit buite v0 te edit nie"

---

## Related Decisions

- **ADR-0010**: Story Protocol SDK Integration (chain service will be in workspace)
- **ADR-0005**: Separate API and Admin API (both visible in workspace)

---

## References

- [VS Code Multi-Root Workspaces](https://code.visualstudio.com/docs/editor/multi-root-workspaces)
- [Monorepo vs Multi-Repo](https://blog.nrwl.io/misconceptions-about-monorepos-monorepo-monolith-df1250d4b03c)

---

## Notes

- Workspace approach preferred over submodules due to complexity
- VS Code workspaces have improved significantly since preview
- Team member can opt out ("jy hoef dit obviously nie te gebruik nie")
- GitHub repo: `Mystira.workspace` (follows Mystira.App naming convention)
- Local clone: `.workspace` (optional, for alphabetical sorting benefit)
- GitHub repos cannot start with `.` - hence the two-name approach

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
