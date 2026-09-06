using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;

namespace GoldTrading.Application.Tests.TestDoubles;

internal sealed class FakePriceCache : IPriceCache
{
    private readonly GoldPrice _price;
    public FakePriceCache(GoldPrice price) => _price = price;
    public Task<GoldPrice> GetAsync(CancellationToken ct = default) => Task.FromResult(_price);
    public void Invalidate() { }
}
