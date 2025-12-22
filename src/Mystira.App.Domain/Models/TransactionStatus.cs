namespace Mystira.App.Domain.Models;

/// <summary>
/// Status of a blockchain transaction
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Status is unknown or not yet determined
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Transaction has been submitted and is pending confirmation
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Transaction has been confirmed on the blockchain
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Transaction failed or was reverted
    /// </summary>
    Failed = 3
}
