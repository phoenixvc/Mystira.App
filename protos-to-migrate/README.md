# Protocol Buffer Definitions

This directory contains Protocol Buffer (`.proto`) files defining the gRPC service contracts for inter-service communication in the Mystira ecosystem.

## Directory Structure

```
protos/
└── mystira/
    └── chain/
        └── v1/
            ├── chain_service.proto   # Main service definition
            ├── ip_assets.proto       # IP Asset related messages
            ├── royalties.proto       # Royalty related messages
            └── common.proto          # Shared types
```

## Usage

### .NET (Mystira.App)

Proto files are automatically compiled via the `Grpc.Tools` package. Add to your `.csproj`:

```xml
<ItemGroup>
  <Protobuf Include="..\..\protos\mystira\chain\v1\*.proto"
            GrpcServices="Client"
            ProtoRoot="..\..\protos" />
</ItemGroup>
```

### Python (Mystira.Chain)

Generate Python code using:

```bash
python -m grpc_tools.protoc \
  -I./protos \
  --python_out=./app \
  --grpc_python_out=./app \
  ./protos/mystira/chain/v1/*.proto
```

## Service Overview

### ChainService

The `ChainService` provides blockchain integration operations via Story Protocol:

| Method | Type | Description |
|--------|------|-------------|
| `RegisterIpAsset` | Unary | Register content as an IP Asset |
| `IsRegistered` | Unary | Check if content is registered |
| `GetIpAssetStatus` | Unary | Get registration status |
| `BatchRegisterIpAssets` | Client Streaming | Register multiple assets |
| `GetRoyaltyConfiguration` | Unary | Get royalty config |
| `UpdateRoyaltySplit` | Unary | Update royalty splits |
| `PayRoyalties` | Unary | Pay royalties to IP Asset |
| `GetClaimableRoyalties` | Unary | Get claimable balance |
| `ClaimRoyalties` | Unary | Claim accumulated royalties |
| `WatchTransactions` | Server Streaming | Stream transaction updates |
| `HealthCheck` | Unary | Service health check |
| `GetServiceInfo` | Unary | Get service metadata |

## Versioning

- API version is encoded in the package name: `mystira.chain.v1`
- Breaking changes require a new version (e.g., `v2`)
- Non-breaking changes can be added to existing version

## Related Documentation

- [ADR-0013: gRPC for C#/Python Integration](../docs/architecture/adr/ADR-0013-grpc-for-csharp-python-integration.md)
- [Implementation Roadmap - Phase 1.5](../docs/planning/implementation-roadmap.md#phase-15-polyglot-integration-grpc)

---

**Last Updated**: 2025-12-22
