using GoldTrading.Application.Configuration;
using GoldTrading.Application.Constants;
using GoldTrading.Application.DTOs;
using GoldTrading.Application.Exceptions;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Constants;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using GoldTrading.Domain.Rules;
using Microsoft.Extensions.Options;

namespace GoldTrading.Application.Services;

public sealed class QuoteService : IQuoteService
{
    private readonly IPriceCache _priceCache;
    private readonly IQuoteRepository _quoteRepo;
    private readonly PricingOptions _pricing;
    private readonly QuoteOptions _quote;
    private readonly TradingOptions _trading;

    public QuoteService(
        IPriceCache priceCache,
        IQuoteRepository quoteRepo,
        IOptions<PricingOptions> pricing,
        IOptions<QuoteOptions> quote,
        IOptions<TradingOptions> trading)
    {
        _priceCache = priceCache;
        _quoteRepo  = quoteRepo;
        _pricing    = pricing.Value;
        _quote      = quote.Value;
        _trading    = trading.Value;
    }

    public async Task<QuoteResponse> CreateAsync(CreateQuoteRequest request, CancellationToken ct = default)
    {
        ValidateAmount(request);

        var goldPrice = await _priceCache.GetAsync(ct);
        if (!goldPrice.IsTrusted)
            throw new PriceUnavailableException();

        var customerPrice = PricingCalculator.CustomerPricePerGram(
            request.TradeType == TradeType.Buy ? TradeDirection.Buy : TradeDirection.Sell,
            goldPrice.PricePerGram,
            _pricing);

        var (grams, pkr) = Resolve(request, customerPrice);

        var now   = DateTimeOffset.UtcNow;
        var quote = new Quote
        {
            TradeType            = request.TradeType,
            GoldAmountInGrams    = grams,
            PkrAmount            = pkr,
            LockedMarketPrice    = goldPrice.PricePerGram,
            CustomerPricePerGram = customerPrice,
            PricingSource        = goldPrice.Source,
            PriceRetrievedAtUtc  = goldPrice.FetchedAt,
            CreatedAtUtc         = now,
            ExpiresAtUtc         = now.AddSeconds(_quote.DurationSeconds)
        };

        _quoteRepo.Save(quote);

        return ToResponse(quote);
    }

    // ── private helpers ───────────────────────────────────────────────────────

    private void ValidateAmount(CreateQuoteRequest request)
    {
        if (request.Amount <= 0)
            throw new InvalidTradeAmountException(ValidationMessages.AmountMustBePositive);

        if (request.InputType == InputType.Gold)
        {
            if (request.Amount < _trading.MinGoldGramsPerTrade)
                throw new InvalidTradeAmountException(ValidationMessages.GoldBelowMinimum);
            if (request.Amount > _trading.MaxGoldGramsPerTrade)
                throw new InvalidTradeAmountException(ValidationMessages.GoldAboveMaximum);
        }
        else
        {
            if (request.Amount < _trading.MinPkrPerTrade)
                throw new InvalidTradeAmountException(ValidationMessages.PkrBelowMinimum);
            if (request.Amount > _trading.MaxPkrPerTrade)
                throw new InvalidTradeAmountException(ValidationMessages.PkrAboveMaximum);
        }
    }

    private static (decimal grams, decimal pkr) Resolve(CreateQuoteRequest request, decimal customerPrice) =>
        request.InputType == InputType.Gold
            ? (Math.Round(request.Amount, DomainConstants.GoldGramPrecision),
               Math.Round(request.Amount * customerPrice, DomainConstants.PkrPrecision))
            : (Math.Round(request.Amount / customerPrice, DomainConstants.GoldGramPrecision),
               Math.Round(request.Amount, DomainConstants.PkrPrecision));

    private static QuoteResponse ToResponse(Quote q) => new(
        q.QuoteId,
        q.TradeType,
        q.LockedMarketPrice,
        q.CustomerPricePerGram,
        q.PkrAmount,
        q.GoldAmountInGrams,
        q.PricingSource.ToString(),
        q.PriceRetrievedAtUtc,
        q.CreatedAtUtc,
        q.ExpiresAtUtc,
        (int)(q.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds
    );
}
