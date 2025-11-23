# Mystira.App.Infrastructure.StoryProtocol

Infrastructure layer implementation for Story Protocol blockchain integration. This project handles royalty splits and IP asset registration for content creators.

## Overview

Story Protocol is a blockchain-based IP management system that enables:
- Registration of content as IP assets on-chain
- Automatic royalty distribution among contributors
- Transparent revenue sharing based on contribution percentages
- Immutable record of IP ownership and licenses

## Implementations

This project provides two implementations:

### 1. MockStoryProtocolService (Default)
- For development and testing
- Simulates blockchain interactions without actual transactions
- Stores registrations in-memory (thread-safe with ConcurrentDictionary)
- No blockchain or wallet required

### 2. StoryProtocolService (Production Ready)
- Real blockchain integration with Azure Key Vault
- Securely manages private keys
- Ready for Story Protocol SDK integration
- Requires blockchain wallet and gas fees

## Configuration

Add to your `appsettings.json`:

```json
{
  "StoryProtocol": {
    "Enabled": true,
    "UseMockImplementation": true,
    "Network": "testnet",
    "RpcUrl": "https://rpc.testnet.story.foundation",
    "KeyVaultName": "mystirakeyvault",
    "Contracts": {
      "IpAssetRegistry": "0x...",
      "RoyaltyModule": "0x...",
      "LicenseRegistry": "0x..."
    }
  }
}
```

### Configuration Options

- **Enabled**: Whether Story Protocol integration is active (default: false)
- **UseMockImplementation**: Use mock (true) or real blockchain (false)
- **Network**: Blockchain network (mainnet, testnet, sepolia)
- **RpcUrl**: RPC endpoint for blockchain interactions
- **KeyVaultName**: Azure Key Vault name containing private key
- **Contracts**: Smart contract addresses for Story Protocol
- **PrivateKey**: Transaction signing key (fallback if Key Vault unavailable) - NOT RECOMMENDED for production

## Azure Key Vault Setup

For production, store the private key in Azure Key Vault:

### 1. Create Key Vault (via Bicep templates)

Use the provided Bicep templates in `Mystira.App.Infrastructure.Azure/Deployment/`:

```bash
az deployment group create \
  --resource-group mystira-app-prod-rg \
  --template-file src/Mystira.App.Infrastructure.Azure/Deployment/prod/main.bicep \
  --parameters keyVaultAdminObjectId=$(az ad signed-in-user show --query id -o tsv)
```

### 2. Store Private Key

```bash
az keyvault secret set \
  --vault-name your-keyvault-name \
  --name "StoryProtocol--PrivateKey" \
  --value "YOUR_PRIVATE_KEY_HERE"
```

### 3. Grant App Service Access

```bash
# Enable managed identity
az webapp identity assign --name your-app-name --resource-group your-rg

# Grant Key Vault access
PRINCIPAL_ID=$(az webapp identity show --name your-app-name --resource-group your-rg --query principalId -o tsv)
az keyvault set-policy \
  --name your-keyvault-name \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

## Usage

### Register Services

In your `Program.cs`:

```csharp
using Mystira.App.Infrastructure.StoryProtocol;

// Add Story Protocol services
services.AddStoryProtocolServices(configuration);
```

The service will automatically select:
- **MockStoryProtocolService** when `UseMockImplementation=true`
- **StoryProtocolService** when `UseMockImplementation=false` and `Enabled=true`

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

## Implementing Blockchain Integration

The `StoryProtocolService` is set up with Azure Key Vault integration but needs the actual Story Protocol SDK implementation.

### Next Steps

1. **Install Story Protocol SDK**
   - Install Nethereum for Ethereum interactions: `dotnet add package Nethereum.Web3`
   - Or implement TypeScript SDK wrapper via REST API

2. **Implement RegisterIpAssetAsync**
   - Connect to Ethereum network using RPC URL
   - Create and sign transaction for IP registration
   - Submit to Story Protocol contracts
   - Wait for confirmation and parse receipt

3. **Implement Other Methods**
   - `IsRegisteredAsync`: Query contract state
   - `GetRoyaltyConfigurationAsync`: Read royalty module
   - `UpdateRoyaltySplitAsync`: Update contributor splits

### Example Implementation (Nethereum)

```csharp
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;

public async Task<StoryProtocolMetadata> RegisterIpAssetAsync(...)
{
    var privateKey = await GetPrivateKeyAsync();
    var account = new Account(privateKey);
    var web3 = new Web3(account, _options.RpcUrl);
    
    // Load contract ABI and address
    var contract = web3.Eth.GetContract(contractAbi, _options.Contracts.IpAssetRegistry);
    
    // Prepare transaction
    var registerFunction = contract.GetFunction("registerIpAsset");
    
    // Prepare royalty splits
    var addresses = contributors.Select(c => c.WalletAddress).ToArray();
    var percentages = contributors.Select(c => (int)(c.ContributionPercentage * 100)).ToArray();
    
    // Send transaction
    var txHash = await registerFunction.SendTransactionAsync(
        account.Address,
        contentId,
        metadataUri,
        addresses,
        percentages
    );
    
    // Wait for confirmation
    var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
    
    // Parse receipt for IP Asset ID
    var ipAssetId = ParseIpAssetIdFromReceipt(receipt);
    
    return new StoryProtocolMetadata
    {
        IpAssetId = ipAssetId,
        RegistrationTxHash = txHash,
        RegisteredAt = DateTime.UtcNow,
        RoyaltyModuleId = _options.Contracts.RoyaltyModule,
        Contributors = contributors
    };
}
```

## Testing

### Unit Tests

Both implementations have unit tests:
- `ContributorTests`: 6 tests for contributor validation
- `StoryProtocolMetadataTests`: 8 tests for royalty split validation

### Integration Testing

```bash
# Test with mock implementation (no blockchain required)
dotnet test

# Test Key Vault access (requires Azure setup)
az webapp log tail --name your-app-name --resource-group your-rg
```

## Security Considerations

### Private Key Management
- ✅ Use Azure Key Vault for production
- ✅ Enable managed identity
- ✅ Never commit keys to source control
- ✅ Rotate keys regularly
- ✅ Use soft delete and purge protection

### Blockchain Security
- Implement transaction retry logic with exponential backoff
- Add gas price estimation and limits
- Monitor for failed transactions
- Implement rate limiting
- Add transaction confirmation waiting

### Access Control
- Key Vault access via managed identity only
- Principle of least privilege for service accounts
- Audit Key Vault access logs
- Enable Key Vault firewall for production

## Monitoring & Observability

### Logging
All operations are logged with structured logging:
- Info: Successful operations
- Warning: Fallback to configuration, Key Vault issues
- Error: Failed blockchain transactions

### Metrics to Monitor
- Key Vault access attempts
- Blockchain transaction success/failure rate
- Gas costs per operation
- Transaction confirmation times
- Failed authentication attempts

### Alerts to Configure
- Failed Key Vault access
- High gas costs
- Transaction failures exceeding threshold
- Unusual transaction patterns

## Troubleshooting

### "Key Vault not configured"
- Ensure `KeyVaultName` is set in configuration
- Verify managed identity is enabled on App Service
- Check Key Vault access policies

### "Private key not configured"
- Verify secret exists in Key Vault: `StoryProtocol--PrivateKey`
- Check secret is not placeholder value
- Ensure managed identity has `get` and `list` permissions

### "NotImplementedException"
- This is expected for blockchain methods
- Implement Story Protocol SDK integration (see above)
- Or continue using `MockStoryProtocolService` for development

## Cost Considerations

### Azure Costs
- Key Vault Standard: ~$0.03 per 10,000 operations
- Key Vault Premium: ~$1.00/month + operation costs
- App Service managed identity: Free

### Blockchain Costs
- Gas fees per transaction (varies by network)
- Testnet: Free (use testnet tokens)
- Mainnet: Real ETH required for gas

## Related Documentation

- [Story Protocol Documentation](https://docs.story.foundation)
- [Story Protocol TypeScript SDK](https://docs.story.foundation/developers/typescript-sdk)
- [Royalty Module Overview](https://docs.story.foundation/concepts/royalty-module/overview)
- [Azure Key Vault Documentation](https://learn.microsoft.com/azure/key-vault/)
- [Deployment Guide](../Mystira.App.Infrastructure.Azure/Deployment/DEPLOYMENT_GUIDE.md)
