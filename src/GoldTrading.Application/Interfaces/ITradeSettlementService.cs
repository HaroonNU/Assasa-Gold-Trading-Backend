using GoldTrading.Application.DTOs;

namespace GoldTrading.Application.Interfaces;

public interface ITradeSettlementService
{
    Task<TradeReceiptResponse> ConfirmAsync(Guid quoteId, CancellationToken ct = default);
}
