namespace GoldTrading.Application.Interfaces;

/// <summary>
/// A process-wide serialisation point for the confirm → balance-debit → trade-save
/// sequence.  Only one settlement can run at a time, preventing two concurrent
/// confirmation requests for the same quote from both succeeding.
///
/// Implemented as a thin wrapper around a <see cref="SemaphoreSlim"/>(1,1) so
/// the lock scope is explicit and auditable at the call site.
/// </summary>
public interface ITradeSettlementLock
{
    /// <summary>
    /// Acquires the settlement lock, executes <paramref name="settlement"/>,
    /// then releases the lock — even if the delegate throws.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<Task<T>> settlement, CancellationToken ct = default);
}
