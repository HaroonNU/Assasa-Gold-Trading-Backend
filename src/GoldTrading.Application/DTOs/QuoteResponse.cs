using GoldTrading.Domain.Enums;

namespace GoldTrading.Application.DTOs;

public sealed record QuoteResponse(
    Guid QuoteId,
    TradeType TradeType,
    decimal LockedMarketPrice,
    decimal CustomerPricePerGram,
    decimal PkrAmount,
    decimal GoldAmountInGrams,
    string PricingSource,
    DateTimeOffset PriceRetrievedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    int RemainingSeconds
);
