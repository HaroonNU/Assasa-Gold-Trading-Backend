using GoldTrading.Application.Interfaces;

namespace GoldTrading.Infrastructure.Repositories;

/// <summary>
/// Process-wide serialisation point for the confirm → debit → save sequence.
///
/// Why a dedicated lock instead of relying on per-repository locks alone
/// ──────────────────────────────────────────────────────────────────────
/// The settlement sequence spans three repositories (quote, account, trade).
/// Each repository has its own fine-grained lock protecting its own data
/// structure.  Without a coarser serialisation point, two concurrent requests
/// for the same quote could both pass the balance-check, both call TryConfirm,
/// and — because TryConfirm is atomic — only one would win the quote lock.
/// That is already safe, but the losing request would then need to unwind.
///
/// The settlement lock makes the entire sequence atomic at the application
/// level: only one goroutine enters the confirm+debit+save block at a time.
/// This eliminates the need for compensating rollback logic and makes the
/// flow easy to reason about.
///
/// Implementation: <see cref="SemaphoreSlim"/>(1,1) — async-compatible,
/// no thread-affinity requirement, zero external dependencies.
///
/// Why Singleton: the lock must be the same instance for every request.
/// A Scoped or Transient lifetime would create a new semaphore per request,
/// providing no mutual exclusion at all.
/// </summary>
public sealed class TradeSettlementLock : ITradeSettlementLock, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> settlement, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            return await settlement();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose() => _semaphore.Dispose();
}
