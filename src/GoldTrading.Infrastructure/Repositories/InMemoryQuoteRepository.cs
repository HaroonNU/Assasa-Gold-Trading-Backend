using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;

namespace GoldTrading.Infrastructure.Repositories;

/// <summary>
/// Stores quotes in a <see cref="Dictionary{TKey,TValue}"/> keyed by QuoteId.
///
/// Synchronisation strategy
/// ────────────────────────
/// A single <c>object _lock</c> guards the dictionary.  <see cref="TryConfirm"/>
/// is the idempotency gate: it checks and transitions the status atomically
/// inside the lock so two concurrent callers cannot both see Pending and both
/// return true.
///
/// Why Singleton: quotes must survive across HTTP requests.  A Scoped lifetime
/// would create a new empty dictionary per request.
/// </summary>
public sealed class InMemoryQuoteRepository : IQuoteRepository
{
    private readonly Dictionary<Guid, Quote> _quotes = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public void Save(Quote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        lock (_lock)
        {
            _quotes[quote.QuoteId] = quote;
        }
    }

    /// <inheritdoc/>
    public Quote? GetById(Guid quoteId)
    {
        lock (_lock)
        {
            return _quotes.GetValueOrDefault(quoteId);
        }
    }

    /// <inheritdoc/>
    public bool TryConfirm(Guid quoteId)
    {
        lock (_lock)
        {
            if (!_quotes.TryGetValue(quoteId, out var quote))
                return false;

            // Expire lazily on first access after deadline.
            if (DateTimeOffset.UtcNow > quote.ExpiresAtUtc)
            {
                quote.Status = QuoteStatus.Expired;
                return false;
            }

            if (quote.Status != QuoteStatus.Pending)
                return false;

            quote.Status         = QuoteStatus.Confirmed;
            quote.ConfirmedAtUtc = DateTimeOffset.UtcNow;
            return true;
        }
    }

    /// <inheritdoc/>
    public void MarkExpired(Guid quoteId)
    {
        lock (_lock)
        {
            if (_quotes.TryGetValue(quoteId, out var quote) &&
                quote.Status == QuoteStatus.Pending)
            {
                quote.Status = QuoteStatus.Expired;
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<Quote> GetAll()
    {
        lock (_lock)
        {
            return _quotes.Values.ToList();
        }
    }
}
