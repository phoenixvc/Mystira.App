# Step 2 UI Flow Recommendation

## Current Implementation
Step 2 (Infrastructure Actions) is currently rendered inline below Step 1 in the same tab, with a scroll-to functionality when clicking "Continue to Step 2".

## Recommendation: **Hybrid Approach - Enhanced Inline with Optional Modal**

### Primary Recommendation: **Keep Inline, Enhance UX**

**Pros:**
- ✅ Seamless workflow - users can see Step 1 context while performing Step 2 actions
- ✅ No modal overhead - faster navigation
- ✅ Better for users who need to reference Step 1 templates while validating/previewing
- ✅ Works well on large screens
- ✅ Already implemented - less refactoring needed

**Enhancements to Current Approach:**
1. **Visual Separation**: Add a clear visual divider between Step 1 and Step 2 with a subtle background color change
2. **Sticky Header**: Make Step 2 header sticky when scrolling past Step 1
3. **Progress Indicator**: Add a progress stepper (Step 1 → Step 2) at the top
4. **Collapsible Step 1**: Optionally collapse Step 1 after moving to Step 2 to save space
5. **Better Scroll Behavior**: Ensure smooth scroll with proper offset for sticky header

### Alternative: **Modal/Dialog Approach**

**Pros:**
- ✅ Focused experience - user commits to Step 2 workflow
- ✅ Better on smaller screens
- ✅ Clear separation of concerns
- ✅ Easy to add "Back" functionality

**Cons:**
- ❌ Loses context from Step 1
- ❌ More clicks to switch between steps
- ❌ Modal can feel constraining for long operations
- ❌ Harder to reference template selections while previewing

**When to Use Modal:**
- If users frequently need to go back and change selections
- If Step 2 actions are very different from Step 1 (e.g., different environment)
- If we want to enforce a strict workflow (can't skip steps)

### Recommendation Decision Matrix

| Factor | Inline (Enhanced) | Modal |
|--------|-------------------|-------|
| Screen Real Estate | Better | Worse (constrained) |
| Context Preservation | ✅ Excellent | ❌ Lost |
| Focus & Flow | Good | ✅ Excellent |
| Mobile Experience | Moderate | ✅ Better |
| Development Effort | Low (enhance existing) | Medium (new component) |
| User Preference | Likely better | Likely worse |

## Final Recommendation

**Enhance the inline approach** with:
1. Visual separation and sticky Step 2 header
2. Progress indicator
3. Optional "Collapse Step 1" button when Step 2 is active
4. Smooth scroll improvements

**Keep modal as future enhancement** if user feedback indicates:
- Need for stricter workflow enforcement
- Difficulty focusing on Step 2 actions
- Preference for "wizard-style" flow

## Implementation Notes

Current code location:
- `InfrastructurePanel.tsx`: Step 2 section at line ~1302
- `ProjectDeploymentPlanner.tsx`: "Continue to Step 2" button at line ~469

Suggested enhancements:
- Add `sticky top-0 z-10 bg-white dark:bg-gray-900` to Step 2 header
- Add progress stepper component
- Add collapse/expand toggle for Step 1 section

