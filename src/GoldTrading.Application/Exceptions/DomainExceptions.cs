using GoldTrading.Application.Constants;

namespace GoldTrading.Application.Exceptions;

public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    protected DomainException(string errorCode, string message) : base(message)
        => ErrorCode = errorCode;
}

public sealed class PriceUnavailableException : DomainException
{
    public PriceUnavailableException() : base(ErrorCodes.PriceUnavailable, ErrorMessages.PriceUnavailable) { }
}

public sealed class QuoteNotFoundException : DomainException
{
    public QuoteNotFoundException() : base(ErrorCodes.QuoteNotFound, ErrorMessages.QuoteNotFound) { }
}

public sealed class TradeNotFoundException : DomainException
{
    public TradeNotFoundException() : base(ErrorCodes.TradeNotFound, ErrorMessages.TradeNotFound) { }
}

public sealed class QuoteExpiredException : DomainException
{
    public QuoteExpiredException() : base(ErrorCodes.QuoteExpired, ErrorMessages.QuoteExpired) { }
}

public sealed class QuoteAlreadyConfirmedException : DomainException
{
    public QuoteAlreadyConfirmedException() : base(ErrorCodes.QuoteAlreadyConfirmed, ErrorMessages.QuoteAlreadyConfirmed) { }
}

public sealed class InsufficientCashException : DomainException
{
    public InsufficientCashException() : base(ErrorCodes.InsufficientCash, ErrorMessages.InsufficientCash) { }
}

public sealed class InsufficientGoldException : DomainException
{
    public InsufficientGoldException() : base(ErrorCodes.InsufficientGold, ErrorMessages.InsufficientGold) { }
}

public sealed class InsufficientInventoryException : DomainException
{
    public InsufficientInventoryException() : base(ErrorCodes.InsufficientInventory, ErrorMessages.InsufficientInventory) { }
}

public sealed class InvalidTradeAmountException : DomainException
{
    public InvalidTradeAmountException(string message) : base(ErrorCodes.InvalidTradeAmount, message) { }
}
