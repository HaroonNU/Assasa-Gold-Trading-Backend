using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoldTrading.IntegrationTests;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

internal sealed record ErrorEnvelope(bool Success, string Code, string Message);
