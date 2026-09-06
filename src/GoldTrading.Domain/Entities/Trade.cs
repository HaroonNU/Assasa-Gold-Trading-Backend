using GoldTrading.Domain.Enums;

namespace GoldTrading.Domain.Entities;

public sealed class BalanceSnapshot
{
    public decimal CustomerPkrBalance { get; init; }
    public decimal CustomerGoldGrams { get; init; }
    public decimal PlatformGoldInventoryGrams { get; init; }
}

public sealed class Trade
{
    public Guid TradeId { get; init; } = Guid.NewGuid();
    public Guid QuoteId { get; init; }
    public TradeType TradeType { get; init; }
    public decimal GoldAmountInGrams { get; init; }
    public decimal PkrAmount { get; init; }
    public decimal PricePerGram { get; init; }
    public PriceSource PricingSource { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public BalanceSnapshot BalanceSnapshot { get; init; } = default!;
}
