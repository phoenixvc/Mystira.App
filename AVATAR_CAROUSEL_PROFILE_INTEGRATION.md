# Avatar Carousel - Profile Management Integration

## Overview
The Avatar Carousel has been successfully integrated into the Profile Management page (`/profiles`), allowing users to select and change avatars when creating or editing their profiles.

## Changes Made

### ProfilesPage.razor Updates

#### UI Changes
1. **Avatar Carousel Component Added**
   - Location: After the Age Range dropdown in the Create/Edit Profile modal
   - Conditionally displayed only when an age group is selected
   - Uses same carousel component as CharacterAssignmentPage

2. **Age Group Dropdown Enhanced**
   - Added `@bind:after="OnAgeGroupChanged"` to trigger carousel reset when age changes
   - Maintains reactive behavior with avatar selection

#### Data Model Changes
- **ProfileFormData Class**: Added `SelectedAvatarMediaId` property
  ```csharp
  public class ProfileFormData
  {
      public string Name { get; set; } = string.Empty;
      public string? AgeGroup { get; set; }
      public string? SelectedAvatarMediaId { get; set; }  // NEW
  }
  ```

#### Event Handlers Added
1. **OnAgeGroupChanged()**
   - Resets `formData.SelectedAvatarMediaId` to null when age group changes
   - Triggers UI re-render to show new carousel for new age group
   - Ensures user must re-select avatar when changing age groups

2. **OnAvatarSelected(string mediaId)**
   - Receives selected media ID from AvatarCarousel component
   - Stores it in `formData.SelectedAvatarMediaId`
   - Logs the selection for debugging
   - Triggers UI re-render

### Workflow

#### Creating a New Profile
1. User clicks "New Profile" button
2. Modal opens with empty form
3. User enters profile name
4. User selects age group from dropdown
5. Avatar carousel appears for that age group
6. User browses and selects an avatar
7. User clicks "Create" button
8. Profile is created with selected avatar ID

#### Editing an Existing Profile
1. User clicks edit button on existing profile
2. Modal opens with current profile data:
   - Profile name pre-filled
   - Age group pre-selected
3. Avatar carousel automatically appears for current age group
4. If user changes age group:
   - Avatar selection is cleared
   - New carousel appears for new age group
   - User can select new avatar
5. User clicks "Update" button
6. Profile is updated with new avatar selection

### Data Persistence

The selected avatar media ID (`SelectedAvatarMediaId`) is now:
- Captured during profile creation
- Captured during profile editing
- Stored in the `PlayerAssignment.SelectedAvatarMediaId` field
- Can be used later for:
  - Profile display (showing user's selected avatar)
  - Character assignment defaults
  - Game session avatar representations

### Component Integration

The avatar carousel component is now used in two places:
1. **CharacterAssignmentPage.razor** - When assigning characters during game setup
2. **ProfilesPage.razor** - When managing user profiles (NEW)

Both implementations use the same component with identical parameters:
- `AgeGroup` (string): The selected age group
- `OnAvatarSelected` (EventCallback<string>): Called with selected media ID

### State Management

**Form State Lifecycle:**
1. Modal opens → `formData` initialized with current data (or empty for new)
2. Age group selected → Carousel displays with avatars for that age group
3. Avatar selected → `formData.SelectedAvatarMediaId` updated
4. Age group changed → `formData.SelectedAvatarMediaId` cleared, new carousel displayed
5. Form submitted → `formData` values used to create/update profile
6. Modal closes → `formData` reset to empty

### User Experience Enhancements

1. **Reactive Carousel**
   - Carousel automatically appears/updates when age group changes
   - No refresh needed
   - Smooth transition between age group avatars

2. **Clear Visual Feedback**
   - Carousel shows current selection with highlighted thumbnail
   - Counter displays current position in carousel
   - Previous/Next buttons provide clear navigation

3. **Accessibility**
   - All interactions have keyboard support
   - Loading states shown during avatar fetch
   - Error handling with user-friendly messages
   - Disabled states prevent invalid submissions

## Testing Checklist

- [ ] Create new profile with avatar selection
- [ ] Verify avatar carousel appears after selecting age group
- [ ] Verify carousel shows avatars for correct age group
- [ ] Verify avatar selection is stored when profile is created
- [ ] Edit existing profile
- [ ] Verify current age group pre-selected
- [ ] Verify avatar carousel appears for current age group
- [ ] Change age group in edit mode
- [ ] Verify carousel updates to new age group
- [ ] Verify avatar selection clears when changing age group
- [ ] Verify new avatar can be selected
- [ ] Verify updated profile saves with new avatar
- [ ] Test avatar carousel functionality on mobile (responsive)

## Technical Details

### Files Modified
- `src/Mystira.App.PWA/Pages/ProfilesPage.razor`

### Files Unchanged (but referenced)
- `src/Mystira.App.PWA/Components/AvatarCarousel.razor` - Used by ProfilesPage
- `src/Mystira.App.Domain/Models/AgeGroupConstants.cs` - Age group constants
- `src/Mystira.App.PWA/Services/ApiClient.cs` - Avatar API methods

### Component Dependencies
```
ProfilesPage.razor
├── AvatarCarousel.razor
│   ├── CachedMystiraImage.razor
│   └── ApiClient (GetAvatarsByAgeGroupAsync)
└── ApiClient (CreateProfileAsync, UpdateProfileAsync)
```

### API Endpoints Used
- `GET /api/avatars/{ageGroup}` - Fetch avatars for age group
- `GET /api/media/{id}` - Fetch avatar images
- `POST /api/userprofiles` - Create profile (unchanged)
- `PUT /api/userprofiles/{id}` - Update profile (unchanged)

## Future Enhancements

1. **Avatar Preview**
   - Display selected avatar next to profile name
   - Show avatar in profile cards in the profiles list

2. **Avatar History**
   - Remember user's avatar choices per age group
   - Quick re-select from recent avatars

3. **Avatar Customization**
   - Allow users to customize avatar colors
   - Add effects/filters to avatars
   - Create variations of avatars

4. **Avatar Gallery**
   - Dedicated avatar selection page
   - Advanced filtering/search
   - Carousel history tracking

## Documentation Links
- [Avatar Carousel Implementation](./AVATAR_CAROUSEL_IMPLEMENTATION.md)
- [CharacterAssignmentPage Integration](./src/Mystira.App.PWA/Pages/CharacterAssignmentPage.razor)

## Related Components
- `AvatarCarousel.razor` - Carousel component
- `CharacterAssignmentPage.razor` - Character assignment with avatars
- `ProfilesPage.razor` - Profile management with avatars (NEW)
