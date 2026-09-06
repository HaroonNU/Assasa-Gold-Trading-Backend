using GoldTrading.Application.Interfaces;

namespace GoldTrading.Infrastructure.Pricing;

/// <summary>
/// Thread-safe holder for demo-only pricing overrides. Singleton for the
/// process lifetime so a demo action taken via one request is visible to
/// every subsequent quote request.
/// </summary>
public sealed class DemoPricingState : IDemoPricingState
{
    private readonly object _lock = new();
    private bool _primaryFailure;
    private bool _allUnavailable;
    private decimal? _overridePrice;

    public bool PrimaryFailureSimulated
    {
        get { lock (_lock) return _primaryFailure; }
    }

    public bool AllUnavailableSimulated
    {
        get { lock (_lock) return _allUnavailable; }
    }

    public decimal? OverridePricePerGram
    {
        get { lock (_lock) return _overridePrice; }
    }

    public void SetPrimaryFailure(bool enabled)
    {
        lock (_lock) _primaryFailure = enabled;
    }

    public void SetAllUnavailable(bool enabled)
    {
        lock (_lock) _allUnavailable = enabled;
    }

    public void SetOverridePrice(decimal? pricePerGram)
    {
        lock (_lock) _overridePrice = pricePerGram;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _primaryFailure = false;
            _allUnavailable = false;
            _overridePrice = null;
        }
    }
}
