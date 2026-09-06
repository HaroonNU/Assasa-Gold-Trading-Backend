using GoldTrading.Application.Configuration;
using GoldTrading.Application.DTOs;
using GoldTrading.Application.Exceptions;
using GoldTrading.Application.Interfaces;
using GoldTrading.Application.Services;
using GoldTrading.Application.Tests.TestDoubles;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace GoldTrading.Application.Tests.Services;

public sealed class QuoteServiceTests
{
    private static GoldPrice TrustedPrice(decimal price = 20_000m) => new()
    {
        PricePerGram = price,
        Source       = PriceSource.PakGold,
        FetchedAt    = DateTimeOffset.UtcNow,
        IsTrusted    = true
    };

    private static QuoteService CreateService(GoldPrice price, IQuoteRepository? repo = null)
    {
        var priceCache = new FakePriceCache(price);
        var pricing = Options.Create(new PricingOptions
        {
            BuyMarkupMultiplier    = 1.015m,
            SellMarkdownMultiplier = 0.985m,
            BuyPriceGuardrail      = 8500m
        });
        var quote   = Options.Create(new QuoteOptions { DurationSeconds = 75 });
        var trading = Options.Create(new TradingOptions());

        return new QuoteService(priceCache, repo ?? new InMemoryQuoteRepository(), pricing, quote, trading);
    }

    [Fact]
    public async Task CreateAsync_BuyWithPkrInput_ComputesGoldAmount()
    {
        var service = CreateService(TrustedPrice(20_000m));

        var result = await service.CreateAsync(new CreateQuoteRequest(TradeType.Buy, InputType.Pkr, 20_300m));

        Assert.Equal(20_300m, result.CustomerPricePerGram); // max(20000*1.015, 8500)
        Assert.Equal(1m, result.GoldAmountInGrams);
        Assert.Equal(20_300m, result.PkrAmount);
    }

    [Fact]
    public async Task CreateAsync_BuyWithGoldInput_ComputesPkrAmount()
    {
        var service = CreateService(TrustedPrice(20_000m));

        var result = await service.CreateAsync(new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 2m));

        Assert.Equal(2m, result.GoldAmountInGrams);
        Assert.Equal(40_600m, result.PkrAmount);
    }

    [Fact]
    public async Task CreateAsync_SellWithGoldInput_AppliesMarkdown()
    {
        var service = CreateService(TrustedPrice(20_000m));

        var result = await service.CreateAsync(new CreateQuoteRequest(TradeType.Sell, InputType.Gold, 1m));

        Assert.Equal(19_700m, result.CustomerPricePerGram); // 20000*0.985
        Assert.Equal(19_700m, result.PkrAmount);
    }

    [Fact]
    public async Task CreateAsync_SellWithPkrInput_ComputesGoldAmount()
    {
        var service = CreateService(TrustedPrice(20_000m));

        var result = await service.CreateAsync(new CreateQuoteRequest(TradeType.Sell, InputType.Pkr, 19_700m));

        Assert.Equal(1m, result.GoldAmountInGrams);
    }

    [Fact]
    public async Task CreateAsync_SetsExpiryFromQuoteOptionsDuration()
    {
        var service = CreateService(TrustedPrice());
        var before = DateTimeOffset.UtcNow;

        var result = await service.CreateAsync(new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m));

        Assert.InRange(result.ExpiresAtUtc, before.AddSeconds(74), before.AddSeconds(76));
    }

    [Fact]
    public async Task CreateAsync_UntrustedPrice_ThrowsPriceUnavailable()
    {
        var untrusted = new GoldPrice
        {
            PricePerGram = 0, Source = PriceSource.Unavailable, FetchedAt = DateTimeOffset.UtcNow, IsTrusted = false
        };
        var service = CreateService(untrusted);

        await Assert.ThrowsAsync<PriceUnavailableException>(() =>
            service.CreateAsync(new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m)));
    }

    [Fact]
    public async Task CreateAsync_ZeroAmount_ThrowsInvalidTradeAmount()
    {
        var service = CreateService(TrustedPrice());

        await Assert.ThrowsAsync<InvalidTradeAmountException>(() =>
            service.CreateAsync(new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 0m)));
    }

    [Fact]
    public async Task CreateAsync_GoldAmountAboveMaximum_Throws()
    {
        var service = CreateService(TrustedPrice());

        await Assert.ThrowsAsync<InvalidTradeAmountException>(() =>
            service.CreateAsync(new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 999_999m)));
    }

    [Fact]
    public async Task CreateAsync_PersistsQuote_RetrievableFromRepository()
    {
        var repo = new InMemoryQuoteRepository();
        var service = CreateService(TrustedPrice(), repo);

        var result = await service.CreateAsync(new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m));

        Assert.NotNull(repo.GetById(result.QuoteId));
    }
}
