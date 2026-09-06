namespace GoldTrading.Domain.Rules;

/// <summary>
/// Pricing rule contract consumed by PricingCalculator.
/// Implemented by PricingOptions in Application so Domain stays dependency-free.
/// Property names match PricingOptions exactly — no mapping layer needed.
/// </summary>
public interface IPricingRules
{
    decimal BuyMarkupMultiplier { get; }
    decimal SellMarkdownMultiplier { get; }
    decimal BuyPriceGuardrail { get; }
}
