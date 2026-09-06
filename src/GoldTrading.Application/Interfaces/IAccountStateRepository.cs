using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;

namespace GoldTrading.Application.Interfaces;

/// <summary>
/// Owns the single in-memory account state: customer PKR, customer gold, platform inventory.
/// All mutations must be atomic — implementations are responsible for synchronisation.
/// </summary>
public interface IAccountStateRepository
{
    /// <summary>Returns a point-in-time snapshot of the current account state.</summary>
    AccountState GetSnapshot();

    /// <summary>
    /// Atomically applies a settled trade to all three balances and returns
    /// the post-trade snapshot.  Called only after the quote has been
    /// exclusively locked by <see cref="ITradeSettlementLock"/>.
    /// </summary>
    AccountState ApplyTrade(TradeDirection direction, decimal grams, decimal pkr);

    /// <summary>
    /// Demo-only override: directly sets any provided balance, bypassing the
    /// normal trade flow. Null parameters are left unchanged. Used exclusively
    /// by demo endpoints to stage stress-test scenarios.
    /// </summary>
    void SetBalances(decimal? customerPkr, decimal? customerGoldGrams, decimal? platformGoldGrams);

    /// <summary>Restores all three balances to the given seed values. Used by the demo reset endpoint.</summary>
    void Reset(AccountState seed);
}
