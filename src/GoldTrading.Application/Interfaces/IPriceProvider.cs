using GoldTrading.Domain.Entities;

namespace GoldTrading.Application.Interfaces;

public interface IPriceProvider
{
    Task<GoldPrice?> FetchAsync(CancellationToken ct = default);
}
