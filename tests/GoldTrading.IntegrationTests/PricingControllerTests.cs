using System.Net.Http.Json;
using GoldTrading.Application.DTOs;

namespace GoldTrading.IntegrationTests;

public sealed class PricingControllerTests
{
    [Fact]
    public async Task GetCurrent_ReturnsTrustedFixedPrice()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var price = await client.GetFromJsonAsync<CurrentPriceResponse>("/api/pricing/current", JsonDefaults.Options);

        Assert.NotNull(price);
        Assert.True(price!.IsTrusted);
        Assert.Equal(GoldTradingApiFactory.FixedMarketPricePerGram, price.PricePerGram);
        Assert.Equal("PakGold", price.Source);
    }

    [Fact]
    public async Task GetCurrent_WhenAllPricingUnavailable_ReturnsUntrusted()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/pricing/simulate-all-unavailable?enabled=true", content: null)).EnsureSuccessStatusCode();

        var price = await client.GetFromJsonAsync<CurrentPriceResponse>("/api/pricing/current", JsonDefaults.Options);

        Assert.NotNull(price);
        Assert.False(price!.IsTrusted);
    }
}
