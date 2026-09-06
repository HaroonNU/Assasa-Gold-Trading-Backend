using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;

namespace GoldTrading.Application.Tests.TestDoubles;

internal sealed class FakePriceProvider : IPriceProvider
{
    private readonly Func<GoldPrice?> _result;
    public int CallCount { get; private set; }

    public FakePriceProvider(Func<GoldPrice?> result) => _result = result;

    public Task<GoldPrice?> FetchAsync(CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult(_result());
    }
}
