using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;

namespace GoldTrading.Infrastructure.Pricing;

/// <summary>
/// Decorates a real price provider with demo-only overrides so reviewers can
/// exercise failure/fallback/guardrail scenarios without touching deployed
/// code. The core providers (<see cref="PakGoldProvider"/>,
/// <see cref="GoldPriceOrgProvider"/>) have zero knowledge of demo state —
/// this decorator is the only place demo concerns intersect the pricing
/// pipeline, keeping it clearly separated from core business logic.
/// </summary>
public sealed class DemoAwarePriceProvider : IPriceProvider
{
    private readonly IPriceProvider _inner;
    private readonly IDemoPricingState _demoState;
    private readonly bool _isPrimary;
    private readonly PriceSource _sourceWhenOverridden;

    public DemoAwarePriceProvider(
        IPriceProvider inner,
        IDemoPricingState demoState,
        bool isPrimary,
        PriceSource sourceWhenOverridden)
    {
        _inner                = inner;
        _demoState            = demoState;
        _isPrimary            = isPrimary;
        _sourceWhenOverridden = sourceWhenOverridden;
    }

    public Task<GoldPrice?> FetchAsync(CancellationToken ct = default)
    {
        if (_demoState.AllUnavailableSimulated || (_isPrimary && _demoState.PrimaryFailureSimulated))
            return Task.FromResult<GoldPrice?>(null);

        if (_demoState.OverridePricePerGram is { } price)
            return Task.FromResult<GoldPrice?>(new GoldPrice
            {
                PricePerGram = price,
                Source       = _sourceWhenOverridden,
                FetchedAt    = DateTimeOffset.UtcNow,
                IsTrusted    = true
            });

        return _inner.FetchAsync(ct);
    }
}
