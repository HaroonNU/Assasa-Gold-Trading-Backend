namespace GoldTrading.Application.Constants;

/// <summary>
/// Fixed values used only by demo/stress-test endpoints (Prompt 12).
/// Never referenced by core pricing, quote, or trade logic.
/// </summary>
public static class DemoConstants
{
    /// <summary>PKR balance applied by the "set low PKR balance" demo action.</summary>
    public const decimal LowCustomerPkrBalance = 50m;

    /// <summary>Gold grams applied by the "set low customer gold" demo action.</summary>
    public const decimal LowCustomerGoldGrams = 0.0001m;

    /// <summary>Gold grams applied by the "set low platform inventory" demo action.</summary>
    public const decimal LowPlatformGoldGrams = 0.0001m;

    /// <summary>
    /// Artificial market price used by the "trigger guardrail" demo action.
    /// Deliberately far below any realistic guardrail so Buy = max(price × markup, guardrail)
    /// always resolves to the guardrail floor.
    /// </summary>
    public const decimal GuardrailTriggerPricePerGram = 1m;
}
