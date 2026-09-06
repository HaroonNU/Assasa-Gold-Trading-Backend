using GoldTrading.Domain.Enums;

namespace GoldTrading.Application.DTOs;

public sealed record TradeReceiptResponse(
    Guid TradeId,
    Guid QuoteId,
    TradeType TradeType,
    decimal PkrAmount,
    decimal GoldAmountInGrams,
    decimal PricePerGram,
    string PricingSource,
    DateTimeOffset CompletedAtUtc,
    decimal UpdatedCustomerPkrBalance,
    decimal UpdatedCustomerGoldBalance,
    decimal UpdatedPlatformInventory
);
