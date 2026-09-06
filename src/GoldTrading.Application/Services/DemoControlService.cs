using GoldTrading.Application.Configuration;
using GoldTrading.Application.Constants;
using GoldTrading.Application.DTOs;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;
using Microsoft.Extensions.Options;

namespace GoldTrading.Application.Services;

/// <summary>
/// Backs the demo/stress-test endpoints described in Prompt 12. Deliberately
/// kept separate from <see cref="QuoteService"/> and <see cref="TradeSettlementService"/> —
/// it never contains pricing or settlement logic itself, only orchestrates the
/// demo-only seams those services expose (<see cref="IDemoPricingState"/>,
/// <see cref="IPriceCache.Invalidate"/>, <see cref="IAccountStateRepository.SetBalances"/>).
/// </summary>
public sealed class DemoControlService
{
    private readonly IAccountStateRepository _accountRepo;
    private readonly IQuoteRepository _quoteRepo;
    private readonly IPriceCache _priceCache;
    private readonly IDemoPricingState _demoState;
    private readonly SeedDataOptions _seed;

    public DemoControlService(
        IAccountStateRepository accountRepo,
        IQuoteRepository quoteRepo,
        IPriceCache priceCache,
        IDemoPricingState demoState,
        IOptions<SeedDataOptions> seed)
    {
        _accountRepo = accountRepo;
        _quoteRepo   = quoteRepo;
        _priceCache  = priceCache;
        _demoState   = demoState;
        _seed        = seed.Value;
    }

    public DemoActionResponse SimulatePrimaryFailure(bool enabled)
    {
        _demoState.SetPrimaryFailure(enabled);
        _priceCache.Invalidate();
        return Applied();
    }

    public DemoActionResponse SimulateAllUnavailable(bool enabled)
    {
        _demoState.SetAllUnavailable(enabled);
        _priceCache.Invalidate();
        return Applied();
    }

    public DemoActionResponse TriggerGuardrailScenario()
    {
        _demoState.SetOverridePrice(DemoConstants.GuardrailTriggerPricePerGram);
        _priceCache.Invalidate();
        return Applied();
    }

    public DemoActionResponse ForceExpireQuote(Guid quoteId)
    {
        _quoteRepo.MarkExpired(quoteId);
        return Applied();
    }

    public DemoActionResponse SetLowPkrBalance()
    {
        _accountRepo.SetBalances(DemoConstants.LowCustomerPkrBalance, null, null);
        return Applied();
    }

    public DemoActionResponse SetLowCustomerGold()
    {
        _accountRepo.SetBalances(null, DemoConstants.LowCustomerGoldGrams, null);
        return Applied();
    }

    public DemoActionResponse SetLowPlatformInventory()
    {
        _accountRepo.SetBalances(null, null, DemoConstants.LowPlatformGoldGrams);
        return Applied();
    }

    public DemoActionResponse ResetDemoState()
    {
        _demoState.Reset();
        _priceCache.Invalidate();
        _accountRepo.Reset(new AccountState
        {
            CustomerPkr       = _seed.CustomerPkr,
            CustomerGoldGrams = _seed.CustomerGoldGrams,
            PlatformGoldGrams = _seed.PlatformGoldGrams
        });
        return new DemoActionResponse(true, SuccessMessages.DemoStateReset);
    }

    private static DemoActionResponse Applied() => new(true, SuccessMessages.DemoActionApplied);
}
