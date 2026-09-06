using GoldTrading.Application.Configuration;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Infrastructure.Pricing;
using GoldTrading.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoldTrading.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // ── Seed AccountState from config ─────────────────────────────────
        services.AddSingleton(sp =>
        {
            var seed = sp.GetRequiredService<IOptions<SeedDataOptions>>().Value;
            return new AccountState
            {
                CustomerPkr       = seed.CustomerPkr,
                CustomerGoldGrams = seed.CustomerGoldGrams,
                PlatformGoldGrams = seed.PlatformGoldGrams
            };
        });

        // ── Repositories — Singleton ──────────────────────────────────────
        services.AddSingleton<IAccountStateRepository, InMemoryAccountStateRepository>();
        services.AddSingleton<IQuoteRepository,        InMemoryQuoteRepository>();
        services.AddSingleton<ITradeRepository,        InMemoryTradeRepository>();
        services.AddSingleton<ITradeSettlementLock,    TradeSettlementLock>();

        // ── Demo state — Singleton so demo actions persist across requests ─
        services.AddSingleton<IDemoPricingState, DemoPricingState>();

        // ── Price providers — keyed registrations, wrapped for demo overrides
        services.AddHttpClient<PakGoldProvider>();
        services.AddHttpClient<GoldPriceOrgProvider>();
        services.AddKeyedTransient<IPriceProvider>("primary", (sp, _) =>
            new DemoAwarePriceProvider(
                sp.GetRequiredService<PakGoldProvider>(),
                sp.GetRequiredService<IDemoPricingState>(),
                isPrimary: true,
                sourceWhenOverridden: PriceSource.PakGold));
        services.AddKeyedTransient<IPriceProvider>("fallback", (sp, _) =>
            new DemoAwarePriceProvider(
                sp.GetRequiredService<GoldPriceOrgProvider>(),
                sp.GetRequiredService<IDemoPricingState>(),
                isPrimary: false,
                sourceWhenOverridden: PriceSource.GoldPriceOrg));

        // ── PriceCache — Singleton, providers resolved manually by key ────
        services.AddSingleton<IPriceCache>(sp => new PriceCache(
            sp.GetRequiredKeyedService<IPriceProvider>("primary"),
            sp.GetRequiredKeyedService<IPriceProvider>("fallback"),
            sp.GetRequiredService<IOptions<PricingOptions>>(),
            sp.GetRequiredService<ILogger<PriceCache>>()
        ));

        return services;
    }
}
