using GoldTrading.Application.Interfaces;
using GoldTrading.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GoldTrading.Api.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<ITradeSettlementService, TradeSettlementService>();
        services.AddScoped<AccountService>();
        services.AddScoped<TradeQueryService>();
        services.AddScoped<PriceQueryService>();
        services.AddScoped<DemoControlService>();
        return services;
    }
}
