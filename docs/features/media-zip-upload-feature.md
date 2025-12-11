# Media Zip Upload Feature Documentation

## Overview
The Media Zip Upload feature allows administrators to upload media assets and their metadata in a single operation using a zip file. This feature implements a **metadata-first approach**, ensuring that metadata is validated and processed before any media files are uploaded.

## Features
- ✅ Metadata-first processing (prevents incomplete uploads)
- ✅ Single zip file containing all metadata and media
- ✅ Override options for both metadata and media files
- ✅ Comprehensive error reporting
- ✅ In-memory processing (no temporary files)
- ✅ Automatic media type detection
- ✅ File hash calculation for integrity
- ✅ Detailed logging for troubleshooting

## API Endpoint

### POST `/api/admin/mediaadmin/upload-zip`

Requires authentication (Admin)

#### Request Parameters
- **zipFile** (form file, required): The zip file containing media-metadata.json and media files
- **overwriteMetadata** (boolean, optional, default: false): Whether to overwrite existing metadata entries
- **overwriteMedia** (boolean, optional, default: false): Whether to overwrite existing media files

#### Response

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Successfully imported metadata and uploaded 5 media files",
  "metadataResult": {
    "success": true,
    "message": "Successfully imported 5 metadata entries",
    "importedCount": 5,
    "errors": [],
    "warnings": []
  },
  "uploadedMediaCount": 5,
  "failedMediaCount": 0,
  "successfulMediaUploads": ["media-id-1", "media-id-2", "media-id-3", "media-id-4", "media-id-5"],
  "mediaErrors": [],
  "allErrors": []
}
```

**Partial Success Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Metadata imported successfully. Uploaded 3 media files, 2 failed",
  "metadataResult": {
    "success": true,
    "message": "Successfully imported 5 metadata entries",
    "importedCount": 5,
    "errors": [],
    "warnings": []
  },
  "uploadedMediaCount": 3,
  "failedMediaCount": 2,
  "successfulMediaUploads": ["media-id-1", "media-id-2", "media-id-3"],
  "mediaErrors": [
    "No metadata entry found for file: unknown_file.mp3",
    "Failed to upload video_file.mp4: File too large"
  ],
  "allErrors": [
    "No metadata entry found for file: unknown_file.mp3",
    "Failed to upload video_file.mp4: File too large"
  ]
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Failed: metadata import error",
  "metadataResult": {
    "success": false,
    "message": "Failed to import metadata: Invalid JSON format",
    "importedCount": 0,
    "errors": ["Invalid JSON format"],
    "warnings": []
  },
  "uploadedMediaCount": 0,
  "failedMediaCount": 0,
  "successfulMediaUploads": [],
  "mediaErrors": [],
  "allErrors": ["Metadata import failed: Invalid JSON format"]
}
```

## Zip File Structure

### Required Structure
```
media-upload.zip
├── media-metadata.json          (REQUIRED)
├── image1.jpg                   (OPTIONAL - files referenced in metadata)
├── image2.png
├── audio1.mp3
├── video1.mp4
└── ... other media files
```

### media-metadata.json Format

The `media-metadata.json` file should contain an array of media metadata entries:

```json
[
  {
    "id": "media-image-001",
    "title": "Sample Image",
    "fileName": "image1.jpg",
    "type": "image",
    "description": "A sample image",
    "age_rating": 12,
    "subjectReferenceId": "subject-001",
    "classificationTags": [
      {
        "key": "category",
        "value": "nature"
      },
      {
        "key": "theme",
        "value": "animals"
      }
    ],
    "modifiers": [
      {
        "key": "filter",
        "value": "none"
      }
    ],
    "loopable": false
  },
  {
    "id": "media-audio-001",
    "title": "Sample Audio",
    "fileName": "audio1.mp3",
    "type": "audio",
    "description": "A sample audio file",
    "age_rating": 5,
    "subjectReferenceId": "subject-002",
    "classificationTags": [],
    "modifiers": [],
    "loopable": true
  }
]
```

## Upload Process Flow

```
1. User uploads zip file
   ↓
2. Validate zip file format and .zip extension
   ↓
3. Extract zip contents to memory
   ↓
4. Look for media-metadata.json
   ├─ Not found → Return error immediately
   └─ Found → Continue
   ↓
5. Parse and validate metadata JSON
   ├─ Invalid JSON → Return error with details
   └─ Valid → Continue
   ↓
6. Import metadata entries to database
   ├─ overwriteMetadata=false: Skip existing entries
   └─ overwriteMetadata=true: Replace existing entries
   ├─ Import fails → Return error, DO NOT upload media
   └─ Import succeeds → Continue to media upload
   ↓
7. For each media file in zip:
   ├─ Find matching metadata entry by filename
   ├─ Check if media already exists
   │  ├─ overwriteMedia=false: Skip existing media
   │  └─ overwriteMedia=true: Delete and replace
   ├─ Calculate file hash
   ├─ Upload to blob storage
   └─ Create media asset record in database
   ↓
8. Return comprehensive result with:
   - Metadata import success/failure
   - Count of successful and failed media uploads
   - Detailed error list
   - List of successfully uploaded media IDs
```

## Error Handling

The feature provides detailed error information for various failure scenarios:

### Metadata-Related Errors
- Missing `media-metadata.json` file
- Invalid JSON format in metadata file
- Duplicate media IDs in metadata

### Media Upload Errors
- No metadata entry found for a file
- Media with same ID already exists (when overwrite is disabled)
- File too large or format not supported
- Blob storage upload failure

### Response Behavior
- **Metadata import failure**: Returns immediately without uploading any media files
- **Media upload partial failure**: Uploads successful files, returns error details for failed ones
- **All media files fail**: Returns 400 Bad Request with details

## Override Options

### overwriteMetadata=false (Default)
- Existing metadata entries are preserved
- New metadata entries are added
- Duplicate IDs are skipped with a warning

### overwriteMetadata=true
- Existing metadata entries are replaced with new values
- New metadata entries are added
- All metadata from the JSON file is applied

### overwriteMedia=false (Default)
- Existing media files are preserved
- New media files are uploaded
- Duplicate media IDs are skipped

### overwriteMedia=true
- Existing media files are deleted and replaced with new versions
- New media files are uploaded
- All media files from the zip are applied (if metadata exists)

## Usage Examples

### cURL Example: Basic Upload
```bash
curl -X POST \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "zipFile=@media-upload.zip" \
  http://localhost:7001/api/admin/mediaadmin/upload-zip
```

### cURL Example: With Override Options
```bash
curl -X POST \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "zipFile=@media-upload.zip" \
  -F "overwriteMetadata=true" \
  -F "overwriteMedia=true" \
  http://localhost:7001/api/admin/mediaadmin/upload-zip
```

### C# HttpClient Example
```csharp
using (var client = new HttpClient())
{
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    using (var form = new MultipartFormDataContent())
    {
        using (var fileStream = System.IO.File.OpenRead("media-upload.zip"))
        {
            form.Add(new StreamContent(fileStream), "zipFile", "media-upload.zip");
            form.Add(new StringContent("true"), "overwriteMetadata");
            form.Add(new StringContent("false"), "overwriteMedia");
            
            var response = await client.PostAsync(
                "http://localhost:7001/api/admin/mediaadmin/upload-zip", 
                form);
            
            var json = await response.Content.ReadAsStringAsync();
        }
    }
}
```

## Implementation Details

### Files Modified
1. **src/Mystira.App.Admin.Api/Models/MediaModels.cs**
   - Added `MetadataImportResult` class
   - Added `ZipUploadResult` class

2. **src/Mystira.App.Admin.Api/Services/IMediaApiService.cs**
   - Added `UploadMediaFromZipAsync` method signature

3. **src/Mystira.App.Admin.Api/Services/MediaApiService.cs**
   - Implemented `UploadMediaFromZipAsync` method
   - Added `CalculateHashFromBytes` helper method
   - Added `System.IO.Compression` using statement

4. **src/Mystira.App.Admin.Api/Controllers/MediaAdminController.cs**
   - Added `UploadMediaZip` endpoint

### Key Technical Decisions
- **In-Memory Processing**: All zip extraction and processing happens in memory to avoid disk I/O
- **Metadata-First Validation**: Prevents partial failures by validating metadata before any media upload
- **Comprehensive Logging**: All operations are logged for debugging and audit purposes
- **Hash Calculation**: File hashes are calculated from raw bytes using SHA256 for integrity verification
- **Automatic MIME Type Detection**: Media files are checked against a known MIME type map

## Troubleshooting

### "No media-metadata.json file found in the zip"
- Ensure the zip file contains a file named exactly `media-metadata.json` at the root level
- Check that the filename casing is correct

### "Invalid JSON format"
- Validate your JSON syntax using a JSON validator
- Ensure all required fields are present in metadata entries
- Check for proper string escaping in descriptions

### "No metadata entry found for file: filename.mp3"
- Verify that `media-metadata.json` contains an entry with a `fileName` field matching the file
- Ensure the filename casing matches exactly

### "Metadata imported successfully but media uploads failed"
- Check that media files referenced in metadata are actually included in the zip
- Verify file permissions in blob storage
- Check blob storage connection settings in appsettings.json

### Media files uploaded but metadata didn't persist
- This should not happen due to metadata-first processing
- If it does, check database transaction logs
- Verify database connection and permissions

## Security Considerations

- ✅ Authentication required (Admin role)
- ✅ File type validation (must be .zip)
- ✅ File size limits should be configured in ASP.NET Core
- ✅ Input validation for JSON structure
- ✅ Media ID validation against whitelist patterns (if implemented)
- ✅ Comprehensive logging for audit trails

## Performance Notes

- Large zip files (>500MB) may require increased timeout settings
- Metadata import is faster than individual file uploads
- Override options should be used carefully to avoid performance impact
- Consider the number of media files when estimating upload time

## Future Enhancements

- Parallel media file uploads within a single zip
- Streaming zip processing for very large files
- Progress reporting during upload
- Batch verification before starting upload
- Rollback capability on partial failures
