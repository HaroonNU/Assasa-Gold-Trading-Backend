using GoldTrading.Application.Constants;
using GoldTrading.Application.DTOs;
using GoldTrading.Application.Exceptions;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoldTrading.Application.Services;

/// <summary>
/// Executes the full confirm → validate → settle → record sequence.
///
/// Concurrency safety
/// ──────────────────
/// Every call enters through ITradeSettlementLock.ExecuteAsync, which wraps a
/// SemaphoreSlim(1,1).  Only one settlement runs at a time in this process.
///
/// Inside the lock the sequence is:
///   1. Load quote — fail fast if not found.
///   2. If already Confirmed → return the existing trade (idempotent).
///   3. Check expiry — reject if past ExpiresAtUtc.
///   4. Snapshot balances and validate sufficiency.
///   5. Call IQuoteRepository.TryConfirm — the atomic status gate.
///      If it returns false a concurrent request already won; return that trade.
///   6. Apply balance changes.
///   7. Save trade.
///
/// Steps 5-7 are inside the settlement lock so they are never interleaved
/// with another settlement.  If anything in steps 6-7 throws, the quote is
/// already Confirmed but no trade exists — the lock prevents a second attempt
/// from re-entering, so the state is inconsistent only in a catastrophic
/// failure scenario (e.g. process crash), which is acceptable for a
/// single-instance in-memory demo.
/// </summary>
public sealed class TradeSettlementService : ITradeSettlementService
{
    private readonly ITradeSettlementLock _lock;
    private readonly IQuoteRepository _quoteRepo;
    private readonly ITradeRepository _tradeRepo;
    private readonly IAccountStateRepository _accountRepo;
    private readonly ILogger<TradeSettlementService> _logger;

    public TradeSettlementService(
        ITradeSettlementLock settlementLock,
        IQuoteRepository quoteRepo,
        ITradeRepository tradeRepo,
        IAccountStateRepository accountRepo,
        ILogger<TradeSettlementService> logger)
    {
        _lock        = settlementLock;
        _quoteRepo   = quoteRepo;
        _tradeRepo   = tradeRepo;
        _accountRepo = accountRepo;
        _logger      = logger;
    }

    public Task<TradeReceiptResponse> ConfirmAsync(Guid quoteId, CancellationToken ct = default) =>
        _lock.ExecuteAsync(() => SettleAsync(quoteId), ct);

    // ── private ───────────────────────────────────────────────────────────────

    private Task<TradeReceiptResponse> SettleAsync(Guid quoteId)
    {
        // 1. Load quote
        var quote = _quoteRepo.GetById(quoteId)
            ?? throw new QuoteNotFoundException();

        // 2. Already confirmed — idempotent return of existing trade
        if (quote.Status == QuoteStatus.Confirmed)
        {
            var existing = _tradeRepo.GetByQuoteId(quoteId)
                ?? throw new QuoteAlreadyConfirmedException(); // trade missing — should never happen
            return Task.FromResult(ToReceipt(existing));
        }

        // 3. Expiry check
        if (DateTimeOffset.UtcNow > quote.ExpiresAtUtc)
        {
            _quoteRepo.MarkExpired(quoteId);
            throw new QuoteExpiredException();
        }

        // 4. Balance validation (read snapshot before mutating anything)
        var balances = _accountRepo.GetSnapshot();
        ValidateBalances(quote, balances);

        // 5. Atomic status gate — only one caller wins
        if (!_quoteRepo.TryConfirm(quoteId))
        {
            // Lost the race — return the trade the winner created
            var raceWinner = _tradeRepo.GetByQuoteId(quoteId);
            if (raceWinner is not null)
                return Task.FromResult(ToReceipt(raceWinner));

            // Quote was expired by TryConfirm
            throw new QuoteExpiredException();
        }

        // 6. Apply balance changes
        var direction = quote.TradeType == TradeType.Buy ? TradeDirection.Buy : TradeDirection.Sell;
        var after     = _accountRepo.ApplyTrade(direction, quote.GoldAmountInGrams, quote.PkrAmount);

        // 7. Save trade
        var trade = new Trade
        {
            QuoteId           = quote.QuoteId,
            TradeType         = quote.TradeType,
            GoldAmountInGrams = quote.GoldAmountInGrams,
            PkrAmount         = quote.PkrAmount,
            PricePerGram      = quote.CustomerPricePerGram,
            PricingSource     = quote.PricingSource,
            BalanceSnapshot   = new BalanceSnapshot
            {
                CustomerPkrBalance          = after.CustomerPkr,
                CustomerGoldGrams           = after.CustomerGoldGrams,
                PlatformGoldInventoryGrams  = after.PlatformGoldGrams
            }
        };

        _tradeRepo.Save(trade);

        _logger.LogInformation(ApiMessages.TradeExecutedLog,
            trade.TradeId, trade.TradeType, trade.GoldAmountInGrams, trade.PkrAmount);

        return Task.FromResult(ToReceipt(trade));
    }

    private static void ValidateBalances(Quote quote, AccountState balances)
    {
        if (quote.TradeType == TradeType.Buy)
        {
            if (balances.CustomerPkr < quote.PkrAmount)
                throw new InsufficientCashException();
            if (balances.PlatformGoldGrams < quote.GoldAmountInGrams)
                throw new InsufficientInventoryException();
        }
        else
        {
            if (balances.CustomerGoldGrams < quote.GoldAmountInGrams)
                throw new InsufficientGoldException();
        }
    }

    private static TradeReceiptResponse ToReceipt(Trade t) => new(
        t.TradeId,
        t.QuoteId,
        t.TradeType,
        t.PkrAmount,
        t.GoldAmountInGrams,
        t.PricePerGram,
        t.PricingSource.ToString(),
        t.CompletedAtUtc,
        t.BalanceSnapshot.CustomerPkrBalance,
        t.BalanceSnapshot.CustomerGoldGrams,
        t.BalanceSnapshot.PlatformGoldInventoryGrams
    );
}
