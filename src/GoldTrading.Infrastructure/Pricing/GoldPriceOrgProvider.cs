using System.Text.Json;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Constants;
using GoldTrading.Domain.Entities;
using GoldTrading.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoldTrading.Infrastructure.Pricing;

public sealed class GoldPriceOrgProvider : IPriceProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GoldPriceOrgProvider> _logger;

    public GoldPriceOrgProvider(HttpClient http, ILogger<GoldPriceOrgProvider> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<GoldPrice?> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync("https://data-asg.goldprice.org/dbXRates/USD", ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("xauPrice", out var xauEl) || !xauEl.TryGetDecimal(out var xauUsd) || xauUsd <= 0)
            { _logger.LogWarning("GoldPriceOrg: missing xauPrice"); return null; }

            if (!root.TryGetProperty("usdPrice", out var usdEl) || !usdEl.TryGetDecimal(out var usdPkr) || usdPkr <= 0)
            { _logger.LogWarning("GoldPriceOrg: missing usdPrice"); return null; }

            return new GoldPrice
            {
                PricePerGram = Math.Round(xauUsd * usdPkr / DomainConstants.TroyOzToGrams, 2),
                Source       = PriceSource.GoldPriceOrg,
                FetchedAt    = DateTimeOffset.UtcNow,
                IsTrusted    = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GoldPriceOrg fetch failed");
            return null;
        }
    }
}
