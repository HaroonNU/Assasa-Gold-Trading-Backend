using System.ComponentModel.DataAnnotations;

namespace GoldTrading.Application.Configuration;

public sealed class TradingOptions
{
    public const string Section = "Trading";

    /// <summary>Maximum gold grams allowed in a single trade.</summary>
    [Range(0.001, 10000)]
    public decimal MaxGoldGramsPerTrade { get; init; } = 100m;

    /// <summary>Maximum PKR allowed in a single trade.</summary>
    [Range(1, double.MaxValue)]
    public decimal MaxPkrPerTrade { get; init; } = 10_000_000m;

    /// <summary>Minimum gold grams allowed in a single trade.</summary>
    [Range(0.000001, 1)]
    public decimal MinGoldGramsPerTrade { get; init; } = 0.001m;

    /// <summary>Minimum PKR allowed in a single trade.</summary>
    [Range(1, double.MaxValue)]
    public decimal MinPkrPerTrade { get; init; } = 100m;
}
