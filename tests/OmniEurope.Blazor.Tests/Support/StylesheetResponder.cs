using System.Net;

namespace OmniEurope.Blazor.Tests;

/// <summary>Answers every request with the same stylesheet, as the host serving the theme tokens would.</summary>
internal sealed class StylesheetResponder(string css) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(css) });
}
