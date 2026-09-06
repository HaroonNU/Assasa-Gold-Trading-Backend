using GoldTrading.Application.Exceptions;
using GoldTrading.Application.Services;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldTrading.Application.Tests.Services;

public sealed class TradeSettlementServiceTests
{
    private sealed record Harness(
        TradeSettlementService Service,
        InMemoryAccountStateRepository AccountRepo,
        InMemoryQuoteRepository QuoteRepo,
        InMemoryTradeRepository TradeRepo);

    private static Harness CreateHarness(decimal pkr = 500_000m, decimal gold = 10m, decimal platform = 1000m)
    {
        var accountRepo = new InMemoryAccountStateRepository(new AccountState
        {
            CustomerPkr       = pkr,
            CustomerGoldGrams = gold,
            PlatformGoldGrams = platform
        });
        var quoteRepo = new InMemoryQuoteRepository();
        var tradeRepo = new InMemoryTradeRepository();
        var settlementLock = new TradeSettlementLock();
        var service = new TradeSettlementService(
            settlementLock, quoteRepo, tradeRepo, accountRepo, NullLogger<TradeSettlementService>.Instance);

        return new Harness(service, accountRepo, quoteRepo, tradeRepo);
    }

    private static Quote MakeQuote(TradeType type, decimal grams, decimal pkr, int expiresInSeconds = 75) => new()
    {
        TradeType            = type,
        GoldAmountInGrams    = grams,
        PkrAmount            = pkr,
        LockedMarketPrice    = 20_000m,
        CustomerPricePerGram = pkr / grams,
        PricingSource        = PriceSource.PakGold,
        PriceRetrievedAtUtc  = DateTimeOffset.UtcNow,
        CreatedAtUtc         = DateTimeOffset.UtcNow,
        ExpiresAtUtc         = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
    };

    [Fact]
    public async Task ConfirmAsync_SuccessfulBuy_DebitsPkrCreditsGoldDebitsInventory()
    {
        var h = CreateHarness(pkr: 500_000m, gold: 10m, platform: 1000m);
        var quote = MakeQuote(TradeType.Buy, grams: 2m, pkr: 40_000m);
        h.QuoteRepo.Save(quote);

        var receipt = await h.Service.ConfirmAsync(quote.QuoteId);

        Assert.Equal(460_000m, receipt.UpdatedCustomerPkrBalance);
        Assert.Equal(12m, receipt.UpdatedCustomerGoldBalance);
        Assert.Equal(998m, receipt.UpdatedPlatformInventory);
    }

    [Fact]
    public async Task ConfirmAsync_SuccessfulSell_CreditsPkrDebitsGoldCreditsInventory()
    {
        var h = CreateHarness(pkr: 500_000m, gold: 10m, platform: 1000m);
        var quote = MakeQuote(TradeType.Sell, grams: 3m, pkr: 60_000m);
        h.QuoteRepo.Save(quote);

        var receipt = await h.Service.ConfirmAsync(quote.QuoteId);

        Assert.Equal(560_000m, receipt.UpdatedCustomerPkrBalance);
        Assert.Equal(7m, receipt.UpdatedCustomerGoldBalance);
        Assert.Equal(1003m, receipt.UpdatedPlatformInventory);
    }

    [Fact]
    public async Task ConfirmAsync_InsufficientCash_ThrowsAndLeavesBalancesAndQuoteUnchanged()
    {
        var h = CreateHarness(pkr: 1_000m, gold: 10m, platform: 1000m);
        var quote = MakeQuote(TradeType.Buy, grams: 2m, pkr: 40_000m);
        h.QuoteRepo.Save(quote);

        await Assert.ThrowsAsync<InsufficientCashException>(() => h.Service.ConfirmAsync(quote.QuoteId));

        var snap = h.AccountRepo.GetSnapshot();
        Assert.Equal(1_000m, snap.CustomerPkr);
        Assert.Equal(10m, snap.CustomerGoldGrams);
        Assert.Equal(QuoteStatus.Pending, h.QuoteRepo.GetById(quote.QuoteId)!.Status);
    }

    [Fact]
    public async Task ConfirmAsync_InsufficientGold_ThrowsAndLeavesBalancesUnchanged()
    {
        var h = CreateHarness(pkr: 500_000m, gold: 1m, platform: 1000m);
        var quote = MakeQuote(TradeType.Sell, grams: 5m, pkr: 100_000m);
        h.QuoteRepo.Save(quote);

        await Assert.ThrowsAsync<InsufficientGoldException>(() => h.Service.ConfirmAsync(quote.QuoteId));

        Assert.Equal(1m, h.AccountRepo.GetSnapshot().CustomerGoldGrams);
    }

    [Fact]
    public async Task ConfirmAsync_InsufficientInventory_ThrowsAndLeavesBalancesUnchanged()
    {
        var h = CreateHarness(pkr: 500_000m, gold: 10m, platform: 1m);
        var quote = MakeQuote(TradeType.Buy, grams: 5m, pkr: 100_000m);
        h.QuoteRepo.Save(quote);

        await Assert.ThrowsAsync<InsufficientInventoryException>(() => h.Service.ConfirmAsync(quote.QuoteId));

        Assert.Equal(1m, h.AccountRepo.GetSnapshot().PlatformGoldGrams);
    }

    [Fact]
    public async Task ConfirmAsync_QuoteNotFound_Throws()
    {
        var h = CreateHarness();
        await Assert.ThrowsAsync<QuoteNotFoundException>(() => h.Service.ConfirmAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ConfirmAsync_ExpiredQuote_ThrowsQuoteExpired()
    {
        var h = CreateHarness();
        var quote = MakeQuote(TradeType.Buy, grams: 1m, pkr: 20_000m, expiresInSeconds: -5);
        h.QuoteRepo.Save(quote);

        await Assert.ThrowsAsync<QuoteExpiredException>(() => h.Service.ConfirmAsync(quote.QuoteId));
    }

    [Fact]
    public async Task ConfirmAsync_SameQuoteConfirmedTwiceSequentially_ReturnsSameTrade_CreatesExactlyOne()
    {
        var h = CreateHarness();
        var quote = MakeQuote(TradeType.Buy, grams: 1m, pkr: 20_000m);
        h.QuoteRepo.Save(quote);

        var first  = await h.Service.ConfirmAsync(quote.QuoteId);
        var second = await h.Service.ConfirmAsync(quote.QuoteId);

        Assert.Equal(first.TradeId, second.TradeId);
        Assert.Single(h.TradeRepo.GetAll());
    }

    [Fact]
    public async Task ConfirmAsync_ConcurrentConfirmation_CreatesExactlyOneTrade_AndAppliesBalancesOnce()
    {
        var h = CreateHarness(pkr: 500_000m, gold: 10m, platform: 1000m);
        var quote = MakeQuote(TradeType.Buy, grams: 1m, pkr: 20_000m);
        h.QuoteRepo.Save(quote);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => h.Service.ConfirmAsync(quote.QuoteId)));

        Assert.Single(h.TradeRepo.GetAll());
        Assert.Single(results.Select(r => r.TradeId).Distinct());

        var snap = h.AccountRepo.GetSnapshot();
        Assert.Equal(480_000m, snap.CustomerPkr);
        Assert.Equal(11m, snap.CustomerGoldGrams);
        Assert.Equal(999m, snap.PlatformGoldGrams);
    }
}
