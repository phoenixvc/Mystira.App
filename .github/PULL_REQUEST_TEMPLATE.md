# Pull Request

## Description
<!-- Describe the changes in this PR -->

## Type of Change
<!-- Mark the relevant option with an [x] -->

- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Infrastructure/DevOps change
- [ ] Refactoring (no functional changes)

## Related Issues
<!-- Link to related issues using #issue_number -->

Closes #

## Testing Checklist

### Environment Testing
<!-- IMPORTANT: Test on BOTH platforms to ensure consistency -->

- [ ] **Tested on Dev Environment** (Azure App Service)
  - [ ] Functionality works as expected
  - [ ] No console errors
  - [ ] API calls succeed

- [ ] **Tested on SWA Preview** (check preview URL in PR comments)
  - [ ] Preview URL accessible and loads correctly
  - [ ] All routes work (navigation doesn't break)
  - [ ] Static assets load properly
  - [ ] No 404 errors for framework files

### SWA-Specific Validation
<!-- These are critical for production parity -->

- [ ] **Routing & Navigation**
  - [ ] Deep links work (e.g., `/adventures`, `/profile`)
  - [ ] Browser back/forward buttons work
  - [ ] 404 pages redirect to app correctly

- [ ] **Caching & Headers**
  - [ ] Blazor environment detected correctly (check console)
  - [ ] Service worker registers successfully (if PWA)
  - [ ] Static assets cached appropriately (check Network tab)

- [ ] **PWA Features** (if applicable)
  - [ ] Service worker updates properly
  - [ ] Offline mode works
  - [ ] Install prompt appears (on supported browsers)

### API Integration
- [ ] API calls succeed with correct CORS headers
- [ ] Authentication works (if applicable)
- [ ] Error handling works as expected

### Manual Testing
- [ ] Tested on desktop browser
- [ ] Tested on mobile device (if UI changes)
- [ ] Checked browser console for errors/warnings
- [ ] Verified responsive design (if UI changes)

## Code Quality

- [ ] Code follows project style guidelines
- [ ] Self-reviewed the code changes
- [ ] Added/updated comments for complex logic
- [ ] No unnecessary console.log statements left in code

## Tests

- [ ] Added/updated unit tests for new functionality
- [ ] All existing tests pass locally
- [ ] Added integration tests (if applicable)
- [ ] Smoke tests pass on SWA Preview (automated)

## Documentation

- [ ] Updated README.md (if needed)
- [ ] Updated API documentation (if applicable)
- [ ] Updated architecture docs (if significant changes)
- [ ] Added inline code comments for complex logic

## Security

- [ ] No secrets or sensitive data in code
- [ ] Input validation added for user inputs
- [ ] CORS configuration reviewed (if API changes)
- [ ] Authentication/authorization checked (if applicable)

## Screenshots
<!-- If UI changes, add before/after screenshots -->

### Before
<!-- Screenshot or description -->

### After
<!-- Screenshot or description -->

## Additional Notes
<!-- Any additional information reviewers should know -->

## Reviewer Checklist
<!-- For reviewers -->

- [ ] Code changes reviewed and approved
- [ ] Architecture patterns followed (Hexagonal/Clean)
- [ ] No business logic in controllers
- [ ] Tests are adequate and pass
- [ ] SWA Preview tested manually
- [ ] Security considerations addressed
- [ ] Documentation updated appropriately

---

**💡 Reminder**: Always test on **both Dev (App Service) AND SWA Preview** to catch platform-specific issues early!
