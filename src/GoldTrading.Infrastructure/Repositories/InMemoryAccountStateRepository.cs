using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;

namespace GoldTrading.Infrastructure.Repositories;

/// <summary>
/// Holds the single customer account state for the lifetime of the process.
///
/// Synchronisation strategy
/// ────────────────────────
/// A dedicated <c>object _lock</c> guards every read and write of the three
/// balance fields.  The lock scope is intentionally narrow — it covers only
/// the balance struct, not the broader settlement sequence.  The broader
/// sequence (quote confirmation + balance debit + trade save) is serialised
/// by <see cref="TradeSettlementLock"/> one level up in the call stack.
///
/// Why Singleton: the account state IS the application state.  A Scoped or
/// Transient lifetime would create a fresh zeroed object per request, losing
/// all balance history.
/// </summary>
public sealed class InMemoryAccountStateRepository : IAccountStateRepository
{
    private readonly AccountState _state;
    private readonly object _lock = new();

    public InMemoryAccountStateRepository(AccountState seedState)
    {
        _state = seedState;
    }

    /// <inheritdoc/>
    public AccountState GetSnapshot()
    {
        lock (_lock)
        {
            // Return a value copy so callers cannot mutate internal state.
            return new AccountState
            {
                CustomerPkr       = _state.CustomerPkr,
                CustomerGoldGrams = _state.CustomerGoldGrams,
                PlatformGoldGrams = _state.PlatformGoldGrams
            };
        }
    }

    /// <inheritdoc/>
    public AccountState ApplyTrade(TradeDirection direction, decimal grams, decimal pkr)
    {
        lock (_lock)
        {
            if (direction == TradeDirection.Buy)
            {
                _state.CustomerPkr       -= pkr;
                _state.CustomerGoldGrams += grams;
                _state.PlatformGoldGrams -= grams;
            }
            else
            {
                _state.CustomerGoldGrams -= grams;
                _state.CustomerPkr       += pkr;
                _state.PlatformGoldGrams += grams;
            }

            return new AccountState
            {
                CustomerPkr       = _state.CustomerPkr,
                CustomerGoldGrams = _state.CustomerGoldGrams,
                PlatformGoldGrams = _state.PlatformGoldGrams
            };
        }
    }

    /// <inheritdoc/>
    public void SetBalances(decimal? customerPkr, decimal? customerGoldGrams, decimal? platformGoldGrams)
    {
        lock (_lock)
        {
            if (customerPkr is { } pkr) _state.CustomerPkr = pkr;
            if (customerGoldGrams is { } gold) _state.CustomerGoldGrams = gold;
            if (platformGoldGrams is { } inventory) _state.PlatformGoldGrams = inventory;
        }
    }

    /// <inheritdoc/>
    public void Reset(AccountState seed)
    {
        lock (_lock)
        {
            _state.CustomerPkr       = seed.CustomerPkr;
            _state.CustomerGoldGrams = seed.CustomerGoldGrams;
            _state.PlatformGoldGrams = seed.PlatformGoldGrams;
        }
    }
}
