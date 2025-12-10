# ADR-0010: Story Protocol SDK Integration Strategy

**Status**: 💭 Proposed

**Date**: 2025-12-10

**Deciders**: Development Team

**Tags**: architecture, blockchain, story-protocol, sdk-integration, infrastructure

**Supersedes**: None (new capability)

---

## Approvals

| Role | Name | Date | Status |
|------|------|------|--------|
| Tech Lead | | | ⏳ Pending |
| Backend Dev | | | ⏳ Pending |
| DevOps | | | ⏳ Pending |

---

## Context

Mystira.App requires blockchain integration with Story Protocol to enable:
- Registration of stories (scenarios) as IP Assets on-chain
- Automatic royalty distribution to contributors (publishers, curators)
- Transparent attribution and ownership tracking

### Current State

The codebase already has a comprehensive Story Protocol integration:
- **Port Interface**: `IStoryProtocolService` defined in Application layer (7 operations)
- **Domain Models**: `StoryProtocolMetadata`, `Contributor`, `RoyaltyPaymentResult`, `RoyaltyBalance`
- **Mock Implementation**: Fully functional for development/testing
- **Production Implementation**: Using Nethereum for direct Ethereum smart contract calls

### Problems Identified

Based on team discussion and technical evaluation:

1. **SDK Availability Mismatch**
   - Story Protocol provides official SDKs for **TypeScript** and **Python** only
   - **No official .NET SDK** exists
   - Our codebase is primarily **.NET 9.0 / C#**

2. **REST API Limitations**
   - Story Protocol's REST API is **read-only**
   - All write operations (register IP, pay royalties, claim) require SDK or direct contract calls
   - Current Nethereum implementation may not support all Story Protocol-specific features

3. **SDK vs Direct Contract Calls**
   - Direct Nethereum calls require maintaining ABI compatibility as contracts evolve
   - Official SDKs abstract contract complexities and handle protocol upgrades
   - SDK provides higher-level operations (e.g., `mintAndRegisterIpAssetWithPilTerms`)

4. **Processing Architecture Gap**
   - No event-driven system currently exists for async blockchain operations
   - Blockchain transactions are slow (seconds to minutes for confirmation)
   - Need a way to decouple API requests from blockchain write operations

5. **Time Constraints**
   - MVP deadline requires minimal, high-leverage implementation
   - Must balance long-term architecture with immediate delivery needs

### Royalty Distribution Requirements

From business requirements:
- Story publisher receives configurable percentage (default: 10%)
- Story curator receives configurable percentage (default: 10%)
- Percentages adjustable via admin portal
- Contributors identified by wallet address (Ethereum format)

---

## Decision Drivers

1. **Maintainability**: Minimize custom blockchain code that requires ongoing maintenance
2. **Reliability**: Use official SDKs where possible for protocol compatibility
3. **Hexagonal Compliance**: Maintain clean architecture principles (port/adapter pattern)
4. **MVP Focus**: Deliver working solution within time constraints
5. **Future-Proofing**: Architecture should scale without major rewrites

---

## Considered Options

### Option 1: Continue with Nethereum Direct Calls (Current)

**Description**: Keep the existing `StoryProtocolService` using Nethereum for direct smart contract interactions.

**Pros**:
- ✅ Already implemented and partially working
- ✅ No additional infrastructure required
- ✅ Pure .NET solution, consistent tech stack
- ✅ Full control over contract interactions

**Cons**:
- ❌ Must maintain ABI compatibility manually as Story Protocol evolves
- ❌ Missing SDK-specific helper functions (e.g., combined mint+register operations)
- ❌ Higher risk of subtle bugs in contract encoding/decoding
- ❌ No access to SDK's built-in retry logic and error handling

### Option 2: Python Sidecar Microservice ⭐ **RECOMMENDED**

**Description**: Create a lightweight Python microservice (`Mystira.Chain`) that wraps Story Protocol's official Python SDK. The .NET API communicates with this service via HTTP/REST.

```
┌─────────────────────┐       ┌──────────────────────┐       ┌─────────────────┐
│  Mystira.App.Api    │──────▶│ Mystira.Chain│──────▶│  Story Protocol │
│  (.NET)             │  HTTP │ (Python/FastAPI)     │  SDK  │  Blockchain     │
└─────────────────────┘       └──────────────────────┘       └─────────────────┘
```

**Pros**:
- ✅ Uses official Story Protocol Python SDK
- ✅ SDK handles protocol upgrades and ABI changes
- ✅ Clean separation of concerns (blockchain logic isolated)
- ✅ Can be deployed independently (Azure Container Apps, App Service)
- ✅ **Team member more familiar with Python than TypeScript**
- ✅ Easier debugging with SDK's built-in logging
- ✅ FastAPI provides automatic OpenAPI docs and validation
- ✅ Python has excellent blockchain/web3 ecosystem (web3.py)

**Cons**:
- ⚠️ Additional service to deploy and maintain
- ⚠️ Network latency between services (minimal for same-region)
- ⚠️ New repository/project to manage
- ⚠️ Requires Python runtime in infrastructure

### Option 3: Azure Function Bridge (TypeScript)

**Description**: Create Azure Functions (TypeScript) for each blockchain operation. .NET API invokes functions via HTTP triggers.

**Pros**:
- ✅ Uses official TypeScript SDK
- ✅ Serverless - scales automatically, pay-per-use
- ✅ No infrastructure to manage
- ✅ Quick to implement for MVP

**Cons**:
- ❌ Cold start latency for blockchain operations
- ❌ Harder to debug function-to-function flows
- ❌ Complex retry/timeout handling across function boundaries
- ❌ Limited execution time (10 min max on Consumption plan)
- ❌ State management complexity for long-running transactions

### Option 4: Background Worker with DB Polling

**Description**: Create a standalone TypeScript background worker that polls Cosmos DB for pending blockchain operations and processes them.

**Pros**:
- ✅ Fully decouples API from blockchain timing
- ✅ Natural queue-like behavior
- ✅ Resilient to API restarts

**Cons**:
- ❌ Polling introduces latency (vs event-driven)
- ❌ Complex state machine for operation tracking
- ❌ Requires careful handling of concurrent processing
- ❌ More operational overhead

### Option 5: Hybrid - Nethereum + SDK Sidecar for Complex Operations

**Description**: Keep Nethereum for simple read operations, add TypeScript sidecar only for complex write operations.

**Pros**:
- ✅ Minimal new code for reads
- ✅ SDK benefits for complex writes

**Cons**:
- ❌ Split logic across two implementations
- ❌ Inconsistent patterns
- ❌ Harder to maintain

---

## Decision

We will adopt **Option 2: Python Sidecar Microservice** with the following implementation plan:

### Architecture

```
┌────────────────────────────────────────────────────────────────────────────┐
│                           Mystira.App (.NET)                               │
│  ┌─────────────────┐    ┌────────────────────────────────────────────┐    │
│  │  Admin.API      │    │  Application Layer                         │    │
│  │  Controller     │───▶│  IStoryProtocolService (Port)              │    │
│  └─────────────────┘    └────────────────────────────────────────────┘    │
│                                        │                                   │
│  ┌─────────────────────────────────────┼───────────────────────────────┐  │
│  │                     Infrastructure  │                                │  │
│  │  ┌─────────────────────────────────▼──────────────────────────────┐ │  │
│  │  │  ChainServiceAdapter : IStoryProtocolService                    │ │  │
│  │  │  (Calls Mystira.Chain via HTTP)                         │ │  │
│  │  └────────────────────────────────────────────────────────────────┘ │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
                                        │
                                   HTTP │ REST
                                        ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                    Mystira.Chain (Python)                          │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │  FastAPI                                                             │  │
│  │  ├── POST /ip-assets/register                                        │  │
│  │  ├── GET  /ip-assets/{id}/status                                     │  │
│  │  ├── POST /royalties/pay                                             │  │
│  │  ├── GET  /royalties/{id}/claimable                                  │  │
│  │  └── POST /royalties/{id}/claim                                      │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                        │                                   │
│  ┌─────────────────────────────────────▼──────────────────────────────┐   │
│  │  Story Protocol Python SDK                                          │   │
│  │  story-protocol-python-sdk                                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
                          ┌─────────────────────────┐
                          │  Story Protocol         │
                          │  Blockchain (Testnet)   │
                          └─────────────────────────┘
```

### Implementation Components

#### 1. New Repository: `Mystira.Chain`

**GitHub Repository Settings:**
| Field | Value |
|-------|-------|
| **Name** | `Mystira.Chain` |
| **Description** | Blockchain integration service for Story Protocol IP registration and royalties |
| **Topics/Labels** | `python`, `fastapi`, `blockchain`, `story-protocol`, `mystira` |
| **Visibility** | Private |
| **License** | Proprietary |

```
Mystira.Chain/
├── app/
│   ├── main.py            # FastAPI app entry point
│   ├── routers/
│   │   ├── ip_assets.py   # IP Asset registration endpoints
│   │   └── royalties.py   # Royalty payment/claiming endpoints
│   ├── services/
│   │   └── story_protocol.py  # SDK wrapper
│   ├── models/
│   │   └── schemas.py     # Pydantic models
│   └── config.py          # Environment configuration
├── requirements.txt
├── pyproject.toml
├── Dockerfile
└── README.md
```

#### Pydantic Schema Examples

```python
# app/models/schemas.py
from datetime import datetime
from enum import Enum
from pydantic import BaseModel, Field


class ContributorType(str, Enum):
    """Type of contributor to a story."""
    PUBLISHER = "publisher"
    CURATOR = "curator"
    AUTHOR = "author"


class Contributor(BaseModel):
    """A contributor to a story with royalty share."""
    wallet_address: str = Field(..., description="Ethereum wallet address")
    contributor_type: ContributorType
    share_percentage: float = Field(..., ge=0, le=100)


class RegisterIpAssetRequest(BaseModel):
    """Request to register a story as an IP Asset."""
    content_id: str = Field(..., description="Unique content identifier")
    content_title: str = Field(..., description="Title of the story")
    contributors: list[Contributor] = Field(..., min_length=1)
    metadata_uri: str | None = Field(None, description="IPFS URI for metadata")

    model_config = {
        "json_schema_extra": {
            "example": {
                "content_id": "story-12345",
                "content_title": "The Dragon's Quest",
                "contributors": [
                    {"wallet_address": "0x1234...", "contributor_type": "publisher", "share_percentage": 10},
                    {"wallet_address": "0x5678...", "contributor_type": "curator", "share_percentage": 10}
                ],
                "metadata_uri": "ipfs://Qm..."
            }
        }
    }


class IpAssetStatus(str, Enum):
    """Status of IP Asset registration."""
    PENDING = "pending"
    PROCESSING = "processing"
    REGISTERED = "registered"
    FAILED = "failed"


class IpAssetResponse(BaseModel):
    """Response after IP Asset registration."""
    content_id: str
    ip_asset_id: str | None = None
    transaction_hash: str | None = None
    status: IpAssetStatus
    registered_at: datetime | None = None
    error_message: str | None = None


class PayRoyaltyRequest(BaseModel):
    """Request to pay royalties to an IP Asset."""
    ip_asset_id: str = Field(..., description="Story Protocol IP Asset ID")
    amount_wei: str = Field(..., description="Amount in wei to distribute")
    currency_token: str = Field(default="0x0", description="Token address (0x0 for native)")


class RoyaltyPaymentResponse(BaseModel):
    """Response after royalty payment."""
    ip_asset_id: str
    transaction_hash: str | None = None
    amount_wei: str
    status: str
    error_message: str | None = None


class ErrorDetail(BaseModel):
    """Error details for API responses."""
    code: str
    message: str
    details: dict | None = None


class ErrorResponse(BaseModel):
    """Standard error response format."""
    error: ErrorDetail
    request_id: str | None = None
```

#### Dockerfile

```dockerfile
# Dockerfile for Mystira.Chain
FROM python:3.11-slim

# Set working directory
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# Install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY app/ ./app/

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health')"

# Run application
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

#### requirements.txt

```txt
# Core
fastapi>=0.109.0
uvicorn[standard]>=0.27.0
pydantic>=2.5.0
pydantic-settings>=2.1.0

# Story Protocol SDK
# story-protocol-python-sdk>=0.1.0  # Uncomment when available

# Web3
web3>=6.15.0

# Observability
opentelemetry-api>=1.22.0
opentelemetry-sdk>=1.22.0
opentelemetry-instrumentation-fastapi>=0.43b0

# Testing
pytest>=7.4.0
pytest-asyncio>=0.23.0
httpx>=0.26.0
```

#### 2. .NET Adapter: `ChainServiceAdapter`

Replace current `StoryProtocolService` registration with new HTTP-based adapter:

```csharp
// Infrastructure.StoryProtocol/Services/ChainServiceAdapter.cs
public class ChainServiceAdapter : IStoryProtocolService
{
    private readonly HttpClient _httpClient;
    private readonly ChainServiceOptions _options;

    public async Task<StoryProtocolMetadata> RegisterIpAssetAsync(...)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_options.BaseUrl}/ip-assets/register",
            new { contentId, contentTitle, contributors, metadataUri });
        // ...
    }
}
```

#### 3. Configuration

```json
// appsettings.json
{
  "ChainService": {
    "BaseUrl": "https://mystira-chain.azurewebsites.net",
    "TimeoutSeconds": 120,
    "RetryCount": 3,
    "ApiKey": "${CHAIN_SERVICE_API_KEY}"
  }
}
```

> **Note**: Azure App Service names cannot contain dots. Use `mystira-chain` for the Azure resource name.

### MVP Implementation (Phase 1)

For immediate delivery, implement minimal endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/ip-assets/register` | POST | Register story as IP Asset |
| `/ip-assets/{content_id}/status` | GET | Check registration status |
| `/royalties/pay` | POST | Pay royalties to IP Asset |
| `/health` | GET | Health check endpoint |

Defer to Phase 2:
- Royalty claiming (`/royalties/{ip_asset_id}/claim`)
- Advanced license terms
- Derivative works

### Security & Authentication

Service-to-service communication secured via:
1. **API Key**: Shared secret in `X-API-Key` header
2. **Network Isolation**: Both services in same Azure VNet (production)
3. **HTTPS Only**: TLS 1.2+ required

```python
# Mystira.Chain authentication middleware
from fastapi import Header, HTTPException

async def verify_api_key(x_api_key: str = Header(...)):
    if x_api_key != settings.API_KEY:
        raise HTTPException(status_code=401, detail="Invalid API key")
```

### Error Response Schema

All errors return consistent format:

```json
{
  "error": {
    "code": "REGISTRATION_FAILED",
    "message": "Failed to register IP asset",
    "details": {
      "tx_hash": "0x...",
      "reason": "Insufficient gas"
    }
  },
  "request_id": "uuid-here"
}
```

### Integration Points

1. **Story Generator / Admin Portal**: Add "Register on Blockchain" button
   - Triggers `POST /admin/royalties/register-ip-asset` in Admin.API
   - Admin.API calls `IStoryProtocolService.RegisterIpAssetAsync()`
   - ChainServiceAdapter forwards to Mystira.Chain

2. **Royalty Configuration**: Admin portal allows setting percentages per contributor

### Fallback Strategy

Keep `MockStoryProtocolService` as fallback when:
- Chain service is unavailable
- Running in development/test mode
- Feature flag disabled

```csharp
services.AddScoped<IStoryProtocolService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StoryProtocolOptions>>().Value;
    if (options.UseMockImplementation)
        return sp.GetRequiredService<MockStoryProtocolService>();
    return sp.GetRequiredService<ChainServiceAdapter>();
});
```

---

## Consequences

### Positive Consequences ✅

1. **Official SDK Support**
   - Story Protocol Python SDK is actively maintained
   - Automatic compatibility with protocol upgrades
   - Access to SDK-specific features and optimizations

2. **Clean Architecture Preserved**
   - Hexagonal architecture maintained via port/adapter pattern
   - No changes required to Application or Domain layers
   - Easy to swap implementations

3. **Independent Scaling**
   - Chain service can scale independently from main API
   - Blockchain operations don't block API responses

4. **Better Developer Experience**
   - Python is familiar to team member (partner)
   - FastAPI provides automatic OpenAPI documentation
   - Excellent blockchain ecosystem (web3.py, eth-brownie)
   - Pydantic for automatic validation

5. **Future Flexibility**
   - Easy to add new blockchain features
   - Can support multiple chains if needed
   - Service can be reused by other applications

### Negative Consequences ❌

1. **Additional Infrastructure**
   - New service to deploy, monitor, and maintain
   - Additional Azure resources (App Service or Container)
   - Cross-service communication complexity

2. **Network Overhead**
   - HTTP calls between services add latency
   - Must handle network failures gracefully
   - Need proper retry/timeout configuration

3. **Operational Complexity**
   - Two services to coordinate deployments
   - Version compatibility between services
   - Distributed logging and tracing

4. **Initial Development Cost**
   - Time to set up new repository and CI/CD
   - Learning curve for Story Protocol SDK
   - Integration testing across services

### Mitigation Strategies

| Risk | Mitigation |
|------|------------|
| Service unavailability | Feature flag to fall back to mock; health checks; alerts |
| Network latency | Same-region deployment; connection pooling; caching reads |
| Version drift | Semantic versioning; integration tests; contract testing |
| Debugging complexity | Correlation IDs; distributed tracing (App Insights) |

---

## Implementation Plan

### Phase 1: MVP (Target: 2 days)

1. Create `Mystira.Chain` repository (Python/FastAPI)
2. Implement core endpoints (register, status)
3. Create `ChainServiceAdapter` in .NET
4. Deploy to Azure App Service (Python)
5. Update Admin portal with registration button

### Phase 2: Production Hardening

1. Add comprehensive error handling
2. Implement retry logic with exponential backoff
3. Add distributed tracing
4. Set up monitoring and alerts
5. Load testing

### Phase 3: Full Features

1. Implement royalty payment flow
2. Add royalty claiming
3. Support license terms configuration
4. Add derivative works registration

---

## Alternatives Not Chosen

### TypeScript SDK
- Originally considered due to type safety benefits
- However, partner/team member is less familiar with TypeScript
- Python SDK provides equivalent functionality
- TypeScript would add Node.js complexity to infrastructure

### Embedded WebAssembly
- TypeScript SDK compiled to WASM and run in .NET
- Too experimental; not worth the risk
- Debugging would be extremely difficult

### gRPC Communication
- Higher performance but more complex setup
- REST is sufficient for current throughput needs
- REST easier to debug and test

---

## Related Decisions

- **ADR-0003**: Hexagonal Architecture (this decision maintains port/adapter pattern)
- **ADR-0005**: Separate API and Admin API (blockchain operations via Admin API)
- **ADR-0011**: Unified Workspace Repository (Mystira.Chain included in workspace)
- **ADR-0012**: Infrastructure as Code (networking and Front Door for Mystira.Chain)

---

## References

- [Story Protocol Python SDK](https://github.com/storyprotocol/python-sdk)
- [Story Protocol Documentation](https://docs.story.foundation/)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Hexagonal Architecture Refactoring Summary](../HEXAGONAL_ARCHITECTURE_REFACTORING_SUMMARY.md)
- [IStoryProtocolService Port](../../../src/Mystira.App.Application/Ports/IStoryProtocolService.cs)

---

## Notes

- This ADR was created based on team discussion on 2025-12-10
- MVP timeline is aggressive; scope may be adjusted
- Python chosen over TypeScript due to team member familiarity
- TypeScript SDK remains a viable alternative if needed

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
