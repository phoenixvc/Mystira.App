# Account Use Cases

This directory contains documentation for account-related operations.

## Overview

Accounts represent user authentication and subscription management. Account operations are primarily handled through services rather than dedicated use cases.

## Use Cases

Account management operations are currently handled through services:

- Account creation and authentication (via Auth0 integration)
- Subscription management (via services)
- Account settings management (via services)

## Related Components

- **Domain Models**: `Account`, `SubscriptionDetails`, `AccountSettings`
- **Services**: `AccountApiService`
- **DTOs**: Account-related request/response DTOs
