# Media Zip Upload Frontend UI Changes

## Overview
Added a comprehensive user interface for uploading media via zip files to the Mystira.App.Admin.Api media import view. The new UI provides a streamlined experience for uploading media metadata and files together.

## Files Modified
- `src/Mystira.App.Admin.Api/Views/Admin/ImportMedia.cshtml`

## UI Changes

### New Zip Upload Card
Added a new upload card titled "Step 2: Zip Upload (Recommended)" with the following features:

#### Form Elements
1. **Zip File Input**
   - Accepts only `.zip` files
   - Required field with clear file format requirement
   - Helper text explaining the expected zip structure:
     - `media-metadata.json` - Contains metadata entries (required)
     - Media files - Audio, video, or image files referenced in metadata

2. **Override Checkboxes**
   - `Overwrite existing metadata entries` - Allows replacement of existing metadata
   - `Overwrite existing media files` - Allows replacement of existing media assets
   - Both disabled by default for safety

3. **Upload Button**
   - Styled with Bootstrap info color (blue)
   - Displays icon with upload text
   - Disabled until metadata is available (same as other upload methods)
   - Only enabled after successful metadata import

#### Visual Design
- **Header**: Info-colored header (blue background with white text) to distinguish from other upload methods
- **Alert**: Info alert explaining this is the recommended method
- **Icon**: File archive icon (fas fa-file-archive) to represent zip files
- **Position**: Placed as an alternative option alongside bulk upload

### JavaScript Functions Added

#### `uploadZipFile()`
Handles the zip file upload process:
- Validates file selection
- Checks that file is a valid zip file
- Shows progress indicator with appropriate messaging
- Sends FormData with:
  - `zipFile`: The zip file
  - `overwriteMetadata`: Boolean flag
  - `overwriteMedia`: Boolean flag
- Handles three response scenarios:
  - Complete success: Shows success message and results
  - Partial success: Shows warning with metadata OK but some media files failed
  - Failure: Shows error message with details
- Refreshes metadata status after successful upload
- Resets form on completion

#### `showZipResults(data)`
Displays comprehensive upload results with three sections:

**1. Zip Upload Summary Header**
- Main title for the results section

**2. Metadata Import Card**
- Shows success or failure status with colored header
- Displays metadata import message
- Shows any errors encountered during import
- Color-coded: green for success, red for failure

**3. Media Upload Card**
- Shows count of successful and failed uploads
- Lists all successfully uploaded media IDs with ✅ checkmarks
- Lists all failed uploads with ❌ indicators and error messages
- Provides clear visual distinction between successes and failures

### Updated Functions

#### `updateMetadataStatus()`
Enhanced to support zip upload card:
- Shows/hides zip upload card based on metadata availability
- Enables/disables zip upload button based on metadata status
- Same visibility logic as single and bulk upload options

#### Event Listeners
- Added submit listener to `zipUploadForm` element
- Calls `uploadZipFile()` on form submission

## API Integration

### Endpoint
The UI communicates with: `POST /api/admin/MediaAdmin/upload-zip`

### Request Format
```
FormData:
- zipFile: File (required) - The zip archive
- overwriteMetadata: boolean (default: false)
- overwriteMedia: boolean (default: false)
```

### Response Format
The UI expects the following response structure:
```json
{
  "success": boolean,
  "message": string,
  "metadataResult": {
    "success": boolean,
    "message": string,
    "importedCount": number,
    "errors": string[],
    "warnings": string[]
  },
  "uploadedMediaCount": number,
  "failedMediaCount": number,
  "successfulMediaUploads": string[],
  "mediaErrors": string[],
  "allErrors": string[]
}
```

## User Experience Flow

1. **Metadata Upload** (Step 1)
   - User uploads initial media metadata JSON/YAML file
   - System validates and imports metadata
   - All upload options become available

2. **Choose Upload Method**
   - User selects one of three options:
     - Single File Upload
     - Bulk Upload
     - Zip Upload (Recommended) ← NEW

3. **Zip Upload Process**
   - User selects zip file containing `media-metadata.json` and media files
   - User optionally enables overwrite flags
   - Clicks "Upload Zip" button
   - Progress indicator shows during processing
   - Results displayed with:
     - Metadata import status
     - List of successful uploads
     - List of failed uploads with error details

## Styling and Layout

### CSS Classes Used
- `card` - Container cards
- `card-header` - Card titles
- `bg-info` / `text-white` - Zip card header styling
- `alert` - Information/warning messages
- `form-group` - Form field containers
- `form-check` - Checkbox styling
- `btn btn-info` - Upload button styling
- `badge` - Status indicators (success/failed counts)
- `list-group` - Result list styling
- `text-success` / `text-danger` - Color-coded text

### Responsive Design
- Uses Bootstrap grid system
- Works on all screen sizes
- Sidebar layout maintained
- Card-based layout for organized content

## Features

✅ **Metadata-First Validation**: Metadata is imported first before any media files
✅ **Override Options**: Choose to replace existing metadata and/or media
✅ **Comprehensive Feedback**: Detailed results showing successes and failures
✅ **Progress Indication**: User-friendly progress messaging
✅ **Error Handling**: Clear error messages for troubleshooting
✅ **File Validation**: Ensures only zip files are accepted
✅ **Status Refresh**: Automatically refreshes metadata status after upload
✅ **Form Reset**: Clears form after successful upload
✅ **Partial Success Support**: Handles cases where metadata succeeds but some media fails

## Testing Recommendations

1. **File Validation**
   - Test with non-zip files (should show error)
   - Test with empty zip file (should show error)
   - Test with zip missing media-metadata.json (should show error)

2. **Metadata Import**
   - Test with valid metadata in zip
   - Test with invalid JSON in metadata file
   - Test with overwrite metadata checkbox enabled/disabled

3. **Media Upload**
   - Test with media files present
   - Test with missing media files referenced in metadata
   - Test with overwrite media checkbox enabled/disabled

4. **Result Display**
   - Verify success results display correctly
   - Verify partial success (metadata OK, some files fail)
   - Verify failure results display errors clearly

## Browser Compatibility
- Chrome/Edge 90+
- Firefox 88+
- Safari 14+
- Mobile browsers (iOS Safari, Chrome Mobile)

## Performance Considerations
- Large zip files may take time to process
- Progress indicator provides user feedback
- File size limits configured on backend
- Timeout settings may need adjustment for very large files
