using GoldTrading.Infrastructure.Pricing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GoldTrading.IntegrationTests;

/// <summary>
/// Boots the full API pipeline (real DI graph, real controllers, real
/// PriceCache/DemoAwarePriceProvider/DemoPricingState) but swaps the HTTP
/// transport under PakGoldProvider and GoldPriceOrgProvider for a canned
/// response, so tests never depend on the real PakGold / GoldPrice.org APIs.
///
/// Each test creates its own factory instance (see usages) so the singleton
/// in-memory repositories never leak state between tests.
/// </summary>
public sealed class GoldTradingApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Price PakGoldProvider "returns" for every test unless overridden via the demo endpoints.</summary>
    public const decimal FixedMarketPricePerGram = 20_000m;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 2000 USD/oz * 311.035 PKR/USD / 31.1035 g/oz = exactly 20,000 PKR/g,
            // so FixedMarketPricePerGram stays the single source of truth for
            // every test that asserts against it.
            services.AddHttpClient<PakGoldProvider>()
                .ConfigurePrimaryHttpMessageHandler(() => new RoutingFakeHttpMessageHandler(
                    ("api.gold-api.com", """{"price": 2000}"""),
                    ("open.er-api.com", """{"result": "success", "rates": {"PKR": 311.035}}""")));

            services.AddHttpClient<GoldPriceOrgProvider>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new FakeHttpMessageHandler("""{"xauPrice": 2650.5, "usdPrice": 278.4}"""));
        });
    }
}
