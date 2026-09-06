using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;

namespace GoldTrading.Infrastructure.Repositories;

/// <summary>
/// Stores completed trades in two indexes: by TradeId and by QuoteId.
///
/// Synchronisation strategy
/// ────────────────────────
/// A single <c>object _lock</c> guards both dictionaries.  <see cref="Save"/>
/// enforces the one-trade-per-quote invariant inside the lock: if a trade for
/// the given QuoteId already exists it throws, making duplicate settlement
/// impossible even if <see cref="TradeSettlementLock"/> were somehow bypassed.
///
/// Why Singleton: trade history must persist for the lifetime of the process.
/// </summary>
public sealed class InMemoryTradeRepository : ITradeRepository
{
    private readonly Dictionary<Guid, Trade> _byTradeId = new();
    private readonly Dictionary<Guid, Trade> _byQuoteId = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public void Save(Trade trade)
    {
        ArgumentNullException.ThrowIfNull(trade);
        lock (_lock)
        {
            if (_byQuoteId.ContainsKey(trade.QuoteId))
                throw new InvalidOperationException(
                    $"A trade for QuoteId {trade.QuoteId} already exists. " +
                    "Duplicate settlement is not allowed.");

            _byTradeId[trade.TradeId] = trade;
            _byQuoteId[trade.QuoteId] = trade;
        }
    }

    /// <inheritdoc/>
    public Trade? GetByQuoteId(Guid quoteId)
    {
        lock (_lock)
        {
            return _byQuoteId.GetValueOrDefault(quoteId);
        }
    }

    /// <inheritdoc/>
    public Trade? GetById(Guid tradeId)
    {
        lock (_lock)
        {
            return _byTradeId.GetValueOrDefault(tradeId);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<Trade> GetAll()
    {
        lock (_lock)
        {
            return _byTradeId.Values
                .OrderByDescending(t => t.CompletedAtUtc)
                .ToList();
        }
    }
}
