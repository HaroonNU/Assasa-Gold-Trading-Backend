using System.Net.Http.Json;
using GoldTrading.Application.DTOs;
using GoldTrading.Domain.Enums;

namespace GoldTrading.IntegrationTests;

public sealed class QuoteAndTradeFlowTests
{
    [Theory]
    [InlineData(TradeType.Buy, InputType.Gold)]
    [InlineData(TradeType.Buy, InputType.Pkr)]
    [InlineData(TradeType.Sell, InputType.Gold)]
    [InlineData(TradeType.Sell, InputType.Pkr)]
    public async Task CreateQuote_ThenConfirm_SettlesTradeAndUpdatesBalances(TradeType tradeType, InputType inputType)
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var amount = inputType == InputType.Gold ? 1m : 20_000m;
        var createResponse = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(tradeType, inputType, amount), JsonDefaults.Options);
        createResponse.EnsureSuccessStatusCode();

        var quote = await createResponse.Content.ReadFromJsonAsync<QuoteResponse>(JsonDefaults.Options);
        Assert.NotNull(quote);
        Assert.Equal(tradeType, quote!.TradeType);
        Assert.True(quote.GoldAmountInGrams > 0);
        Assert.True(quote.PkrAmount > 0);
        Assert.True(quote.RemainingSeconds > 0);

        var confirmResponse = await client.PostAsync($"/api/quotes/{quote.QuoteId}/confirm", content: null);
        confirmResponse.EnsureSuccessStatusCode();

        var receipt = await confirmResponse.Content.ReadFromJsonAsync<TradeReceiptResponse>(JsonDefaults.Options);
        Assert.NotNull(receipt);
        Assert.Equal(quote.QuoteId, receipt!.QuoteId);
        Assert.Equal(tradeType, receipt.TradeType);

        var balancesResponse = await client.GetFromJsonAsync<AccountBalancesResponse>(
            "/api/account/balances", JsonDefaults.Options);
        Assert.Equal(receipt.UpdatedCustomerPkrBalance, balancesResponse!.CustomerPkrBalance);
        Assert.Equal(receipt.UpdatedCustomerGoldBalance, balancesResponse.CustomerGoldGrams);
        Assert.Equal(receipt.UpdatedPlatformInventory, balancesResponse.PlatformGoldInventoryGrams);
    }

    [Fact]
    public async Task ConfirmSameQuoteTwice_ReturnsSameTrade_DoesNotDoubleApplyBalances()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m), JsonDefaults.Options);
        var quote = await createResponse.Content.ReadFromJsonAsync<QuoteResponse>(JsonDefaults.Options);

        var first  = await client.PostAsync($"/api/quotes/{quote!.QuoteId}/confirm", content: null);
        var second = await client.PostAsync($"/api/quotes/{quote.QuoteId}/confirm", content: null);
        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        var firstReceipt  = await first.Content.ReadFromJsonAsync<TradeReceiptResponse>(JsonDefaults.Options);
        var secondReceipt = await second.Content.ReadFromJsonAsync<TradeReceiptResponse>(JsonDefaults.Options);

        Assert.Equal(firstReceipt!.TradeId, secondReceipt!.TradeId);
        Assert.Equal(firstReceipt.UpdatedCustomerGoldBalance, secondReceipt.UpdatedCustomerGoldBalance);
    }

    [Fact]
    public async Task GetTrade_AfterConfirm_ReturnsMatchingReceipt()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/quotes", new CreateQuoteRequest(TradeType.Buy, InputType.Gold, 1m), JsonDefaults.Options);
        var quote = await createResponse.Content.ReadFromJsonAsync<QuoteResponse>(JsonDefaults.Options);
        var confirmResponse = await client.PostAsync($"/api/quotes/{quote!.QuoteId}/confirm", content: null);
        var receipt = await confirmResponse.Content.ReadFromJsonAsync<TradeReceiptResponse>(JsonDefaults.Options);

        var fetched = await client.GetFromJsonAsync<TradeReceiptResponse>(
            $"/api/trades/{receipt!.TradeId}", JsonDefaults.Options);

        Assert.Equal(receipt.TradeId, fetched!.TradeId);
        Assert.Equal(receipt.PkrAmount, fetched.PkrAmount);
    }
}
