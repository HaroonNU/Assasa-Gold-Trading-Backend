namespace GoldTrading.Application.DTOs;

public sealed record AccountBalancesResponse(
    decimal CustomerPkrBalance,
    decimal CustomerGoldGrams,
    decimal PlatformGoldInventoryGrams
);
