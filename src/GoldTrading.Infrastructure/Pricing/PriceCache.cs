using GoldTrading.Application.Configuration;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoldTrading.Infrastructure.Pricing;

public sealed class PriceCache : IPriceCache
{
    private readonly IPriceProvider _primary;
    private readonly IPriceProvider _fallback;
    private readonly PricingOptions _options;
    private readonly ILogger<PriceCache> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private GoldPrice? _cached;

    // Providers are injected by the DI factory in InfrastructureServiceExtensions
    // which resolves them by key and passes them as positional arguments.
    public PriceCache(
        IPriceProvider primary,
        IPriceProvider fallback,
        IOptions<PricingOptions> options,
        ILogger<PriceCache> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _options  = options.Value;
        _logger   = logger;
    }

    public async Task<GoldPrice> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null && !IsStale(_cached))
            return _cached;

        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is not null && !IsStale(_cached))
                return _cached;

            var price = await _primary.FetchAsync(ct);

            if (price is null)
            {
                _logger.LogWarning("Primary provider failed, trying fallback");
                price = await _fallback.FetchAsync(ct);
            }

            _cached = price ?? new GoldPrice
            {
                PricePerGram = 0,
                Source       = PriceSource.Unavailable,
                FetchedAt    = DateTimeOffset.UtcNow,
                IsTrusted    = false
            };

            return _cached;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public void Invalidate()
    {
        _gate.Wait();
        try
        {
            _cached = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsStale(GoldPrice price) =>
        (DateTimeOffset.UtcNow - price.FetchedAt).TotalMinutes >= _options.CacheDurationMinutes;
}
