using GoldTrading.Application.DTOs;
using GoldTrading.Application.Interfaces;

namespace GoldTrading.Application.Services;

/// <summary>
/// Read-only view of the current cached gold price (Prompt 1, requirement #2 —
/// "Show live 24K gold price in PKR per gram"). Never triggers a quote or any
/// balance/trade side effects; it only surfaces what <see cref="IPriceCache"/>
/// currently holds.
/// </summary>
public sealed class PriceQueryService
{
    private readonly IPriceCache _priceCache;
    public PriceQueryService(IPriceCache priceCache) => _priceCache = priceCache;

    public async Task<CurrentPriceResponse> GetCurrentAsync(CancellationToken ct = default)
    {
        var price = await _priceCache.GetAsync(ct);
        return new CurrentPriceResponse(price.PricePerGram, price.Source.ToString(), price.FetchedAt, price.IsTrusted);
    }
}
