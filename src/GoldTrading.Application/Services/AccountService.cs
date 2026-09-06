using GoldTrading.Application.DTOs;
using GoldTrading.Application.Interfaces;

namespace GoldTrading.Application.Services;

public sealed class AccountService
{
    private readonly IAccountStateRepository _repo;
    public AccountService(IAccountStateRepository repo) => _repo = repo;

    public AccountBalancesResponse GetBalances()
    {
        var s = _repo.GetSnapshot();
        return new AccountBalancesResponse(s.CustomerPkr, s.CustomerGoldGrams, s.PlatformGoldGrams);
    }
}
