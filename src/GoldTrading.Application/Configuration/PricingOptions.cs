using System.ComponentModel.DataAnnotations;
using GoldTrading.Domain.Rules;

namespace GoldTrading.Application.Configuration;

/// <summary>
/// All live-price and markup/markdown business values.
/// Bound from the "Pricing" section of appsettings.json.
///
/// Implements <see cref="IPricingRules"/> so the Domain's PricingCalculator
/// can consume this directly without any mapping layer.
///
/// Access pattern: inject <c>IOptions&lt;PricingOptions&gt;</c> and read
/// <c>.Value</c> once in the constructor.  PricingOptions is not expected
/// to change at runtime, so IOptions (snapshot) is correct here.
/// </summary>
public sealed class PricingOptions : IPricingRules
{
    public const string Section = "Pricing";

    /// <summary>
    /// How long a fetched gold price is considered fresh before the cache
    /// triggers a new external fetch. The assessment brief requires fetching
    /// no more than once every five minutes, so this must be at least 5.
    /// </summary>
    [Range(5, 1440, ErrorMessage = "CacheDurationMinutes must be between 5 and 1440.")]
    public int CacheDurationMinutes { get; init; } = 5;

    /// <summary>
    /// Multiplier applied to the market price to derive the customer buy price.
    /// Per the brief: customer buys at max(market × 1.10, guardrail).
    /// Must be strictly greater than 1 — a multiplier of 1 or below means
    /// the platform sells at or below cost.
    /// </summary>
    [Range(1.0001, 2.0, ErrorMessage = "BuyMarkupMultiplier must be between 1.0001 and 2.0.")]
    public decimal BuyMarkupMultiplier { get; init; } = 1.10m;

    /// <summary>
    /// Multiplier applied to the market price to derive the customer sell price.
    /// Per the brief: customer sells at market × 0.90.
    /// Must be strictly less than 1 — a multiplier of 1 or above means the
    /// platform buys at or above market, which is not a valid spread.
    /// </summary>
    [Range(0.0001, 0.9999, ErrorMessage = "SellMarkdownMultiplier must be between 0.0001 and 0.9999.")]
    public decimal SellMarkdownMultiplier { get; init; } = 0.90m;

    /// <summary>
    /// Minimum floor price (PKR/gram) applied to buy quotes regardless of
    /// the market price.  Protects the platform from selling below cost
    /// during extreme market dips.  Must be greater than zero.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "BuyPriceGuardrail must be greater than zero.")]
    public decimal BuyPriceGuardrail { get; init; } = 8500m;
}
