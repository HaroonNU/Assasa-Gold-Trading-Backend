using GoldTrading.Application.Constants;
using GoldTrading.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldTrading.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController : ControllerBase
{
    private readonly TradeQueryService _service;
    public TradesController(TradeQueryService service) => _service = service;

    [HttpGet("{tradeId:guid}", Name = ApiMessages.GetTradeRoute)]
    public IActionResult GetById(Guid tradeId) => Ok(_service.GetById(tradeId));

    [HttpGet("{tradeId:guid}/receipt", Name = ApiMessages.GetReceiptRoute)]
    public IActionResult GetReceipt(Guid tradeId) => Ok(_service.GetById(tradeId));

    [HttpGet]
    public IActionResult GetAll() => Ok(_service.GetAll());
}
