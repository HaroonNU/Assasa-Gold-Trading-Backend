namespace GoldTrading.Application.Constants;

/// <summary>Validation failure messages for request input errors.</summary>
public static class ValidationMessages
{
    public const string AmountRequired        = "Amount is required.";
    public const string AmountMustBePositive  = "Amount must be greater than zero.";
    public const string InvalidTradeType      = "TradeType must be Buy or Sell.";
    public const string InvalidInputType      = "InputType must be Pkr or Gold.";
    public const string GoldBelowMinimum      = "Gold amount is below the minimum trade size.";
    public const string GoldAboveMaximum      = "Gold amount exceeds the maximum trade size.";
    public const string PkrBelowMinimum       = "PKR amount is below the minimum trade size.";
    public const string PkrAboveMaximum       = "PKR amount exceeds the maximum trade size.";
}
