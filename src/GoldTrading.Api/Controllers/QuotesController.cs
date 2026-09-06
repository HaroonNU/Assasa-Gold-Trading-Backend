using GoldTrading.Application.DTOs;
using GoldTrading.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoldTrading.Api.Controllers;

[ApiController]
[Route("api/quotes")]
public sealed class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ITradeSettlementService _settlementService;

    public QuotesController(IQuoteService quoteService, ITradeSettlementService settlementService)
    {
        _quoteService      = quoteService;
        _settlementService = settlementService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request, CancellationToken ct)
    {
        var result = await _quoteService.CreateAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("{quoteId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid quoteId, CancellationToken ct)
    {
        var result = await _settlementService.ConfirmAsync(quoteId, ct);
        return Ok(result);
    }
}
