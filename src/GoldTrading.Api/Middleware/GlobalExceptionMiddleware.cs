using System.Text.Json;
using GoldTrading.Application.Constants;
using GoldTrading.Application.Exceptions;

namespace GoldTrading.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a consistent JSON envelope.
/// No internal details are ever exposed to the caller.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain error {Code}: {Message}", ex.ErrorCode, ex.Message);
            await WriteAsync(ctx, StatusCodeFor(ex.ErrorCode), ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(ctx, 500, ErrorCodes.TradeFailed, ErrorMessages.UnexpectedError);
        }
    }

    private static int StatusCodeFor(string code) => code switch
    {
        ErrorCodes.PriceUnavailable      => 503,
        ErrorCodes.QuoteNotFound         => 404,
        ErrorCodes.TradeNotFound         => 404,
        ErrorCodes.QuoteExpired          => 410,
        ErrorCodes.QuoteAlreadyConfirmed => 409,
        ErrorCodes.InsufficientCash      => 422,
        ErrorCodes.InsufficientGold      => 422,
        ErrorCodes.InsufficientInventory => 422,
        ErrorCodes.InvalidTradeAmount    => 400,
        ErrorCodes.InvalidInput          => 400,
        _                                => 500
    };

    private static Task WriteAsync(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new { success = false, code, message }, _json);
        return ctx.Response.WriteAsync(body);
    }
}
