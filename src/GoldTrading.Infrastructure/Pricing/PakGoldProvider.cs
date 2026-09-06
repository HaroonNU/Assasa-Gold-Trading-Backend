using System.Text.Json;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Constants;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoldTrading.Infrastructure.Pricing;

/// <summary>
/// "PakGold" is the assessment brief's label for the primary pricing source.
/// No real PakGold API exists — pakgold.com is an unrelated parked domain
/// (redirects to a HugeDomains sale page), not a live pricing service — so
/// this combines two real, free, keyless APIs to derive the same normalized
/// PKR/gram figure the brief expects:
///   - api.gold-api.com for the USD/troy-oz XAU spot price
///   - open.er-api.com for the USD → PKR exchange rate
/// Both were verified reachable and returning live data. See WhatIDid.md for
/// the reasoning behind this substitution.
/// </summary>
public sealed class PakGoldProvider : IPriceProvider
{
    private const string SpotPriceUrl = "https://api.gold-api.com/price/XAU";
    private const string ExchangeRateUrl = "https://open.er-api.com/v6/latest/USD";

    private readonly HttpClient _http;
    private readonly ILogger<PakGoldProvider> _logger;

    public PakGoldProvider(HttpClient http, ILogger<PakGoldProvider> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<GoldPrice?> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            var spotTask = _http.GetStringAsync(SpotPriceUrl, ct);
            var rateTask = _http.GetStringAsync(ExchangeRateUrl, ct);
            await Task.WhenAll(spotTask, rateTask);

            using var spotDoc = JsonDocument.Parse(await spotTask);
            if (!spotDoc.RootElement.TryGetProperty("price", out var priceEl) ||
                !priceEl.TryGetDecimal(out var usdPerOz) || usdPerOz <= 0)
            {
                _logger.LogWarning("PakGold: unexpected spot price response shape");
                return null;
            }

            using var rateDoc = JsonDocument.Parse(await rateTask);
            if (!rateDoc.RootElement.TryGetProperty("rates", out var ratesEl) ||
                !ratesEl.TryGetProperty("PKR", out var pkrEl) ||
                !pkrEl.TryGetDecimal(out var usdToPkr) || usdToPkr <= 0)
            {
                _logger.LogWarning("PakGold: unexpected exchange rate response shape");
                return null;
            }

            return new GoldPrice
            {
                PricePerGram = Math.Round(usdPerOz * usdToPkr / DomainConstants.TroyOzToGrams, 2),
                Source       = PriceSource.PakGold,
                FetchedAt    = DateTimeOffset.UtcNow,
                IsTrusted    = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PakGold fetch failed");
            return null;
        }
    }
}
