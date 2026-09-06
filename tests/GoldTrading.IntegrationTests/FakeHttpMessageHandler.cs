using System.Net;
using System.Text;

namespace GoldTrading.IntegrationTests;

/// <summary>
/// Returns a canned JSON response for every request, so <c>GoldPriceOrgProvider</c>
/// can be exercised end-to-end (including its real JSON parsing) without ever
/// calling the real external API.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _json;
    private readonly HttpStatusCode _status;

    public FakeHttpMessageHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _json   = json;
        _status = status;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(_status)
        {
            Content = new StringContent(_json, Encoding.UTF8, "application/json")
        });
}

/// <summary>
/// Routes to a canned JSON response by matching a substring of the request
/// URL. PakGoldProvider calls two different real APIs (spot price + exchange
/// rate) over the same HttpClient, so a single canned response isn't enough —
/// this picks the right one per request.
/// </summary>
internal sealed class RoutingFakeHttpMessageHandler : HttpMessageHandler
{
    private readonly (string UrlContains, string Json)[] _routes;

    public RoutingFakeHttpMessageHandler(params (string UrlContains, string Json)[] routes) => _routes = routes;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        var match = _routes.FirstOrDefault(r => url.Contains(r.UrlContains));
        var json = match.Json ?? throw new InvalidOperationException($"No fake route configured for {url}");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}
