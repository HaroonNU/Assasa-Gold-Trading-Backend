using System.Net;
using System.Net.Http.Json;
using GoldTrading.Application.DTOs;
using GoldTrading.Domain.Enums;

namespace GoldTrading.IntegrationTests;

public sealed class TradingErrorTests
{
    private static async Task<QuoteResponse> CreateQuoteAsync(HttpClient client, TradeType type, InputType inputType, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(type, inputType, amount), JsonDefaults.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuoteResponse>(JsonDefaults.Options))!;
    }

    [Fact]
    public async Task GetUnknownTrade_Returns404_TradeNotFound()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/trades/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("TRADE_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task ConfirmUnknownQuote_Returns404_QuoteNotFound()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/quotes/{Guid.NewGuid()}/confirm", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("QUOTE_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task ConfirmForceExpiredQuote_Returns410_QuoteExpired()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var quote = await CreateQuoteAsync(client, TradeType.Buy, InputType.Gold, 1m);
        (await client.PostAsync($"/api/demo/quotes/{quote.QuoteId}/force-expire", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/quotes/{quote.QuoteId}/confirm", content: null);

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("QUOTE_EXPIRED", error!.Code);
    }

    [Fact]
    public async Task CreateQuote_WhenAllPricingUnavailable_Returns503_PriceUnavailable()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/pricing/simulate-all-unavailable?enabled=true", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m), JsonDefaults.Options);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("PRICE_UNAVAILABLE", error!.Code);
    }

    [Fact]
    public async Task ConfirmBuy_WithLowPkrBalance_Returns422_InsufficientCash()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/balances/low-pkr", content: null)).EnsureSuccessStatusCode();
        var quote = await CreateQuoteAsync(client, TradeType.Buy, InputType.Gold, 1m);

        var response = await client.PostAsync($"/api/quotes/{quote.QuoteId}/confirm", content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("INSUFFICIENT_CASH", error!.Code);
    }

    [Fact]
    public async Task ConfirmSell_WithLowGoldBalance_Returns422_InsufficientGold()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/balances/low-gold", content: null)).EnsureSuccessStatusCode();
        var quote = await CreateQuoteAsync(client, TradeType.Sell, InputType.Gold, 1m);

        var response = await client.PostAsync($"/api/quotes/{quote.QuoteId}/confirm", content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("INSUFFICIENT_GOLD", error!.Code);
    }

    [Fact]
    public async Task ConfirmBuy_WithLowPlatformInventory_Returns422_InsufficientInventory()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/balances/low-inventory", content: null)).EnsureSuccessStatusCode();
        var quote = await CreateQuoteAsync(client, TradeType.Buy, InputType.Gold, 1m);

        var response = await client.PostAsync($"/api/quotes/{quote.QuoteId}/confirm", content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("INSUFFICIENT_INVENTORY", error!.Code);
    }

    [Fact]
    public async Task CreateQuote_ZeroAmount_Returns400_InvalidTradeAmount()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 0m), JsonDefaults.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonDefaults.Options);
        Assert.Equal("INVALID_TRADE_AMOUNT", error!.Code);
    }
}
