using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Infrastructure.Repositories;

namespace GoldTrading.Application.Tests.Repositories;

public sealed class InMemoryQuoteRepositoryTests
{
    private static Quote MakePendingQuote(int expiresInSeconds = 120) => new()
    {
        TradeType            = TradeType.Buy,
        GoldAmountInGrams    = 1m,
        PkrAmount            = 10_000m,
        LockedMarketPrice    = 9_800m,
        CustomerPricePerGram = 10_000m,
        PricingSource        = PriceSource.PakGold,
        PriceRetrievedAtUtc  = DateTimeOffset.UtcNow,
        CreatedAtUtc         = DateTimeOffset.UtcNow,
        ExpiresAtUtc         = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
    };

    [Fact]
    public void Save_And_GetById_RoundTrips()
    {
        var repo  = new InMemoryQuoteRepository();
        var quote = MakePendingQuote();
        repo.Save(quote);

        var found = repo.GetById(quote.QuoteId);
        Assert.NotNull(found);
        Assert.Equal(quote.QuoteId, found.QuoteId);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var repo = new InMemoryQuoteRepository();
        Assert.Null(repo.GetById(Guid.NewGuid()));
    }

    [Fact]
    public void TryConfirm_PendingQuote_ReturnsTrue_AndSetsStatus()
    {
        var repo  = new InMemoryQuoteRepository();
        var quote = MakePendingQuote();
        repo.Save(quote);

        var result = repo.TryConfirm(quote.QuoteId);

        Assert.True(result);
        Assert.Equal(QuoteStatus.Confirmed, repo.GetById(quote.QuoteId)!.Status);
        Assert.NotNull(repo.GetById(quote.QuoteId)!.ConfirmedAtUtc);
    }

    [Fact]
    public void TryConfirm_AlreadyConfirmed_ReturnsFalse()
    {
        var repo  = new InMemoryQuoteRepository();
        var quote = MakePendingQuote();
        repo.Save(quote);

        repo.TryConfirm(quote.QuoteId);
        var second = repo.TryConfirm(quote.QuoteId);

        Assert.False(second);
    }

    [Fact]
    public void TryConfirm_ExpiredQuote_ReturnsFalse_AndSetsExpiredStatus()
    {
        var repo  = new InMemoryQuoteRepository();
        var quote = MakePendingQuote(expiresInSeconds: -1);
        repo.Save(quote);

        var result = repo.TryConfirm(quote.QuoteId);

        Assert.False(result);
        Assert.Equal(QuoteStatus.Expired, repo.GetById(quote.QuoteId)!.Status);
    }

    [Fact]
    public void TryConfirm_UnknownId_ReturnsFalse()
    {
        var repo = new InMemoryQuoteRepository();
        Assert.False(repo.TryConfirm(Guid.NewGuid()));
    }

    [Fact]
    public async Task TryConfirm_ConcurrentRequests_OnlyOneSucceeds()
    {
        var repo  = new InMemoryQuoteRepository();
        var quote = MakePendingQuote();
        repo.Save(quote);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() => repo.TryConfirm(quote.QuoteId))));

        Assert.Equal(1, results.Count(r => r));
    }

    [Fact]
    public void MarkExpired_PendingQuote_SetsExpiredStatus()
    {
        var repo  = new InMemoryQuoteRepository();
        var quote = MakePendingQuote();
        repo.Save(quote);

        repo.MarkExpired(quote.QuoteId);

        Assert.Equal(QuoteStatus.Expired, repo.GetById(quote.QuoteId)!.Status);
    }

    [Fact]
    public void MarkExpired_ConfirmedQuote_DoesNotChangeStatus()
    {
        var repo  = new InMemoryQuoteRepository();
        var quote = MakePendingQuote();
        repo.Save(quote);
        repo.TryConfirm(quote.QuoteId);

        repo.MarkExpired(quote.QuoteId);

        Assert.Equal(QuoteStatus.Confirmed, repo.GetById(quote.QuoteId)!.Status);
    }
}
