namespace GoldTrading.Application.Constants;

/// <summary>
/// Machine-readable error codes returned in the "code" field of every error response.
/// The frontend uses these to drive UI behaviour — never parse the message string.
/// </summary>
public static class ErrorCodes
{
    public const string PriceUnavailable       = "PRICE_UNAVAILABLE";
    public const string QuoteNotFound          = "QUOTE_NOT_FOUND";
    public const string TradeNotFound          = "TRADE_NOT_FOUND";
    public const string QuoteExpired           = "QUOTE_EXPIRED";
    public const string QuoteAlreadyConfirmed  = "QUOTE_ALREADY_CONFIRMED";
    public const string InsufficientCash       = "INSUFFICIENT_CASH";
    public const string InsufficientGold       = "INSUFFICIENT_GOLD";
    public const string InsufficientInventory  = "INSUFFICIENT_INVENTORY";
    public const string InvalidTradeAmount     = "INVALID_TRADE_AMOUNT";
    public const string InvalidInput           = "INVALID_INPUT";
    public const string TradeFailed            = "TRADE_FAILED";
}
