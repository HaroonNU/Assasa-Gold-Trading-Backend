namespace GoldTrading.Domain.Constants;

/// <summary>
/// Numeric and domain constants that belong to business rules.
/// No user-facing strings here — those live in Application.Constants.
/// </summary>
public static class DomainConstants
{
    /// <summary>Troy ounces per gram. Used when converting XAU spot price.</summary>
    public const decimal TroyOzToGrams = 31.1035m;

    /// <summary>Decimal places to round gold gram quantities to.</summary>
    public const int GoldGramPrecision = 6;

    /// <summary>Decimal places to round PKR monetary amounts to.</summary>
    public const int PkrPrecision = 2;
}
