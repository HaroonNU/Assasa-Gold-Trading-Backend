using System.ComponentModel.DataAnnotations;

namespace GoldTrading.Application.Configuration;

public sealed class SeedDataOptions
{
    public const string Section = "SeedData";

    [Range(0, double.MaxValue)]
    public decimal CustomerPkr { get; init; } = 500_000m;

    [Range(0, double.MaxValue)]
    public decimal CustomerGoldGrams { get; init; } = 10m;

    [Range(0, double.MaxValue)]
    public decimal PlatformGoldGrams { get; init; } = 1000m;
}
