using System.Net.Http.Json;
using GoldTrading.Application.DTOs;
using GoldTrading.Domain.Enums;

namespace GoldTrading.IntegrationTests;

public sealed class DemoControlsTests
{
    [Fact]
    public async Task GuardrailScenario_ForcesBuyPriceToGuardrailFloor()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/pricing/guardrail-scenario", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m), JsonDefaults.Options);
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>(JsonDefaults.Options);

        // appsettings.json BuyPriceGuardrail = 8500.00
        Assert.Equal(8500m, quote!.CustomerPricePerGram);
    }

    [Fact]
    public async Task SimulatePrimaryFailure_FallsBackToGoldPriceOrg()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/pricing/simulate-primary-failure?enabled=true", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m), JsonDefaults.Options);
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>(JsonDefaults.Options);

        Assert.Equal("GoldPriceOrg", quote!.PricingSource);
    }

    [Fact]
    public async Task Reset_RestoresBalancesToSeedValues_AndClearsPricingOverrides()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/api/demo/balances/low-pkr", content: null)).EnsureSuccessStatusCode();
        (await client.PostAsync("/api/demo/pricing/guardrail-scenario", content: null)).EnsureSuccessStatusCode();

        (await client.PostAsync("/api/demo/reset", content: null)).EnsureSuccessStatusCode();

        var balances = await client.GetFromJsonAsync<AccountBalancesResponse>(
            "/api/account/balances", JsonDefaults.Options);
        Assert.Equal(500_000m, balances!.CustomerPkrBalance);
        Assert.Equal(10m, balances.CustomerGoldGrams);
        Assert.Equal(1000m, balances.PlatformGoldInventoryGrams);

        var quoteResponse = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m), JsonDefaults.Options);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<QuoteResponse>(JsonDefaults.Options);

        // Guardrail override cleared -> back to fixed market price (20000) * markup (1.10) = 22000
        Assert.Equal(22_000m, quote!.CustomerPricePerGram);
    }
}
