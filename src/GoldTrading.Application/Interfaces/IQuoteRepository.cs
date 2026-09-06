using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;

namespace GoldTrading.Application.Interfaces;

/// <summary>
/// Stores and retrieves quotes.  Quote status transitions are the only
/// mutations allowed after initial save.
/// </summary>
public interface IQuoteRepository
{
    void Save(Quote quote);

    Quote? GetById(Guid quoteId);

    /// <summary>
    /// Atomically transitions a Pending quote to Confirmed.
    /// Returns <c>false</c> if the quote is not found, already Confirmed,
    /// already Expired, or has passed its ExpiresAt timestamp.
    /// This is the idempotency gate — only one caller can win.
    /// </summary>
    bool TryConfirm(Guid quoteId);

    /// <summary>Marks a quote Expired without confirming it.</summary>
    void MarkExpired(Guid quoteId);

    IReadOnlyList<Quote> GetAll();
}
