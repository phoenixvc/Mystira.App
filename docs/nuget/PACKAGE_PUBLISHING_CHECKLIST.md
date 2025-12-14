# Package Publishing Checklist

## Pre-Publishing Checklist

Before publishing packages to the internal NuGet feed, ensure:

- [ ] All 8 shared library `.csproj` files have package metadata
- [ ] Package IDs match convention: `Mystira.App.{Library}`
- [ ] Initial version set to `1.0.0`
- [ ] Package descriptions are accurate and descriptive
- [ ] Repository URL points to correct GitHub repository
- [ ] All projects build successfully
- [ ] All tests pass
- [ ] Azure DevOps Artifacts feed is created
- [ ] GitHub Secrets are configured (MYSTIRA_DEVOPS_AZURE_ORG, MYSTIRA_DEVOPS_AZURE_PROJECT, MYSTIRA_DEVOPS_AZURE_PAT, MYSTIRA_DEVOPS_NUGET_FEED)

## Package Metadata Checklist

For each shared library, verify:

- [ ] `<PackageId>` - Matches naming convention
- [ ] `<Version>` - Set to `1.0.0` for initial release
- [ ] `<Authors>` - "Mystira Team"
- [ ] `<Company>` - "Phoenix VC"
- [ ] `<Description>` - Clear, descriptive description
- [ ] `<RepositoryUrl>` - Points to `https://github.com/phoenixvc/Mystira.App`
- [ ] `<RepositoryType>` - "git"
- [ ] `<PackageLicenseExpression>` - "PROPRIETARY"
- [ ] `<GeneratePackageOnBuild>` - "false" (we control when to pack)
- [ ] `<IncludeSymbols>` - "true"
- [ ] `<SymbolPackageFormat>` - "snupkg"
- [ ] `<PackageTags>` - Relevant tags for discoverability

## Libraries to Package

1. ✅ `Mystira.App.Domain`
2. ✅ `Mystira.App.Application`
3. ✅ `Mystira.App.Contracts`
4. ✅ `Mystira.App.Infrastructure.Azure`
5. ✅ `Mystira.App.Infrastructure.Data`
6. ✅ `Mystira.App.Infrastructure.Discord`
7. ✅ `Mystira.App.Infrastructure.StoryProtocol`
8. ✅ `Mystira.App.Shared`

## Publishing Steps

### Manual Publishing (First Time)

1. **Build and test**:
   ```bash
   dotnet build Mystira.App.sln --configuration Release
   dotnet test Mystira.App.sln --configuration Release
   ```

2. **Pack all libraries**:
   ```bash
   dotnet pack src/Mystira.App.Domain/Mystira.App.Domain.csproj --configuration Release --output ./nupkg
   dotnet pack src/Mystira.App.Application/Mystira.App.Application.csproj --configuration Release --output ./nupkg
   dotnet pack src/Mystira.App.Contracts/Mystira.App.Contracts.csproj --configuration Release --output ./nupkg
   dotnet pack src/Mystira.App.Infrastructure.Azure/Mystira.App.Infrastructure.Azure.csproj --configuration Release --output ./nupkg
   dotnet pack src/Mystira.App.Infrastructure.Data/Mystira.App.Infrastructure.Data.csproj --configuration Release --output ./nupkg
   dotnet pack src/Mystira.App.Infrastructure.Discord/Mystira.App.Infrastructure.Discord.csproj --configuration Release --output ./nupkg
   dotnet pack src/Mystira.App.Infrastructure.StoryProtocol/Mystira.App.Infrastructure.StoryProtocol.csproj --configuration Release --output ./nupkg
   dotnet pack src/Mystira.App.Shared/Mystira.App.Shared.csproj --configuration Release --output ./nupkg
   ```

3. **Verify packages**:
   - Check `nupkg` folder contains 8 `.nupkg` files
   - Verify package names and versions

4. **Configure NuGet source** (if not already done):
   ```bash
   dotnet nuget add source https://pkgs.dev.azure.com/{org}/{project}/_packaging/{feed}/nuget/v3/index.json \
     --name "Mystira-Internal" \
     --username "{email}" \
     --password "{pat-token}"
   ```

5. **Publish packages**:
   ```bash
   dotnet nuget push ./nupkg/*.nupkg --source "Mystira-Internal" --api-key "{pat-token}"
   ```

6. **Verify in Azure DevOps**:
   - Go to Artifacts → Mystira-Internal feed
   - Verify all 8 packages appear with version `1.0.0`

### Automated Publishing (After Setup)

Once CI/CD workflow is configured:

1. Make changes to shared libraries
2. Commit and push to `main` branch
3. Workflow automatically detects changes
4. Packages are built and published automatically
5. Check workflow run status in GitHub Actions

## Post-Publishing Verification

- [ ] All packages visible in Azure DevOps Artifacts feed
- [ ] Package versions are `1.0.0`
- [ ] Package metadata (description, authors, etc.) is correct
- [ ] Symbols packages (.snupkg) are available
- [ ] Can restore packages in a test project
- [ ] Package dependencies resolve correctly

## Testing Package Consumption

Create a test project to verify packages can be consumed:

```bash
# Create test project
dotnet new classlib -n TestPackageConsumption
cd TestPackageConsumption

# Add package reference
dotnet add package Mystira.App.Domain --version 1.0.0 --source "Mystira-Internal"

# Restore and build
dotnet restore
dotnet build
```

If successful, packages are correctly configured and accessible.

## Troubleshooting

### Build Errors

**Issue**: Build fails after adding package metadata

**Check**:
- XML syntax in `.csproj` files
- All property groups properly closed
- No duplicate property definitions

### Pack Errors

**Issue**: `dotnet pack` fails

**Check**:
- Project builds successfully first
- All dependencies can be resolved
- No circular dependencies

### Publish Errors

**Issue**: `dotnet nuget push` fails with authentication error

**Check**:
- NuGet source is correctly configured
- PAT token has correct permissions (Packaging Read & Write)
- PAT token hasn't expired
- Feed URL is correct

**Issue**: Package already exists error

**Solution**: Use `--skip-duplicate` flag or increment version

### Package Not Found

**Issue**: Can't restore package after publishing

**Check**:
- Package name matches exactly (case-sensitive)
- Version number matches
- Feed URL is correct in consuming project's NuGet.config
- Feed permissions allow read access

## Next Steps

After initial packages are published:

1. Update migration plan Phase 1 status
2. Proceed to Phase 2: Create Admin API repository
3. Test Admin API can consume packages
4. Document any issues or learnings

