using GoldTrading.Application.Constants;
using GoldTrading.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldTrading.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly AccountService _service;
    public AccountController(AccountService service) => _service = service;

    [HttpGet("balances", Name = ApiMessages.GetBalancesRoute)]
    public IActionResult GetBalances() => Ok(_service.GetBalances());
}
