using GoldTrading.Domain.Enums;

namespace GoldTrading.Application.DTOs;

public sealed record CreateQuoteRequest(
    TradeType TradeType,
    InputType InputType,
    decimal Amount
);
