using GoldTrading.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoldTrading.Api.Controllers;

/// <summary>
/// Reviewer-facing stress-test controls (Prompt 12). Lets reviewers exercise
/// failure, expiry, low-balance and guardrail scenarios without redeploying
/// or editing configuration. Intentionally thin — all behaviour lives in
/// <see cref="DemoControlService"/>, kept separate from core trading logic.
/// </summary>
[ApiController]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    private readonly DemoControlService _demo;
    public DemoController(DemoControlService demo) => _demo = demo;

    [HttpPost("pricing/simulate-primary-failure")]
    public IActionResult SimulatePrimaryFailure([FromQuery] bool enabled = true) =>
        Ok(_demo.SimulatePrimaryFailure(enabled));

    [HttpPost("pricing/simulate-all-unavailable")]
    public IActionResult SimulateAllUnavailable([FromQuery] bool enabled = true) =>
        Ok(_demo.SimulateAllUnavailable(enabled));

    [HttpPost("pricing/guardrail-scenario")]
    public IActionResult TriggerGuardrailScenario() =>
        Ok(_demo.TriggerGuardrailScenario());

    [HttpPost("quotes/{quoteId:guid}/force-expire")]
    public IActionResult ForceExpireQuote(Guid quoteId) =>
        Ok(_demo.ForceExpireQuote(quoteId));

    [HttpPost("balances/low-pkr")]
    public IActionResult SetLowPkrBalance() =>
        Ok(_demo.SetLowPkrBalance());

    [HttpPost("balances/low-gold")]
    public IActionResult SetLowCustomerGold() =>
        Ok(_demo.SetLowCustomerGold());

    [HttpPost("balances/low-inventory")]
    public IActionResult SetLowPlatformInventory() =>
        Ok(_demo.SetLowPlatformInventory());

    [HttpPost("reset")]
    public IActionResult Reset() =>
        Ok(_demo.ResetDemoState());
}
