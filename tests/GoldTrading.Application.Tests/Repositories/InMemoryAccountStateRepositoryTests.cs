using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Infrastructure.Repositories;

namespace GoldTrading.Application.Tests.Repositories;

public sealed class InMemoryAccountStateRepositoryTests
{
    private static InMemoryAccountStateRepository CreateRepo(
        decimal pkr = 500_000m, decimal gold = 10m, decimal platform = 1000m) =>
        new(new AccountState
        {
            CustomerPkr       = pkr,
            CustomerGoldGrams = gold,
            PlatformGoldGrams = platform
        });

    [Fact]
    public void GetSnapshot_ReturnsCorrectSeedValues()
    {
        var repo = CreateRepo(pkr: 100_000m, gold: 5m, platform: 200m);
        var snap = repo.GetSnapshot();

        Assert.Equal(100_000m, snap.CustomerPkr);
        Assert.Equal(5m,       snap.CustomerGoldGrams);
        Assert.Equal(200m,     snap.PlatformGoldGrams);
    }

    [Fact]
    public void GetSnapshot_ReturnsValueCopy_NotReference()
    {
        var repo = CreateRepo();
        var snap = repo.GetSnapshot();
        snap.CustomerPkr = 0m;

        Assert.Equal(500_000m, repo.GetSnapshot().CustomerPkr);
    }

    [Fact]
    public void ApplyTrade_Buy_DebitsCustomerPkrAndCreditsGold()
    {
        var repo  = CreateRepo(pkr: 500_000m, gold: 10m, platform: 1000m);
        var after = repo.ApplyTrade(TradeDirection.Buy, grams: 2m, pkr: 20_000m);

        Assert.Equal(480_000m, after.CustomerPkr);
        Assert.Equal(12m,      after.CustomerGoldGrams);
        Assert.Equal(998m,     after.PlatformGoldGrams);
    }

    [Fact]
    public void ApplyTrade_Sell_CreditsCustomerPkrAndDebitsGold()
    {
        var repo  = CreateRepo(pkr: 500_000m, gold: 10m, platform: 1000m);
        var after = repo.ApplyTrade(TradeDirection.Sell, grams: 3m, pkr: 30_000m);

        Assert.Equal(530_000m, after.CustomerPkr);
        Assert.Equal(7m,       after.CustomerGoldGrams);
        Assert.Equal(1003m,    after.PlatformGoldGrams);
    }

    [Fact]
    public async Task ApplyTrade_ConcurrentBuys_BalancesRemainConsistent()
    {
        var repo  = CreateRepo(pkr: 1_000_000m, gold: 0m, platform: 1000m);
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
                repo.ApplyTrade(TradeDirection.Buy, grams: 1m, pkr: 10_000m)));

        await Task.WhenAll(tasks);

        var final = repo.GetSnapshot();
        Assert.Equal(900_000m, final.CustomerPkr);
        Assert.Equal(10m,      final.CustomerGoldGrams);
        Assert.Equal(990m,     final.PlatformGoldGrams);
    }
}
