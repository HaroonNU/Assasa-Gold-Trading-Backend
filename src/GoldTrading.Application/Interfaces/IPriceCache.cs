using GoldTrading.Domain.Entities;

namespace GoldTrading.Application.Interfaces;

/// <summary>
/// Caches the gold price and enforces the configured refresh interval.
/// Implementations must be thread-safe.
/// </summary>
public interface IPriceCache
{
    Task<GoldPrice> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the cached price so the next <see cref="GetAsync"/> call re-fetches
    /// from providers immediately, ignoring the configured cache interval.
    /// Used by demo endpoints so simulated failures/overrides take effect at once.
    /// </summary>
    void Invalidate();
}
