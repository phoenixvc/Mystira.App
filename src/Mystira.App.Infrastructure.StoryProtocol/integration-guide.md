# Story Protocol Integration Guide

## Status: Framework Ready ✅

The Story Protocol integration framework is now in place with Nethereum blockchain support. This guide explains how to complete the integration.

## What's Already Done ✅

1. ✅ **Nethereum packages added** (v4.22.0)
   - Nethereum.Web3 - Ethereum blockchain interaction
   - Nethereum.Accounts - Account/wallet management

2. ✅ **StoryProtocolClient created** with methods for:
   - `RegisterIpAssetAsync()` - Register content as IP Asset
   - `PayRoyaltyAsync()` - Pay royalties to IP Assets
   - `SetRoyaltySplitsAsync()` - Configure multi-contributor revenue splits

3. ✅ **Azure Key Vault integration** for secure private key storage

4. ✅ **Configuration infrastructure** ready (StoryProtocolOptions)

## What Needs to be Completed 🚧

### 1. Get Story Protocol Contract Information

**Required Information:**
- Contract addresses for target network (testnet/mainnet)
- Contract ABIs (Application Binary Interfaces)

**Where to find:**
- Documentation: https://docs.story.foundation/
- Contract deployments: Check Story Protocol GitHub or docs for deployed addresses
- ABIs: https://github.com/storyprotocol/protocol-core

**Update these constants in `StoryProtocolClient.cs`:**
```csharp
private const string IP_ASSET_REGISTRY_ADDRESS = "0x..."; // Get from docs
private const string ROYALTY_MODULE_ADDRESS = "0x...";     // Get from docs
private const string LICENSING_MODULE_ADDRESS = "0x...";   // Get from docs
```

### 2. Add Contract ABIs

The ABI (Application Binary Interface) defines how to interact with smart contracts.

**Steps:**
1. Get ABI JSON from Story Protocol deployment or compile from source
2. Update methods in `StoryProtocolClient.cs`:
   - `GetIPAssetRegistryABI()` - Returns IP Asset Registry contract ABI
   - `GetRoyaltyModuleABI()` - Returns Royalty Module contract ABI

**Example ABI structure:**
```csharp
private string GetIPAssetRegistryABI()
{
    return @"[
        {
            ""inputs"": [
                {""name"": ""nftContract"", ""type"": ""address""},
                {""name"": ""tokenId"", ""type"": ""uint256""},
                {""name"": ""metadataURI"", ""type"": ""string""}
            ],
            ""name"": ""register"",
            ""outputs"": [{""name"": ""ipAssetId"", ""type"": ""address""}],
            ""stateMutability"": ""nonpayable"",
            ""type"": ""function""
        }
    ]";
}
```

### 3. Implement Event Log Parsing

Extract IP Asset ID from blockchain transaction logs:

```csharp
private string ExtractIpAssetIdFromLogs(Nethereum.RPC.Eth.DTOs.Log[] logs)
{
    // Story Protocol emits IPAssetRegistered event
    // Parse the event logs to extract the IP Asset ID

    foreach (var log in logs)
    {
        // Check if this is the IPAssetRegistered event
        // Event signature hash should match Story Protocol's event
        if (log.Topics[0] == "0x...") // IPAssetRegistered event signature
        {
            // Decode the event data
            // Extract ipAssetId from topics or data
            return log.Topics[1]; // Example - actual structure depends on contract
        }
    }

    throw new Exception("IP Asset ID not found in transaction logs");
}
```

### 4. NFT Integration

Story Protocol requires an NFT to exist before registering it as an IP Asset.

**Options:**

**Option A: Mint NFT for each scenario**
```csharp
// Create ERC-721 NFT contract instance
var nftContract = _web3.Eth.GetContract(ERC721_ABI, NFT_CONTRACT_ADDRESS);
var mintFunction = nftContract.GetFunction("mint");

// Mint NFT for scenario
var tokenId = await mintFunction.CallAsync<BigInteger>(
    ownerAddress,
    metadataUri);

// Then register with Story Protocol
var (ipAssetId, txHash) = await client.RegisterIpAssetAsync(
    NFT_CONTRACT_ADDRESS,
    tokenId,
    metadataUri);
```

**Option B: Use existing NFT platform**
- Deploy ERC-721 contract for Mystira content
- Mint NFTs via API/admin interface
- Store NFT details in database
- Reference when registering with Story Protocol

### 5. Configuration Setup

**appsettings.json:**
```json
{
  "StoryProtocol": {
    "Enabled": true,
    "UseMockImplementation": false,  // Set to false for production
    "Network": "testnet",             // or "mainnet"
    "RpcUrl": "https://rpc.testnet.story.foundation",
    "KeyVaultName": "mystira-keyvault",
    "Contracts": {
      "IpAssetRegistry": "0x...",     // From Story Protocol docs
      "RoyaltyModule": "0x...",        // From Story Protocol docs
      "LicenseRegistry": "0x..."       // From Story Protocol docs
    }
  }
}
```

**Azure Key Vault:**
```bash
# Store private key securely
az keyvault secret set \
  --vault-name mystira-keyvault \
  --name "StoryProtocol--PrivateKey" \
  --value "0x..."  # NEVER commit private keys!
```

### 6. Testing Strategy

**Testnet Testing:**
1. Get testnet tokens from faucet
2. Deploy or use testnet NFT contract
3. Mint test NFT
4. Register with Story Protocol testnet
5. Test royalty payments
6. Verify on testnet explorer

**Integration Tests:**
```csharp
[Fact]
public async Task RegisterIpAsset_WithValidNFT_ReturnsIpAssetId()
{
    // Arrange
    var service = new StoryProtocolService(logger, options);

    // Act
    var metadata = await service.RegisterIpAssetAsync(
        contentId: "test-scenario-123",
        contentTitle: "Test Scenario",
        contributors: new List<Contributor>
        {
            new() { Name = "Author", WalletAddress = "0x...", RoyaltyShare = 100 }
        });

    // Assert
    Assert.NotNull(metadata.IpAssetId);
    Assert.NotNull(metadata.RegistrationTxHash);
}
```

## Implementation Checklist

- [ ] Get Story Protocol contract addresses for target network
- [ ] Get contract ABIs from Story Protocol
- [ ] Update contract address constants in StoryProtocolClient.cs
- [ ] Implement GetIPAssetRegistryABI() method
- [ ] Implement GetRoyaltyModuleABI() method
- [ ] Implement ExtractIpAssetIdFromLogs() event parsing
- [ ] Deploy or integrate ERC-721 NFT contract
- [ ] Implement NFT minting in RegisterIpAssetAsync()
- [ ] Configure Azure Key Vault with wallet private key
- [ ] Update appsettings.json with contract addresses
- [ ] Test on Story Protocol testnet
- [ ] Create integration tests
- [ ] Document deployment process
- [ ] Security audit of private key handling

## Example: Complete Flow

```csharp
// 1. Content creator publishes scenario
var scenario = new Scenario { Title = "Dragon's Quest", ... };
await _scenarioRepository.AddAsync(scenario);

// 2. Register as IP Asset on Story Protocol
var contributors = new List<Contributor>
{
    new() { Name = "Writer", WalletAddress = "0xABC...", RoyaltyShare = 60 },
    new() { Name = "Artist", WalletAddress = "0xDEF...", RoyaltyShare = 30 },
    new() { Name = "Composer", WalletAddress = "0x123...", RoyaltyShare = 10 }
};

var storyMetadata = await _storyProtocolService.RegisterIpAssetAsync(
    contentId: scenario.Id,
    contentTitle: scenario.Title,
    contributors: contributors,
    metadataUri: $"https://mystira.app/api/scenarios/{scenario.Id}/metadata");

scenario.StoryProtocol = storyMetadata;
await _unitOfWork.SaveChangesAsync();

// 3. When revenue is earned (e.g., scenario purchase)
await _storyProtocolService.PayRoyaltyAsync(
    ipAssetId: storyMetadata.IpAssetId,
    amount: purchasePrice);

// Story Protocol automatically distributes:
// - 60% to Writer (0xABC...)
// - 30% to Artist (0xDEF...)
// - 10% to Composer (0x123...)
```

## Resources

- **Story Protocol Docs**: https://docs.story.foundation/
- **Royalty Module**: https://docs.story.foundation/concepts/royalty-module/overview
- **TypeScript SDK (reference)**: https://docs.story.foundation/developers/typescript-sdk/
- **Contract Source**: https://github.com/storyprotocol/protocol-core
- **Nethereum Docs**: https://docs.nethereum.com/
- **Discord Community**: Check Story Protocol Discord for developer support

## Support

For questions or issues:
1. Check Story Protocol documentation
2. Review Nethereum examples
3. Test on testnet first
4. Join Story Protocol Discord for community support
