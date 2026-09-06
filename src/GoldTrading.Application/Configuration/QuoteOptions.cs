using System.ComponentModel.DataAnnotations;

namespace GoldTrading.Application.Configuration;

/// <summary>
/// Controls how long a locked quote remains valid for confirmation.
/// Bound from the "Quote" section of appsettings.json.
///
/// Access pattern: inject <c>IOptions&lt;QuoteOptions&gt;</c>.
/// Quote duration is set once at startup and does not change at runtime.
/// </summary>
public sealed class QuoteOptions
{
    public const string Section = "Quote";

    /// <summary>
    /// Seconds a quote remains in Pending status before it automatically
    /// expires.  Must be at least 10 seconds (enough for a user to read
    /// and confirm) and no more than 300 seconds (5 minutes).
    /// </summary>
    [Range(10, 300, ErrorMessage = "DurationSeconds must be between 10 and 300.")]
    public int DurationSeconds { get; init; } = 75;
}
