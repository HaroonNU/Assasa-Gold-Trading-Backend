using GoldTrading.Domain.Enums;

namespace GoldTrading.Domain.Entities;

public sealed class GoldPrice
{
    public decimal PricePerGram { get; init; }
    public PriceSource Source { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
    public bool IsTrusted { get; init; }
}
