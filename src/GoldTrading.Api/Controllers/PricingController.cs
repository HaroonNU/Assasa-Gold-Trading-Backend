using GoldTrading.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldTrading.Api.Controllers;

[ApiController]
[Route("api/pricing")]
public sealed class PricingController : ControllerBase
{
    private readonly PriceQueryService _service;
    public PricingController(PriceQueryService service) => _service = service;

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct) => Ok(await _service.GetCurrentAsync(ct));
}
