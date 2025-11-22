# API Endpoint Classification Guide

## Decision Rule

**Question**: "Can this action damage someone else's data or the whole system?"

- **Yes** → `/adminapi` (Admin API)
- **No, it's scoped to the caller** → `/api` (Public API)

## Classification Principles

### `/api` (Public API - Port 7000)
**Purpose**: User-facing operations where users act on their own resources

**Characteristics**:
- User acts on their own data (profiles, sessions, badges)
- Read-only access to shared content (scenarios, media, characters)
- User authentication and account management
- Game session operations for the authenticated user
- Safe operations that don't affect other users or system integrity

### `/adminapi` (Admin API - Port 7001)
**Purpose**: Administrative operations that can affect system-wide data or other users

**Characteristics**:
- Content creation and management (scenarios, media, characters)
- System configuration (badge configurations, avatar configurations)
- User management (viewing/managing other users' data)
- Bulk operations and data imports
- Dangerous operations that could damage system integrity
- Content moderation and administrative oversight

## Endpoint Classification

### Public API (`/api`) Endpoints

#### Authentication (`/api/auth`)
- ✅ `POST /api/auth/passwordless/signup` - User signs up
- ✅ `POST /api/auth/passwordless/verify` - User verifies their signup
- ✅ `POST /api/auth/passwordless/signin` - User signs in
- ✅ `POST /api/auth/passwordless/signin/verify` - User verifies signin
- ✅ `POST /api/auth/refresh` - User refreshes their token

#### Accounts (`/api/accounts`)
- ✅ `GET /api/accounts/email/{email}` - Get own account by email
- ✅ `GET /api/accounts/{accountId}` - Get own account
- ✅ `PUT /api/accounts/{accountId}` - Update own account
- ✅ `DELETE /api/accounts/{accountId}` - Delete own account
- ✅ `POST /api/accounts/{accountId}/profiles` - Add profile to own account
- ✅ `GET /api/accounts/{accountId}/profiles` - Get profiles for own account
- ✅ `GET /api/accounts/validate/{email}` - Validate own email

#### User Profiles (`/api/userprofiles`)
- ✅ `POST /api/userprofiles` - Create own profile
- ✅ `GET /api/userprofiles/{id}` - Get own profile
- ✅ `PUT /api/userprofiles/{id}` - Update own profile
- ✅ `DELETE /api/userprofiles/{id}` - Delete own profile
- ✅ `POST /api/userprofiles/{id}/complete-onboarding` - Complete own onboarding
- ✅ `POST /api/userprofiles/batch` - Create multiple own profiles
- ✅ `POST /api/userprofiles/{profileId}/assign-character` - Assign character to own profile
- ✅ `DELETE /api/userprofiles/{profileId}/account` - Remove profile from own account
- ✅ `GET /api/userprofiles/account/{accountId}` - Get profiles for own account

#### User Badges (`/api/userbadges`)
- ✅ `POST /api/userbadges/award` - Award badge to own profile
- ✅ `GET /api/userbadges/user/{userProfileId}` - Get badges for own profile
- ✅ `GET /api/userbadges/user/{userProfileId}/axis/{axis}` - Get badges by axis for own profile
- ✅ `GET /api/userbadges/user/{userProfileId}/badge/{badgeConfigurationId}/earned` - Check if own profile earned badge
- ✅ `GET /api/userbadges/user/{userProfileId}/statistics` - Get badge statistics for own profile
- ✅ `GET /api/userbadges/account/{email}` - Get badges for own account
- ✅ `GET /api/userbadges/account/{email}/statistics` - Get badge statistics for own account

#### Game Sessions (`/api/gamesessions`)
- ✅ `POST /api/gamesessions` - Create own game session
- ✅ `GET /api/gamesessions/{id}` - Get own game session
- ✅ `GET /api/gamesessions/account/{accountId}` - Get own account's sessions
- ✅ `GET /api/gamesessions/profile/{profileId}` - Get own profile's sessions
- ✅ `GET /api/gamesessions/account/{accountId}/in-progress` - Get own in-progress sessions
- ✅ `POST /api/gamesessions/choice` - Make choice in own session
- ✅ `POST /api/gamesessions/{id}/progress-scene` - Progress own session
- ✅ `POST /api/gamesessions/{id}/end` - End own session
- ✅ `GET /api/gamesessions/{id}/stats` - Get stats for own session
- ✅ `GET /api/gamesessions/{id}/achievements` - Get achievements for own session
- ✅ `POST /api/gamesessions/complete-scenario` - Complete scenario in own session

#### Scenarios (`/api/scenarios`)
- ✅ `GET /api/scenarios` - List scenarios (read-only)
- ✅ `GET /api/scenarios/{id}` - Get scenario (read-only)
- ✅ `GET /api/scenarios/age-group/{ageGroup}` - Get scenarios by age group (read-only)
- ✅ `GET /api/scenarios/featured` - Get featured scenarios (read-only)
- ✅ `GET /api/scenarios/with-game-state/{accountId}` - Get scenarios with own game state

#### Media (`/api/media`)
- ✅ `GET /api/media` - List media (read-only)
- ✅ `GET /api/media/{mediaId}/info` - Get media info (read-only)
- ✅ `GET /api/media/{mediaId}` - Get media (read-only)

#### Characters (`/api/characters`)
- ✅ `GET /api/characters/{id}` - Get character (read-only)

#### Character Maps (`/api/charactermaps`)
- ✅ `GET /api/charactermaps` - List character maps (read-only)
- ✅ `GET /api/charactermaps/{id}` - Get character map (read-only)

#### Badge Configurations (`/api/badgeconfigurations`)
- ✅ `GET /api/badgeconfigurations` - List badge configurations (read-only)
- ✅ `GET /api/badgeconfigurations/{id}` - Get badge configuration (read-only)
- ✅ `GET /api/badgeconfigurations/axis/{axis}` - Get badge configurations by axis (read-only)

#### Avatars (`/api/avatars`)
- ✅ `GET /api/avatars` - Get avatar configurations (read-only)
- ✅ `GET /api/avatars/{ageGroup}` - Get avatars by age group (read-only)

#### Content Bundles (`/api/bundles`)
- ✅ `GET /api/bundles` - List content bundles (read-only)
- ✅ `GET /api/bundles/age-group/{ageGroup}` - Get bundles by age group (read-only)

#### Health (`/api/health`)
- ✅ `GET /api/health/ready` - Health check
- ✅ `GET /api/health/live` - Liveness check

### Admin API (`/adminapi`) Endpoints

#### Admin Dashboard (`/admin`)
- 🔒 `GET /admin` - Admin dashboard
- 🔒 `GET /admin/login` - Admin login page
- 🔒 `GET /admin/scenarios` - Scenarios management UI
- 🔒 `GET /admin/media` - Media management UI
- 🔒 `GET /admin/media-metadata` - Media metadata management UI
- 🔒 `GET /admin/character-media-metadata` - Character media metadata UI
- 🔒 `GET /admin/bundles` - Bundles management UI
- 🔒 `GET /admin/avatars` - Avatars management UI
- 🔒 `GET /admin/charactermaps` - Character maps management UI
- 🔒 `GET /admin/status` - System status UI
- 🔒 `POST /admin/status` - Update system status
- 🔒 `POST /admin/scenarios/upload` - Upload scenarios (bulk operation)
- 🔒 `POST /admin/bundles/upload` - Upload bundles (bulk operation)
- 🔒 `POST /admin/bundles/validate` - Validate bundles
- 🔒 `POST /admin/charactermaps/import` - Import character maps (bulk operation)
- 🔒 `POST /admin/initialize-sample-data` - Initialize sample data (system-wide)
- 🔒 `POST /admin/fix-metadata-format` - Fix metadata format (system-wide)

#### Authentication (`/api/auth`)
- 🔒 `POST /api/auth/login` - Admin login
- 🔒 `POST /api/auth/logout` - Admin logout

#### Scenarios (`/api/admin/scenariosadmin`)
- 🔒 `POST /api/admin/scenariosadmin` - Create scenario (affects all users)
- 🔒 `PUT /api/admin/scenariosadmin/{id}` - Update scenario (affects all users)
- 🔒 `DELETE /api/admin/scenariosadmin/{id}` - Delete scenario (affects all users)
- 🔒 `POST /api/admin/scenariosadmin/validate` - Validate scenario
- 🔒 `GET /api/admin/scenariosadmin/{id}` - Get scenario (admin view)
- 🔒 `GET /api/admin/scenariosadmin/{id}/validate-references` - Validate scenario references
- 🔒 `GET /api/admin/scenariosadmin/validate-all-references` - Validate all scenarios (system-wide)

#### Scenarios Read (`/api/scenarios`)
- 🔒 `GET /api/scenarios/{id}` - Get scenario (admin view)
- 🔒 `GET /api/scenarios/age-group/{ageGroup}` - Get scenarios by age group (admin view)
- 🔒 `GET /api/scenarios/featured` - Get featured scenarios (admin view)

#### Media (`/api/admin/mediaadmin`)
- 🔒 `GET /api/admin/mediaadmin/{mediaId}` - Get media (admin view)
- 🔒 `POST /api/admin/mediaadmin/upload` - Upload media (affects all users)
- 🔒 `POST /api/admin/mediaadmin/bulk-upload` - Bulk upload media (affects all users)
- 🔒 `POST /api/admin/mediaadmin/upload-zip` - Upload media ZIP (bulk operation)
- 🔒 `PUT /api/admin/mediaadmin/{mediaId}` - Update media (affects all users)
- 🔒 `DELETE /api/admin/mediaadmin/{mediaId}` - Delete media (affects all users)
- 🔒 `POST /api/admin/mediaadmin/validate` - Validate media references
- 🔒 `GET /api/admin/mediaadmin/statistics` - Get media statistics (system-wide)

#### Media Metadata (`/api/admin/mediametadataadmin`)
- 🔒 `GET /api/admin/mediametadataadmin/entries/{entryId}` - Get metadata entry
- 🔒 `POST /api/admin/mediametadataadmin/entries` - Create metadata entry (affects all users)
- 🔒 `PUT /api/admin/mediametadataadmin/entries/{entryId}` - Update metadata entry (affects all users)
- 🔒 `DELETE /api/admin/mediametadataadmin/entries/{entryId}` - Delete metadata entry (affects all users)
- 🔒 `POST /api/admin/mediametadataadmin/import` - Import metadata (bulk operation)

#### Character Media Metadata (`/api/admin/charactermediametadataadmin`)
- 🔒 `GET /api/admin/charactermediametadataadmin/entries/{entryId}` - Get character metadata entry
- 🔒 `POST /api/admin/charactermediametadataadmin/entries` - Create character metadata entry (affects all users)
- 🔒 `PUT /api/admin/charactermediametadataadmin/entries/{entryId}` - Update character metadata entry (affects all users)
- 🔒 `DELETE /api/admin/charactermediametadataadmin/entries/{entryId}` - Delete character metadata entry (affects all users)
- 🔒 `POST /api/admin/charactermediametadataadmin/import` - Import character metadata (bulk operation)

#### Characters (`/api/admin/characteradmin`)
- 🔒 `PUT /api/admin/characteradmin/{id}` - Update character (affects all users)
- 🔒 `DELETE /api/admin/characteradmin/{id}` - Delete character (affects all users)

#### Character Maps (`/api/admin/charactermapsadmin`)
- 🔒 `GET /api/admin/charactermapsadmin/{id}` - Get character map (admin view)
- 🔒 `POST /api/admin/charactermapsadmin` - Create character map (affects all users)
- 🔒 `PUT /api/admin/charactermapsadmin/{id}` - Update character map (affects all users)
- 🔒 `DELETE /api/admin/charactermapsadmin/{id}` - Delete character map (affects all users)
- 🔒 `GET /api/admin/charactermapsadmin/export` - Export character map
- 🔒 `POST /api/admin/charactermapsadmin/import` - Import character map (bulk operation)

#### Badge Configurations (`/api/badgeconfigurationsadmin`)
- 🔒 `GET /api/badgeconfigurationsadmin/{id}` - Get badge configuration (admin view)
- 🔒 `GET /api/badgeconfigurationsadmin/axis/{axis}` - Get badge configurations by axis (admin view)
- 🔒 `POST /api/badgeconfigurationsadmin` - Create badge configuration (affects all users)
- 🔒 `PUT /api/badgeconfigurationsadmin/{id}` - Update badge configuration (affects all users)
- 🔒 `DELETE /api/badgeconfigurationsadmin/{id}` - Delete badge configuration (affects all users)
- 🔒 `GET /api/badgeconfigurationsadmin/export` - Export badge configurations
- 🔒 `POST /api/badgeconfigurationsadmin/import` - Import badge configurations (bulk operation)

#### Avatars (`/api/admin/avataradmin`)
- 🔒 `GET /api/admin/avataradmin/{ageGroup}` - Get avatars by age group (admin view)
- 🔒 `POST /api/admin/avataradmin/{ageGroup}` - Update avatar configuration (affects all users)
- 🔒 `POST /api/admin/avataradmin/{ageGroup}/add` - Add avatar to age group (affects all users)
- 🔒 `DELETE /api/admin/avataradmin/{ageGroup}/remove/{mediaId}` - Remove avatar from age group (affects all users)

#### Content Bundles (`/api/admin/bundlesadmin`)
- 🔒 `GET /api/admin/bundlesadmin/{id}` - Get content bundle (admin view)
- 🔒 `POST /api/admin/bundlesadmin` - Create content bundle (affects all users)
- 🔒 `PUT /api/admin/bundlesadmin/{id}` - Update content bundle (affects all users)

#### User Profiles (`/api/userprofilesadmin`)
- 🔒 `GET /api/userprofilesadmin/account/{accountId}` - Get profiles for any account (user management)
- 🔒 `GET /api/userprofilesadmin/{id}` - Get any profile (user management)
- 🔒 `PUT /api/userprofilesadmin/{name}` - Update any profile (user management)
- 🔒 `PUT /api/userprofilesadmin/id/{profileId}` - Update any profile by ID (user management)
- 🔒 `DELETE /api/userprofilesadmin/{name}` - Delete any profile (user management)
- 🔒 `GET /api/userprofilesadmin/non-guest` - Get all non-guest profiles (user management)
- 🔒 `GET /api/userprofilesadmin/guest` - Get all guest profiles (user management)

#### Game Sessions (`/api/gamesessionsadmin`)
- 🔒 `GET /api/gamesessionsadmin/{id}` - Get any game session (admin view)
- 🔒 `GET /api/gamesessionsadmin/account/{accountId}` - Get sessions for any account (user management)
- 🔒 `GET /api/gamesessionsadmin/profile/{profileId}` - Get sessions for any profile (user management)
- 🔒 `GET /api/gamesessionsadmin/account/email/{email}` - Get sessions by email (user management)
- 🔒 `GET /api/gamesessionsadmin/account/{email}/history` - Get session history by email (user management)
- 🔒 `POST /api/gamesessionsadmin/choice` - Make choice in any session (admin override)
- 🔒 `POST /api/gamesessionsadmin/{id}/pause` - Pause any session (admin override)
- 🔒 `POST /api/gamesessionsadmin/{id}/resume` - Resume any session (admin override)
- 🔒 `POST /api/gamesessionsadmin/{id}/end` - End any session (admin override)
- 🔒 `POST /api/gamesessionsadmin/{id}/progress-scene` - Progress any session (admin override)
- 🔒 `POST /api/gamesessionsadmin/{id}/select-character` - Select character in any session (admin override)
- 🔒 `GET /api/gamesessionsadmin/{id}/stats` - Get stats for any session (admin view)
- 🔒 `GET /api/gamesessionsadmin/{id}/achievements` - Get achievements for any session (admin view)

## Migration Checklist

### Endpoints That Should Move to `/adminapi`

**None identified** - Current classification appears correct based on the decision rule.

### Endpoints That Should Stay in `/api`

All current `/api` endpoints are correctly classified as user-scoped operations.

## Notes

- 🔒 = Requires admin authentication
- ✅ = User-scoped operation (correctly in `/api`)
- All `/adminapi` endpoints require admin authentication
- All `/api` endpoints that modify data require user authentication and operate on the authenticated user's own resources
- Read-only endpoints in `/api` are safe for public access (scenarios, media, characters, etc.)
