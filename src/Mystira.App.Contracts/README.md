# Mystira.App.Contracts

This project contains Data Transfer Objects (DTOs) and API contracts used for communication between layers.

## Structure

- **Requests/**: Request DTOs for API endpoints
- **Responses/**: Response DTOs for API endpoints  
- **DTOs/**: General data transfer objects

## Purpose

This project decouples API contracts from domain models, allowing:
- Independent versioning of API contracts
- Clear separation between domain and presentation layers
- Easier API evolution without affecting domain models

## Usage

DTOs should be used for:
- API request/response models
- Data transfer between layers
- Serialization/deserialization

Domain models should be used for:
- Business logic
- Domain validation
- Core entities

