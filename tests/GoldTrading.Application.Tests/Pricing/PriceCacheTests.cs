using GoldTrading.Application.Configuration;
using GoldTrading.Application.Tests.TestDoubles;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Infrastructure.Pricing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GoldTrading.Application.Tests.Pricing;

public sealed class PriceCacheTests
{
    private static IOptions<PricingOptions> OptionsWith(int cacheDurationMinutes = 5) =>
        Options.Create(new PricingOptions { CacheDurationMinutes = cacheDurationMinutes });

    private static GoldPrice TrustedPrice(decimal price = 20_000m, PriceSource source = PriceSource.PakGold) => new()
    {
        PricePerGram = price,
        Source       = source,
        FetchedAt    = DateTimeOffset.UtcNow,
        IsTrusted    = true
    };

    [Fact]
    public async Task GetAsync_PrimarySucceeds_ReturnsPrimaryPrice_AndNeverCallsFallback()
    {
        var primary  = new FakePriceProvider(() => TrustedPrice(20_000m, PriceSource.PakGold));
        var fallback = new FakePriceProvider(() => TrustedPrice(21_000m, PriceSource.GoldPriceOrg));
        var cache    = new PriceCache(primary, fallback, OptionsWith(), NullLogger<PriceCache>.Instance);

        var result = await cache.GetAsync();

        Assert.Equal(20_000m, result.PricePerGram);
        Assert.Equal(PriceSource.PakGold, result.Source);
        Assert.True(result.IsTrusted);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task GetAsync_PrimaryFails_FallsBackToSecondary()
    {
        var primary  = new FakePriceProvider(() => null);
        var fallback = new FakePriceProvider(() => TrustedPrice(21_000m, PriceSource.GoldPriceOrg));
        var cache    = new PriceCache(primary, fallback, OptionsWith(), NullLogger<PriceCache>.Instance);

        var result = await cache.GetAsync();

        Assert.Equal(21_000m, result.PricePerGram);
        Assert.Equal(PriceSource.GoldPriceOrg, result.Source);
        Assert.True(result.IsTrusted);
    }

    [Fact]
    public async Task GetAsync_BothFail_ReturnsUntrustedUnavailablePrice()
    {
        var primary  = new FakePriceProvider(() => null);
        var fallback = new FakePriceProvider(() => null);
        var cache    = new PriceCache(primary, fallback, OptionsWith(), NullLogger<PriceCache>.Instance);

        var result = await cache.GetAsync();

        Assert.False(result.IsTrusted);
        Assert.Equal(PriceSource.Unavailable, result.Source);
    }

    [Fact]
    public async Task GetAsync_WithinCacheWindow_ReturnsCachedValue_DoesNotRefetch()
    {
        var primary  = new FakePriceProvider(() => TrustedPrice());
        var fallback = new FakePriceProvider(() => null);
        var cache    = new PriceCache(primary, fallback, OptionsWith(cacheDurationMinutes: 5), NullLogger<PriceCache>.Instance);

        await cache.GetAsync();
        await cache.GetAsync();
        await cache.GetAsync();

        Assert.Equal(1, primary.CallCount);
    }

    [Fact]
    public async Task Invalidate_ForcesRefetchOnNextCall()
    {
        var primary  = new FakePriceProvider(() => TrustedPrice());
        var fallback = new FakePriceProvider(() => null);
        var cache    = new PriceCache(primary, fallback, OptionsWith(cacheDurationMinutes: 5), NullLogger<PriceCache>.Instance);

        await cache.GetAsync();
        cache.Invalidate();
        await cache.GetAsync();

        Assert.Equal(2, primary.CallCount);
    }
}
