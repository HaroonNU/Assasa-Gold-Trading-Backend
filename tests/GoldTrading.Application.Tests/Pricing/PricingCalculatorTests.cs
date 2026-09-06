using GoldTrading.Application.Configuration;
using GoldTrading.Domain.Enums;
using GoldTrading.Domain.Rules;

namespace GoldTrading.Application.Tests.Pricing;

public sealed class PricingCalculatorTests
{
    private static PricingOptions Rules(
        decimal markup = 1.015m, decimal markdown = 0.985m, decimal guardrail = 8500m) => new()
    {
        BuyMarkupMultiplier    = markup,
        SellMarkdownMultiplier = markdown,
        BuyPriceGuardrail      = guardrail
    };

    [Fact]
    public void Buy_AppliesMarkupMultiplier_WhenAboveGuardrail()
    {
        var price = PricingCalculator.CustomerPricePerGram(TradeDirection.Buy, 20_000m, Rules());
        Assert.Equal(20_300m, price);
    }

    [Fact]
    public void Buy_FallsBackToGuardrail_WhenMarketPriceVeryLow()
    {
        var price = PricingCalculator.CustomerPricePerGram(TradeDirection.Buy, 1m, Rules(guardrail: 8500m));
        Assert.Equal(8500m, price);
    }

    [Fact]
    public void Buy_UsesGuardrail_WhenExactlyAtBoundary()
    {
        // 8500 / 1.015 is the market price where markup == guardrail exactly.
        var boundaryMarketPrice = 8500m / 1.015m;
        var price = PricingCalculator.CustomerPricePerGram(TradeDirection.Buy, boundaryMarketPrice, Rules());
        Assert.Equal(8500m, price);
    }

    [Fact]
    public void Sell_AppliesMarkdownMultiplier()
    {
        var price = PricingCalculator.CustomerPricePerGram(TradeDirection.Sell, 20_000m, Rules());
        Assert.Equal(19_700m, price);
    }

    [Fact]
    public void Sell_NeverAppliesGuardrail()
    {
        // Guardrail only protects the platform on Buy — Sell has no floor.
        var price = PricingCalculator.CustomerPricePerGram(TradeDirection.Sell, 100m, Rules(guardrail: 8500m));
        Assert.Equal(98.5m, price);
    }
}
