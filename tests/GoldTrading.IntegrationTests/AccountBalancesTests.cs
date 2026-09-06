using System.Net;
using System.Net.Http.Json;
using GoldTrading.Application.DTOs;

namespace GoldTrading.IntegrationTests;

public sealed class AccountBalancesTests
{
    [Fact]
    public async Task GetBalances_ReturnsSeedValuesFromConfiguration()
    {
        using var factory = new GoldTradingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/account/balances");
        response.EnsureSuccessStatusCode();

        var balances = await response.Content.ReadFromJsonAsync<AccountBalancesResponse>(JsonDefaults.Options);

        Assert.NotNull(balances);
        Assert.Equal(500_000m, balances!.CustomerPkrBalance);
        Assert.Equal(10m, balances.CustomerGoldGrams);
        Assert.Equal(1000m, balances.PlatformGoldInventoryGrams);
    }
}
