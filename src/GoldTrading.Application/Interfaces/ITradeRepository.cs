using GoldTrading.Domain.Entities;

namespace GoldTrading.Application.Interfaces;

/// <summary>
/// Stores and retrieves completed trades.
/// Enforces the one-trade-per-quote invariant.
/// </summary>
public interface ITradeRepository
{
    /// <summary>
    /// Persists a trade.
    /// Throws <see cref="InvalidOperationException"/> if a trade for
    /// <paramref name="trade"/>.QuoteId already exists.
    /// </summary>
    void Save(Trade trade);

    Trade? GetByQuoteId(Guid quoteId);

    Trade? GetById(Guid tradeId);

    IReadOnlyList<Trade> GetAll();
}
