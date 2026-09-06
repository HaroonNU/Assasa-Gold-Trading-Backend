using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Infrastructure.Repositories;

namespace GoldTrading.Application.Tests.Repositories;

public sealed class InMemoryTradeRepositoryTests
{
    private static Trade MakeTrade(Guid? quoteId = null) => new()
    {
        QuoteId           = quoteId ?? Guid.NewGuid(),
        TradeType         = TradeType.Buy,
        GoldAmountInGrams = 1m,
        PkrAmount         = 10_000m,
        PricePerGram      = 10_000m,
        PricingSource     = PriceSource.PakGold,
        BalanceSnapshot   = new BalanceSnapshot
        {
            CustomerPkrBalance         = 490_000m,
            CustomerGoldGrams          = 11m,
            PlatformGoldInventoryGrams = 999m
        }
    };

    [Fact]
    public void Save_And_GetById_RoundTrips()
    {
        var repo  = new InMemoryTradeRepository();
        var trade = MakeTrade();
        repo.Save(trade);

        Assert.Equal(trade.TradeId, repo.GetById(trade.TradeId)!.TradeId);
    }

    [Fact]
    public void GetByQuoteId_ReturnsCorrectTrade()
    {
        var repo    = new InMemoryTradeRepository();
        var quoteId = Guid.NewGuid();
        var trade   = MakeTrade(quoteId);
        repo.Save(trade);

        Assert.Equal(trade.TradeId, repo.GetByQuoteId(quoteId)!.TradeId);
    }

    [Fact]
    public void Save_DuplicateQuoteId_Throws()
    {
        var repo    = new InMemoryTradeRepository();
        var quoteId = Guid.NewGuid();
        repo.Save(MakeTrade(quoteId));

        Assert.Throws<InvalidOperationException>(() => repo.Save(MakeTrade(quoteId)));
    }

    [Fact]
    public void GetAll_ReturnsAllTrades_MostRecentFirst()
    {
        var repo = new InMemoryTradeRepository();
        repo.Save(MakeTrade());
        repo.Save(MakeTrade());
        repo.Save(MakeTrade());

        var all = repo.GetAll();
        Assert.Equal(3, all.Count);
        for (int i = 0; i < all.Count - 1; i++)
            Assert.True(all[i].CompletedAtUtc >= all[i + 1].CompletedAtUtc);
    }

    [Fact]
    public void GetByQuoteId_UnknownId_ReturnsNull()
    {
        var repo = new InMemoryTradeRepository();
        Assert.Null(repo.GetByQuoteId(Guid.NewGuid()));
    }
}
