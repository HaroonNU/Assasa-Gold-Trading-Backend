using GoldTrading.Application.DTOs;
using GoldTrading.Application.Exceptions;
using GoldTrading.Application.Interfaces;
using GoldTrading.Domain.Entities;

namespace GoldTrading.Application.Services;

public sealed class TradeQueryService
{
    private readonly ITradeRepository _repo;
    public TradeQueryService(ITradeRepository repo) => _repo = repo;

    public TradeReceiptResponse GetById(Guid tradeId)
    {
        var trade = _repo.GetById(tradeId) ?? throw new TradeNotFoundException();
        return ToReceipt(trade);
    }

    public IReadOnlyList<TradeReceiptResponse> GetAll() =>
        _repo.GetAll().Select(ToReceipt).ToList();

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
