using GoldTrading.Domain.Enums;

namespace GoldTrading.Domain.Rules;

/// <summary>
/// Pure pricing formula. Depends on an interface so Domain has zero
/// references to Application or Infrastructure configuration types.
/// </summary>
public static class PricingCalculator
{
    public static decimal CustomerPricePerGram(
        TradeDirection direction,
        decimal marketPrice,
        IPricingRules rules) =>
        direction == TradeDirection.Buy
            ? Math.Max(marketPrice * rules.BuyMarkupMultiplier, rules.BuyPriceGuardrail)
            : marketPrice * rules.SellMarkdownMultiplier;
}
