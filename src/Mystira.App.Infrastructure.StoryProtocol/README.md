# Mystira.App.Infrastructure.StoryProtocol

Infrastructure layer implementation for Story Protocol blockchain integration. This project handles royalty splits and IP asset registration for content creators.

## Overview

Story Protocol is a blockchain-based IP management system that enables:
- Registration of content as IP assets on-chain
- Automatic royalty distribution among contributors
- Transparent revenue sharing based on contribution percentages
- Immutable record of IP ownership and licenses

## Current Implementation

The current implementation provides a **mock/stub service** for development and testing purposes. This allows the application to function without actual blockchain interactions while the full integration is being developed.

### Mock Service Features

- Simulates IP asset registration
- Generates mock transaction hashes and asset IDs
- Stores registrations in-memory (not persisted)
- Validates contributor splits
- Logs all operations for debugging

## Configuration

Add the following to your `appsettings.json`:

```json
{
  "StoryProtocol": {
    "Enabled": true,
    "UseMockImplementation": true,
    "Network": "testnet",
    "RpcUrl": "https://rpc.testnet.story.foundation",
    "Contracts": {
      "IpAssetRegistry": "0x...",
      "RoyaltyModule": "0x...",
      "LicenseRegistry": "0x..."
    }
  }
}
```

### Configuration Options

- **Enabled**: Whether Story Protocol integration is active
- **UseMockImplementation**: Use mock service (true) or real blockchain (false)
- **Network**: Blockchain network (mainnet, testnet, sepolia)
- **RpcUrl**: RPC endpoint for blockchain interactions
- **Contracts**: Smart contract addresses for Story Protocol
- **PrivateKey**: Transaction signing key (store in Azure Key Vault!)

## Usage

### Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
using Mystira.App.Infrastructure.StoryProtocol;

// Add Story Protocol services
services.AddStoryProtocolServices(configuration);
```

### Use in Application Layer

```csharp
public class RegisterScenarioIpAssetUseCase
{
    private readonly IStoryProtocolService _storyProtocolService;

    public RegisterScenarioIpAssetUseCase(IStoryProtocolService storyProtocolService)
    {
        _storyProtocolService = storyProtocolService;
    }

    public async Task<StoryProtocolMetadata> ExecuteAsync(...)
    {
        var metadata = await _storyProtocolService.RegisterIpAssetAsync(
            contentId,
            contentTitle,
            contributors,
            metadataUri,
            licenseTermsId
        );
        
        return metadata;
    }
}
```

## Future Implementation

When implementing the real blockchain integration:

1. Add Story Protocol TypeScript SDK or .NET wrapper package
2. Create `StoryProtocolService` class implementing `IStoryProtocolService`
3. Handle transaction signing with secure key management
4. Implement proper error handling for blockchain failures
5. Add transaction confirmation waiting logic
6. Update configuration to use real contract addresses
7. Test on testnet before mainnet deployment

### Security Considerations

- **Never commit private keys** - use Azure Key Vault
- **Use managed identities** for production deployments
- **Implement rate limiting** to prevent excessive gas costs
- **Add transaction monitoring** and alerting
- **Validate all inputs** before blockchain submission
- **Handle network failures** gracefully
- **Add transaction retry logic** with exponential backoff

## Related Documentation

- [Story Protocol Documentation](https://docs.story.foundation)
- [Story Protocol TypeScript SDK](https://docs.story.foundation/developers/typescript-sdk)
- [Royalty Module Overview](https://docs.story.foundation/concepts/royalty-module/overview)

## Development

### Building

```bash
dotnet build src/Mystira.App.Infrastructure.StoryProtocol/
```

### Testing

The mock implementation allows for testing without blockchain:

```bash
dotnet test tests/Mystira.App.Infrastructure.StoryProtocol.Tests/
```

## Contributing

When contributing blockchain integration code:

1. Follow clean architecture principles
2. Abstract blockchain details behind interfaces
3. Add comprehensive logging
4. Include unit and integration tests
5. Document gas cost implications
6. Consider testnet testing requirements
