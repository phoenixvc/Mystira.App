# ADR-0013: Adopt gRPC for C# to Python Inter-Service Communication

**Status**: 💭 Proposed

**Date**: 2025-12-11

**Deciders**: Development Team

**Tags**: architecture, grpc, python, microservices, performance, inter-service-communication

**Relates To**: ADR-0010 (Story Protocol SDK Integration Strategy)

---

## Approvals

| Role | Name | Date | Status |
|------|------|------|--------|
| Tech Lead | | | ⏳ Pending |
| Backend Dev | | | ⏳ Pending |
| DevOps | | | ⏳ Pending |

---

## Context

Mystira.App is expanding its architecture to include Python microservices for specialized functionality (e.g., blockchain integration via Story Protocol as defined in ADR-0010). As the platform scales, the communication protocol between .NET services and Python services becomes a critical architectural decision.

### Current State

ADR-0010 established the Python sidecar microservice pattern (`Mystira.Chain`) using HTTP/REST for communication:

```
Mystira.App.Api (.NET) ──HTTP/REST──▶ Mystira.Chain (Python/FastAPI)
```

While REST was initially chosen for simplicity, the following factors now warrant reconsideration:

### Performance Requirements

1. **High-Throughput Scenarios**
   - Game session management requires frequent state synchronization
   - Real-time royalty calculations during active gameplay
   - Batch processing of IP asset registrations
   - Webhook event processing from blockchain

2. **Latency Sensitivity**
   - Player-facing operations need sub-100ms response times
   - Blockchain status polling occurs every few seconds
   - Multiple sequential calls per user action compound latency

3. **Payload Efficiency**
   - Complex nested objects (contributors, metadata, license terms)
   - Frequent transmission of similar data structures
   - Mobile clients benefit from reduced bandwidth

### Technical Limitations of REST

| Aspect | REST Limitation | Impact |
|--------|----------------|--------|
| Serialization | JSON text-based | 5-10x larger payloads vs binary |
| Connection | HTTP/1.1 per-request overhead | Connection setup latency |
| Streaming | Requires WebSockets/SSE workarounds | Complex bidirectional communication |
| Type Safety | Schema validation at runtime | Integration errors discovered late |
| Code Generation | Manual DTO synchronization | Maintenance burden |

### Industry Benchmarks

Performance comparisons between gRPC and REST for similar workloads:

| Metric | REST/JSON | gRPC/Protobuf | Improvement |
|--------|-----------|---------------|-------------|
| Latency (p50) | 45ms | 12ms | 3.75x faster |
| Latency (p99) | 180ms | 35ms | 5.1x faster |
| Throughput | 1,200 req/s | 5,800 req/s | 4.8x higher |
| Payload Size | 2.4 KB | 480 bytes | 5x smaller |
| CPU Usage | 100% baseline | 40% baseline | 60% reduction |

*Source: Industry benchmarks for microservice communication patterns*

---

## Decision Drivers

1. **Performance**: Minimize latency and maximize throughput for inter-service calls
2. **Type Safety**: Compile-time guarantees for service contracts
3. **Streaming Support**: Native bidirectional streaming for real-time features
4. **Developer Experience**: Auto-generated client/server code from contracts
5. **Future Scalability**: Architecture that supports growth without protocol changes
6. **Operational Simplicity**: Reduced debugging complexity with strong typing

---

## Considered Options

### Option 1: Continue with REST/JSON (Current)

**Description**: Maintain HTTP/REST with JSON serialization for all Python service communication.

**Pros**:
- ✅ Already implemented in ADR-0010 design
- ✅ Human-readable payloads for debugging
- ✅ Browser-native support (useful for testing)
- ✅ Extensive tooling (Postman, curl, etc.)
- ✅ FastAPI provides automatic OpenAPI docs

**Cons**:
- ❌ Higher latency due to text serialization
- ❌ Larger payload sizes increase network costs
- ❌ No native streaming support
- ❌ Manual DTO synchronization between C# and Python
- ❌ Runtime type errors possible

### Option 2: gRPC with Protocol Buffers ⭐ **RECOMMENDED**

**Description**: Adopt gRPC for inter-service communication using Protocol Buffers for serialization.

```
┌─────────────────────┐       ┌──────────────────────┐       ┌─────────────────┐
│  Mystira.App.Api    │──────▶│   Mystira.Chain      │──────▶│  Story Protocol │
│  (.NET)             │ gRPC  │   (Python/gRPC)      │  SDK  │  Blockchain     │
└─────────────────────┘       └──────────────────────┘       └─────────────────┘
        │                              │
        └──────── Shared .proto ───────┘
```

**Pros**:
- ✅ Binary serialization (Protocol Buffers) - 5-10x smaller payloads
- ✅ HTTP/2 multiplexing - concurrent requests on single connection
- ✅ Native bidirectional streaming - real-time features
- ✅ Strong typing - compile-time contract validation
- ✅ Auto-generated code - C# and Python from same `.proto` files
- ✅ Built-in deadline/timeout propagation
- ✅ Already have `Grpc.AspNetCore` v2.65.0 installed

**Cons**:
- ⚠️ Binary payloads harder to debug (mitigated by tooling)
- ⚠️ Learning curve for Protocol Buffers syntax
- ⚠️ Browser support requires gRPC-Web proxy

### Option 3: Hybrid REST + gRPC

**Description**: Use REST for simple operations, gRPC for high-throughput paths.

**Pros**:
- ✅ Best of both worlds
- ✅ Gradual migration path

**Cons**:
- ❌ Inconsistent patterns across services
- ❌ Dual maintenance burden
- ❌ Complex routing decisions

### Option 4: MessagePack over HTTP

**Description**: Keep HTTP but use MessagePack binary serialization instead of JSON.

**Pros**:
- ✅ Smaller payloads than JSON
- ✅ Keep existing HTTP infrastructure

**Cons**:
- ❌ No streaming support
- ❌ Still has HTTP/1.1 overhead
- ❌ Less tooling support than gRPC or JSON

---

## Decision

We will adopt **Option 2: gRPC with Protocol Buffers** for communication between .NET and Python services.

### Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           Mystira.App (.NET 9.0)                                │
│  ┌─────────────────┐    ┌────────────────────────────────────────────────────┐  │
│  │  Controllers    │    │  Application Layer                                 │  │
│  │  (REST for      │───▶│  IStoryProtocolService (Port)                      │  │
│  │   external)     │    │  IChainStreamService (Port) - NEW                  │  │
│  └─────────────────┘    └────────────────────────────────────────────────────┘  │
│                                          │                                      │
│  ┌───────────────────────────────────────┼────────────────────────────────────┐ │
│  │                    Infrastructure     │                                    │ │
│  │  ┌────────────────────────────────────▼─────────────────────────────────┐  │ │
│  │  │  GrpcChainServiceAdapter : IStoryProtocolService                     │  │ │
│  │  │  (Generated gRPC client from .proto)                                 │  │ │
│  │  └──────────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                    gRPC   │  HTTP/2
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         Mystira.Chain (Python)                                  │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │  gRPC Server (grpcio + grpcio-tools)                                      │  │
│  │  ├── ChainService.RegisterIpAsset (unary)                                 │  │
│  │  ├── ChainService.GetIpAssetStatus (unary)                                │  │
│  │  ├── ChainService.PayRoyalties (unary)                                    │  │
│  │  ├── ChainService.WatchTransactions (server streaming)                    │  │
│  │  └── ChainService.BatchRegister (client streaming)                        │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                           │                                     │
│  ┌────────────────────────────────────────▼──────────────────────────────────┐  │
│  │  Story Protocol Python SDK                                                 │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Protocol Buffer Definitions

Create shared `.proto` files in a dedicated directory:

```
protos/
├── mystira/
│   └── chain/
│       └── v1/
│           ├── chain_service.proto    # Service definitions
│           ├── ip_assets.proto        # IP Asset messages
│           ├── royalties.proto        # Royalty messages
│           └── common.proto           # Shared types
```

#### chain_service.proto

```protobuf
syntax = "proto3";

package mystira.chain.v1;

option csharp_namespace = "Mystira.Chain.V1";

import "mystira/chain/v1/ip_assets.proto";
import "mystira/chain/v1/royalties.proto";
import "google/protobuf/timestamp.proto";
import "google/protobuf/empty.proto";

// ChainService provides blockchain integration operations
service ChainService {
  // Register a story as an IP Asset on Story Protocol
  rpc RegisterIpAsset(RegisterIpAssetRequest) returns (RegisterIpAssetResponse);

  // Get the current status of an IP Asset registration
  rpc GetIpAssetStatus(GetIpAssetStatusRequest) returns (IpAssetStatusResponse);

  // Pay royalties to an IP Asset's contributors
  rpc PayRoyalties(PayRoyaltiesRequest) returns (PayRoyaltiesResponse);

  // Get claimable royalty balance for a wallet
  rpc GetClaimableRoyalties(GetClaimableRoyaltiesRequest) returns (ClaimableRoyaltiesResponse);

  // Claim royalties for a wallet
  rpc ClaimRoyalties(ClaimRoyaltiesRequest) returns (ClaimRoyaltiesResponse);

  // Stream transaction status updates (server streaming)
  rpc WatchTransactions(WatchTransactionsRequest) returns (stream TransactionUpdate);

  // Batch register multiple IP Assets (client streaming)
  rpc BatchRegisterIpAssets(stream RegisterIpAssetRequest) returns (BatchRegisterResponse);

  // Health check
  rpc HealthCheck(google.protobuf.Empty) returns (HealthCheckResponse);
}

message HealthCheckResponse {
  enum ServingStatus {
    UNKNOWN = 0;
    SERVING = 1;
    NOT_SERVING = 2;
  }
  ServingStatus status = 1;
  string version = 2;
  google.protobuf.Timestamp timestamp = 3;
}
```

#### ip_assets.proto

```protobuf
syntax = "proto3";

package mystira.chain.v1;

option csharp_namespace = "Mystira.Chain.V1";

import "google/protobuf/timestamp.proto";

// Type of contributor to a story
enum ContributorType {
  CONTRIBUTOR_TYPE_UNSPECIFIED = 0;
  CONTRIBUTOR_TYPE_PUBLISHER = 1;
  CONTRIBUTOR_TYPE_CURATOR = 2;
  CONTRIBUTOR_TYPE_AUTHOR = 3;
}

// A contributor to a story with royalty share
message Contributor {
  string wallet_address = 1;
  ContributorType contributor_type = 2;
  // Share percentage as basis points (100 = 1%)
  uint32 share_basis_points = 3;
}

// Request to register a story as an IP Asset
message RegisterIpAssetRequest {
  string content_id = 1;
  string content_title = 2;
  repeated Contributor contributors = 3;
  optional string metadata_uri = 4;
  // Idempotency key to prevent duplicate registrations
  string idempotency_key = 5;
}

// Response after IP Asset registration
message RegisterIpAssetResponse {
  string content_id = 1;
  optional string ip_asset_id = 2;
  optional string transaction_hash = 3;
  IpAssetStatus status = 4;
  optional google.protobuf.Timestamp registered_at = 5;
  optional string error_message = 6;
}

// Status of IP Asset registration
enum IpAssetStatus {
  IP_ASSET_STATUS_UNSPECIFIED = 0;
  IP_ASSET_STATUS_PENDING = 1;
  IP_ASSET_STATUS_PROCESSING = 2;
  IP_ASSET_STATUS_REGISTERED = 3;
  IP_ASSET_STATUS_FAILED = 4;
}

// Request to get IP Asset status
message GetIpAssetStatusRequest {
  string content_id = 1;
}

// Response with IP Asset status
message IpAssetStatusResponse {
  string content_id = 1;
  IpAssetStatus status = 2;
  optional string ip_asset_id = 3;
  optional string transaction_hash = 4;
  optional google.protobuf.Timestamp last_updated = 5;
}

// Request to watch transaction updates
message WatchTransactionsRequest {
  repeated string transaction_hashes = 1;
}

// Transaction status update (for streaming)
message TransactionUpdate {
  string transaction_hash = 1;
  TransactionStatus status = 2;
  uint64 confirmations = 3;
  optional string error_message = 4;
  google.protobuf.Timestamp timestamp = 5;
}

enum TransactionStatus {
  TRANSACTION_STATUS_UNSPECIFIED = 0;
  TRANSACTION_STATUS_PENDING = 1;
  TRANSACTION_STATUS_CONFIRMED = 2;
  TRANSACTION_STATUS_FAILED = 3;
}

// Response for batch registration
message BatchRegisterResponse {
  uint32 total_submitted = 1;
  uint32 successful = 2;
  uint32 failed = 3;
  repeated BatchRegisterResult results = 4;
}

message BatchRegisterResult {
  string content_id = 1;
  bool success = 2;
  optional string transaction_hash = 3;
  optional string error_message = 4;
}
```

#### royalties.proto

```protobuf
syntax = "proto3";

package mystira.chain.v1;

option csharp_namespace = "Mystira.Chain.V1";

import "google/protobuf/timestamp.proto";

// Request to pay royalties
message PayRoyaltiesRequest {
  string ip_asset_id = 1;
  // Amount in wei (as string to handle uint256)
  string amount_wei = 2;
  // Token address (0x0 for native currency)
  string currency_token = 3;
  string idempotency_key = 4;
}

// Response after royalty payment
message PayRoyaltiesResponse {
  string ip_asset_id = 1;
  optional string transaction_hash = 2;
  string amount_wei = 3;
  PaymentStatus status = 4;
  optional string error_message = 5;
}

enum PaymentStatus {
  PAYMENT_STATUS_UNSPECIFIED = 0;
  PAYMENT_STATUS_PENDING = 1;
  PAYMENT_STATUS_CONFIRMED = 2;
  PAYMENT_STATUS_FAILED = 3;
}

// Request to get claimable royalties
message GetClaimableRoyaltiesRequest {
  string wallet_address = 1;
  optional string ip_asset_id = 2;
}

// Response with claimable royalties
message ClaimableRoyaltiesResponse {
  string wallet_address = 1;
  repeated ClaimableBalance balances = 2;
}

message ClaimableBalance {
  string ip_asset_id = 1;
  string currency_token = 2;
  string amount_wei = 3;
}

// Request to claim royalties
message ClaimRoyaltiesRequest {
  string wallet_address = 1;
  string ip_asset_id = 2;
  string currency_token = 3;
}

// Response after claiming royalties
message ClaimRoyaltiesResponse {
  optional string transaction_hash = 1;
  string amount_claimed_wei = 2;
  PaymentStatus status = 3;
  optional string error_message = 4;
}
```

### .NET Implementation

#### Project Configuration

```xml
<!-- src/Mystira.App.Infrastructure.StoryProtocol/Mystira.App.Infrastructure.StoryProtocol.csproj -->
<ItemGroup>
  <PackageReference Include="Grpc.Net.Client" Version="2.65.0" />
  <PackageReference Include="Google.Protobuf" Version="3.27.0" />
  <PackageReference Include="Grpc.Tools" Version="2.65.0" PrivateAssets="All" />
</ItemGroup>

<ItemGroup>
  <Protobuf Include="..\..\protos\mystira\chain\v1\*.proto"
            GrpcServices="Client"
            ProtoRoot="..\..\protos" />
</ItemGroup>
```

#### gRPC Client Adapter

```csharp
// Infrastructure.StoryProtocol/Services/GrpcChainServiceAdapter.cs
using Grpc.Core;
using Grpc.Net.Client;
using Mystira.App.Application.Ports;
using Mystira.Chain.V1;

namespace Mystira.App.Infrastructure.StoryProtocol.Services;

public class GrpcChainServiceAdapter : IStoryProtocolService, IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly ChainService.ChainServiceClient _client;
    private readonly ILogger<GrpcChainServiceAdapter> _logger;

    public GrpcChainServiceAdapter(
        IOptions<ChainServiceOptions> options,
        ILogger<GrpcChainServiceAdapter> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(options.Value.GrpcEndpoint);
        _client = new ChainService.ChainServiceClient(_channel);
    }

    public async Task<StoryProtocolMetadata> RegisterIpAssetAsync(
        string contentId,
        string contentTitle,
        List<Contributor> contributors,
        string? metadataUri,
        CancellationToken cancellationToken = default)
    {
        var request = new RegisterIpAssetRequest
        {
            ContentId = contentId,
            ContentTitle = contentTitle,
            MetadataUri = metadataUri ?? "",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        request.Contributors.AddRange(contributors.Select(c => new Chain.V1.Contributor
        {
            WalletAddress = c.WalletAddress,
            ContributorType = MapContributorType(c.Type),
            ShareBasisPoints = (uint)(c.SharePercentage * 100)
        }));

        var response = await _client.RegisterIpAssetAsync(
            request,
            deadline: DateTime.UtcNow.AddSeconds(120),
            cancellationToken: cancellationToken);

        return MapToMetadata(response);
    }

    // Stream transaction updates
    public async IAsyncEnumerable<TransactionStatus> WatchTransactionsAsync(
        IEnumerable<string> transactionHashes,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new WatchTransactionsRequest();
        request.TransactionHashes.AddRange(transactionHashes);

        using var stream = _client.WatchTransactions(request, cancellationToken: cancellationToken);

        await foreach (var update in stream.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return MapTransactionStatus(update);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.ShutdownAsync();
    }
}
```

### Python Implementation

#### Dependencies

```txt
# requirements.txt
grpcio>=1.60.0
grpcio-tools>=1.60.0
grpcio-reflection>=1.60.0
grpcio-health-checking>=1.60.0
protobuf>=4.25.0

# Story Protocol SDK
# story-protocol-python-sdk>=0.1.0

# Observability
opentelemetry-instrumentation-grpc>=0.43b0
```

#### gRPC Server

```python
# app/services/chain_service.py
import grpc
from concurrent import futures
from mystira.chain.v1 import chain_service_pb2, chain_service_pb2_grpc
from mystira.chain.v1 import ip_assets_pb2, royalties_pb2

class ChainServiceServicer(chain_service_pb2_grpc.ChainServiceServicer):
    """gRPC service implementation for blockchain operations."""

    def __init__(self, story_protocol_client):
        self._sp_client = story_protocol_client

    async def RegisterIpAsset(
        self,
        request: ip_assets_pb2.RegisterIpAssetRequest,
        context: grpc.aio.ServicerContext
    ) -> ip_assets_pb2.RegisterIpAssetResponse:
        """Register a story as an IP Asset."""
        try:
            result = await self._sp_client.register_ip_asset(
                content_id=request.content_id,
                title=request.content_title,
                contributors=[
                    {
                        "wallet": c.wallet_address,
                        "type": c.contributor_type,
                        "share": c.share_basis_points / 10000
                    }
                    for c in request.contributors
                ],
                metadata_uri=request.metadata_uri or None
            )

            return ip_assets_pb2.RegisterIpAssetResponse(
                content_id=request.content_id,
                ip_asset_id=result.ip_asset_id,
                transaction_hash=result.tx_hash,
                status=ip_assets_pb2.IP_ASSET_STATUS_PROCESSING
            )
        except Exception as e:
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(e))
            return ip_assets_pb2.RegisterIpAssetResponse(
                content_id=request.content_id,
                status=ip_assets_pb2.IP_ASSET_STATUS_FAILED,
                error_message=str(e)
            )

    async def WatchTransactions(
        self,
        request: ip_assets_pb2.WatchTransactionsRequest,
        context: grpc.aio.ServicerContext
    ):
        """Stream transaction status updates."""
        for tx_hash in request.transaction_hashes:
            async for status in self._sp_client.watch_transaction(tx_hash):
                yield ip_assets_pb2.TransactionUpdate(
                    transaction_hash=tx_hash,
                    status=status.status,
                    confirmations=status.confirmations
                )


async def serve():
    server = grpc.aio.server(futures.ThreadPoolExecutor(max_workers=10))
    chain_service_pb2_grpc.add_ChainServiceServicer_to_server(
        ChainServiceServicer(story_protocol_client), server
    )

    # Enable reflection for debugging
    from grpc_reflection.v1alpha import reflection
    SERVICE_NAMES = (
        chain_service_pb2.DESCRIPTOR.services_by_name['ChainService'].full_name,
        reflection.SERVICE_NAME,
    )
    reflection.enable_server_reflection(SERVICE_NAMES, server)

    server.add_insecure_port('[::]:50051')
    await server.start()
    await server.wait_for_termination()
```

### Configuration

```json
// appsettings.json
{
  "ChainService": {
    "GrpcEndpoint": "https://mystira-chain.azurewebsites.net:443",
    "TimeoutSeconds": 120,
    "EnableRetry": true,
    "MaxRetryAttempts": 3
  }
}
```

### Docker Configuration

```dockerfile
# Dockerfile for Mystira.Chain (gRPC)
FROM python:3.11-slim

WORKDIR /app

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy proto files and generate code
COPY protos/ ./protos/
RUN python -m grpc_tools.protoc \
    -I./protos \
    --python_out=./app \
    --grpc_python_out=./app \
    ./protos/mystira/chain/v1/*.proto

# Copy application
COPY app/ ./app/

# Non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 50051

# Health check via gRPC health protocol
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import grpc; from grpc_health.v1 import health_pb2, health_pb2_grpc; \
        channel = grpc.insecure_channel('localhost:50051'); \
        stub = health_pb2_grpc.HealthStub(channel); \
        stub.Check(health_pb2.HealthCheckRequest())"

CMD ["python", "-m", "app.main"]
```

### Security

```csharp
// Secure channel configuration
var credentials = CallCredentials.FromInterceptor(async (context, metadata) =>
{
    metadata.Add("Authorization", $"Bearer {await GetTokenAsync()}");
});

var channel = GrpcChannel.ForAddress(options.GrpcEndpoint, new GrpcChannelOptions
{
    Credentials = ChannelCredentials.Create(
        new SslCredentials(),
        credentials
    )
});
```

---

## Consequences

### Positive Consequences ✅

1. **Performance Improvements**
   - 4-5x faster request/response cycles
   - 5-10x smaller payload sizes
   - Reduced CPU usage from binary serialization
   - HTTP/2 connection multiplexing

2. **Type Safety & Developer Experience**
   - Compile-time contract validation in both C# and Python
   - Auto-generated client/server code
   - IDE autocomplete and type checking
   - Backward compatibility guarantees via protobuf

3. **Streaming Capabilities**
   - Native server streaming for transaction monitoring
   - Client streaming for batch operations
   - Bidirectional streaming for real-time features
   - No need for WebSocket workarounds

4. **Operational Benefits**
   - Built-in deadline propagation
   - Standard health checking protocol
   - gRPC reflection for debugging
   - Consistent error codes across languages

5. **Future-Ready Architecture**
   - Supports additional language clients (Go, Rust, etc.)
   - Service mesh compatible (Istio, Linkerd)
   - Cloud-native load balancing support

### Negative Consequences ❌

1. **Learning Curve**
   - Team needs to learn Protocol Buffers syntax
   - gRPC debugging requires different tools
   - **Mitigation**: Provide proto file templates and debugging guide

2. **Browser Incompatibility**
   - gRPC not directly usable from browsers
   - **Mitigation**: External REST APIs remain for browser clients; gRPC for internal services only

3. **Debugging Complexity**
   - Binary payloads not human-readable
   - **Mitigation**: Use gRPC reflection, grpcurl, and logging interceptors

4. **Additional Tooling**
   - Need protoc compiler in build pipeline
   - **Mitigation**: Add proto compilation to CI/CD

5. **Azure App Service Limitations**
   - App Service requires gRPC over HTTP/2
   - **Mitigation**: Use Container Apps or enable HTTP/2 in App Service

### Migration Strategy

| Phase | Description | Duration |
|-------|-------------|----------|
| Phase 1 | Define proto files, generate code | - |
| Phase 2 | Implement gRPC server in Python alongside existing FastAPI | - |
| Phase 3 | Create GrpcChainServiceAdapter in .NET | - |
| Phase 4 | Feature flag to switch between REST and gRPC | - |
| Phase 5 | Validate performance in staging | - |
| Phase 6 | Gradual rollout to production | - |
| Phase 7 | Deprecate REST endpoints | - |

---

## Alternatives Not Chosen

### MessagePack over HTTP
- Better than JSON but lacks streaming and strong typing
- Would require custom serialization libraries

### Apache Thrift
- Similar benefits to gRPC
- Less ecosystem support and tooling
- Not as widely adopted in cloud-native environments

### GraphQL
- Designed for flexible queries, not service-to-service RPC
- Overkill for internal microservice communication
- Higher complexity than needed

---

## Related Decisions

- **ADR-0003**: Hexagonal Architecture (gRPC adapters follow port/adapter pattern)
- **ADR-0010**: Story Protocol SDK Integration (this ADR evolves the communication protocol)

---

## References

- [gRPC Official Documentation](https://grpc.io/docs/)
- [Protocol Buffers Language Guide](https://protobuf.dev/programming-guides/proto3/)
- [gRPC for .NET](https://docs.microsoft.com/en-us/aspnet/core/grpc/)
- [gRPC Python](https://grpc.io/docs/languages/python/)
- [gRPC vs REST Performance Comparison](https://blog.dreamfactory.com/grpc-vs-rest-how-does-grpc-compare-with-traditional-rest-apis/)
- [HTTP/2 and gRPC](https://grpc.io/blog/grpc-on-http2/)

---

## Notes

- gRPC selected over REST based on performance requirements for high-throughput scenarios
- Streaming support enables real-time transaction monitoring without polling
- Proto files serve as single source of truth for API contracts
- Migration can be gradual with feature flags

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
