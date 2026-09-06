using GoldTrading.Application.DTOs;

namespace GoldTrading.Application.Interfaces;

public interface IQuoteService
{
    Task<QuoteResponse> CreateAsync(CreateQuoteRequest request, CancellationToken ct = default);
}
