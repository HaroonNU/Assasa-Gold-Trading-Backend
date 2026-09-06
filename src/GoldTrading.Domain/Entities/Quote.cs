using GoldTrading.Domain.Enums;

namespace GoldTrading.Domain.Entities;

public sealed class Quote
{
    public Guid QuoteId { get; init; } = Guid.NewGuid();
    public TradeType TradeType { get; init; }
    public decimal GoldAmountInGrams { get; init; }
    public decimal PkrAmount { get; init; }
    public decimal LockedMarketPrice { get; init; }
    public decimal CustomerPricePerGram { get; init; }
    public PriceSource PricingSource { get; init; }
    public DateTimeOffset PriceRetrievedAtUtc { get; init; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Pending;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
}
