namespace GoldTrading.Application.Constants;

public static class ApiMessages
{
    public const string ApiTitle           = "Asasa Gold Trading API";
    public const string ApiVersion         = "v1";
    public const string DefaultCorsPolicy  = "AllowFrontend";

    // Named route constants — used in CreatedAtAction / route attributes
    public const string GetBalancesRoute   = "GetBalances";
    public const string GetTradeRoute      = "GetTrade";
    public const string GetReceiptRoute    = "GetReceipt";

    // Structured log templates
    public const string QuoteCreatedLog    = "Quote {QuoteId} created: {TradeType} {GoldGrams}g @ {CustomerPrice}/g";
    public const string TradeExecutedLog   = "Trade {TradeId} settled: {TradeType} {GoldGrams}g for PKR {PkrAmount}";
    public const string PriceFetchLog      = "Price fetched from {Source}: PKR {Price}/g";
    public const string ProviderFailLog    = "Provider {Provider} failed, trying fallback";
}
