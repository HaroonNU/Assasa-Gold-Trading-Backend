namespace GoldTrading.Application.Interfaces;

/// <summary>
/// Mutable, in-memory demo-only overrides for the pricing pipeline.
/// Consumed by the demo-aware price provider decorator in Infrastructure so
/// reviewers can exercise failure/fallback/guardrail scenarios without
/// touching deployed code or the core provider implementations.
/// Implementations must be thread-safe.
/// </summary>
public interface IDemoPricingState
{
    bool PrimaryFailureSimulated { get; }
    bool AllUnavailableSimulated { get; }
    decimal? OverridePricePerGram { get; }

    void SetPrimaryFailure(bool enabled);
    void SetAllUnavailable(bool enabled);
    void SetOverridePrice(decimal? pricePerGram);
    void Reset();
}
