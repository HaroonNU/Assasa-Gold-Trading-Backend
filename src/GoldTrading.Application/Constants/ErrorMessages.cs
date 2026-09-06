namespace GoldTrading.Application.Constants;

/// <summary>User-facing error messages. Always paired with an ErrorCode.</summary>
public static class ErrorMessages
{
    public const string PriceUnavailable      = "Gold price is currently unavailable. Trading is disabled.";
    public const string QuoteNotFound         = "Quote not found.";
    public const string TradeNotFound         = "Trade not found.";
    public const string QuoteExpired          = "This quote has expired. Please request a new one.";
    public const string QuoteAlreadyConfirmed = "This quote has already been confirmed.";
    public const string InsufficientCash      = "Insufficient PKR balance to complete this purchase.";
    public const string InsufficientGold      = "Insufficient gold balance to complete this sale.";
    public const string InsufficientInventory = "Insufficient platform gold inventory.";
    public const string TradeFailed           = "Trade could not be completed. Please try again.";
    public const string UnexpectedError       = "An unexpected error occurred. Please try again.";
}
