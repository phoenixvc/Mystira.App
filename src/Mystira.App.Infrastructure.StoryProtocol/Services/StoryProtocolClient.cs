using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Mystira.App.Infrastructure.StoryProtocol.Configuration;

namespace Mystira.App.Infrastructure.StoryProtocol.Services;

/// <summary>
/// Client for interacting with Story Protocol smart contracts via Nethereum
///
/// Story Protocol Documentation:
/// - Royalty Module: https://docs.story.foundation/concepts/royalty-module/overview
/// - TypeScript SDK: https://docs.story.foundation/developers/typescript-sdk/pay-ipa
/// - Contract Addresses: Check Story Protocol testnet/mainnet deployment docs
/// </summary>
public class StoryProtocolClient
{
    private readonly ILogger<StoryProtocolClient> _logger;
    private readonly Web3 _web3;
    private readonly StoryProtocolOptions _options;

    // TODO: Get actual contract addresses from Story Protocol deployment
    // These are placeholders - replace with actual deployed contract addresses
    private const string IP_ASSET_REGISTRY_ADDRESS = "0x0000000000000000000000000000000000000000";
    private const string ROYALTY_MODULE_ADDRESS = "0x0000000000000000000000000000000000000000";
    private const string LICENSING_MODULE_ADDRESS = "0x0000000000000000000000000000000000000000";

    public StoryProtocolClient(
        string privateKey,
        StoryProtocolOptions options,
        ILogger<StoryProtocolClient> logger)
    {
        _logger = logger;
        _options = options;

        // Create account from private key
        var account = new Account(privateKey);

        // Initialize Web3 with RPC endpoint
        _web3 = new Web3(account, options.RpcUrl);

        _logger.LogInformation(
            "Story Protocol client initialized for network: {Network}, RPC: {RpcUrl}",
            options.Network, options.RpcUrl);
    }

    /// <summary>
    /// Register an IP Asset on Story Protocol
    /// Based on: https://docs.story.foundation/developers/typescript-sdk/register-ip-asset
    /// </summary>
    public async Task<(string ipAssetId, string txHash)> RegisterIpAssetAsync(
        string nftContractAddress,
        BigInteger tokenId,
        string metadataUri)
    {
        _logger.LogInformation(
            "Registering IP Asset - NFT: {Contract}, Token: {TokenId}",
            nftContractAddress, tokenId);

        try
        {
            // Story Protocol requires an NFT to be minted first, then registered as an IP Asset
            // The TypeScript SDK equivalent:
            // const response = await client.ipAsset.register({
            //   nftContract: nftContractAddress,
            //   tokenId: tokenId,
            //   metadata: { metadataURI: metadataUri }
            // });

            // TODO: Replace with actual Story Protocol contract ABI
            // For now, showing the pattern of how to call a smart contract function
            var contract = _web3.Eth.GetContract(GetIPAssetRegistryABI(), IP_ASSET_REGISTRY_ADDRESS);

            // Example function call (adjust based on actual Story Protocol ABI)
            var registerFunction = contract.GetFunction("register");

            // Estimate gas
            var gas = await registerFunction.EstimateGasAsync(
                _web3.TransactionManager.Account.Address,
                new HexBigInteger(3000000), // Max gas
                null,
                nftContractAddress,
                tokenId,
                metadataUri);

            // Send transaction
            var txHash = await registerFunction.SendTransactionAsync(
                _web3.TransactionManager.Account.Address,
                gas,
                null, // Gas price (null = auto)
                null, // Value
                nftContractAddress,
                tokenId,
                metadataUri);

            _logger.LogInformation("IP Asset registration transaction sent: {TxHash}", txHash);

            // Wait for transaction receipt
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            while (receipt == null)
            {
                await Task.Delay(2000);
                receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            }

            // TODO: Parse logs to extract IP Asset ID
            // Story Protocol emits events with the registered IP Asset ID
            var ipAssetId = ExtractIpAssetIdFromLogs(receipt.Logs);

            _logger.LogInformation(
                "IP Asset registered successfully - ID: {IpAssetId}, TxHash: {TxHash}",
                ipAssetId, txHash);

            return (ipAssetId, txHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering IP Asset");
            throw;
        }
    }

    /// <summary>
    /// Pay royalties to an IP Asset
    /// Based on: https://docs.story.foundation/developers/typescript-sdk/pay-ipa
    /// </summary>
    public async Task<string> PayRoyaltyAsync(
        string receiverIpAssetId,
        string payerIpAssetId, // Use "0x0000..." for external payments
        string tokenAddress, // WIP token address
        BigInteger amount)
    {
        _logger.LogInformation(
            "Paying royalty - Receiver: {Receiver}, Payer: {Payer}, Amount: {Amount}",
            receiverIpAssetId, payerIpAssetId, amount);

        try
        {
            // TypeScript SDK equivalent:
            // await client.royalty.payRoyaltyOnBehalf({
            //   receiverIpId: receiverIpAssetId,
            //   payerIpId: payerIpAssetId,
            //   token: tokenAddress,
            //   amount: amount
            // });

            var contract = _web3.Eth.GetContract(GetRoyaltyModuleABI(), ROYALTY_MODULE_ADDRESS);
            var payFunction = contract.GetFunction("payRoyaltyOnBehalf");

            var gas = await payFunction.EstimateGasAsync(
                _web3.TransactionManager.Account.Address,
                new HexBigInteger(300000),
                null,
                receiverIpAssetId,
                payerIpAssetId,
                tokenAddress,
                amount);

            var txHash = await payFunction.SendTransactionAsync(
                _web3.TransactionManager.Account.Address,
                gas,
                null,
                null,
                receiverIpAssetId,
                payerIpAssetId,
                tokenAddress,
                amount);

            _logger.LogInformation("Royalty payment transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying royalty");
            throw;
        }
    }

    /// <summary>
    /// Set royalty splits for an IP Asset
    /// Distributes revenue among multiple contributors
    /// </summary>
    public async Task<string> SetRoyaltySplitsAsync(
        string ipAssetId,
        string[] contributorAddresses,
        uint[] percentages) // Percentages in basis points (10000 = 100%)
    {
        _logger.LogInformation(
            "Setting royalty splits for IP Asset: {IpAssetId} with {Count} contributors",
            ipAssetId, contributorAddresses.Length);

        // Validate percentages sum to 10000 (100%)
        var totalPercentage = 0u;
        foreach (var pct in percentages)
        {
            totalPercentage += pct;
        }

        if (totalPercentage != 10000)
        {
            throw new ArgumentException($"Royalty percentages must sum to 100% (10000 basis points). Got: {totalPercentage}");
        }

        try
        {
            var contract = _web3.Eth.GetContract(GetRoyaltyModuleABI(), ROYALTY_MODULE_ADDRESS);
            var setSplitsFunction = contract.GetFunction("setRoyaltySplits");

            var txHash = await setSplitsFunction.SendTransactionAsync(
                _web3.TransactionManager.Account.Address,
                ipAssetId,
                contributorAddresses,
                percentages);

            _logger.LogInformation("Royalty splits transaction sent: {TxHash}", txHash);
            return txHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting royalty splits");
            throw;
        }
    }

    // ABI methods - these should be replaced with actual Story Protocol contract ABIs

    private string GetIPAssetRegistryABI()
    {
        // TODO: Get actual ABI from Story Protocol deployment
        // Example: https://github.com/storyprotocol/protocol-core/blob/main/contracts/IPAssetRegistry.sol
        return @"[]"; // Placeholder
    }

    private string GetRoyaltyModuleABI()
    {
        // TODO: Get actual ABI from Story Protocol deployment
        // Example: https://github.com/storyprotocol/protocol-core/blob/main/contracts/modules/royalty/RoyaltyModule.sol
        return @"[]"; // Placeholder
    }

    private string ExtractIpAssetIdFromLogs(Nethereum.RPC.Eth.DTOs.Log[] logs)
    {
        // TODO: Parse event logs to extract IP Asset ID
        // Story Protocol emits IPAssetRegistered event with the ID
        // For now, generate a placeholder ID
        return $"ipa_{Guid.NewGuid():N}";
    }
}
