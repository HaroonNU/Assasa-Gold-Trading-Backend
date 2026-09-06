namespace GoldTrading.Application.DTOs;

public sealed record CurrentPriceResponse(
    decimal PricePerGram,
    string Source,
    DateTimeOffset RetrievedAtUtc,
    bool IsTrusted
);
