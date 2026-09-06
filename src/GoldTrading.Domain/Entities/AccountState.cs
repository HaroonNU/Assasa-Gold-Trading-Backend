namespace GoldTrading.Domain.Entities;

/// <summary>
/// The single customer account state held in memory for the lifetime of the process.
/// Tracks PKR cash, customer gold holdings, and platform gold inventory.
/// </summary>
public sealed class AccountState
{
    public decimal CustomerPkr { get; set; }
    public decimal CustomerGoldGrams { get; set; }
    public decimal PlatformGoldGrams { get; set; }
}
