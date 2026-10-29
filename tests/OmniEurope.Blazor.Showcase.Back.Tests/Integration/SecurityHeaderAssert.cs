namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>The security headers every API response carries, success or refusal (audit A360-004).</summary>
internal static class SecurityHeaderAssert
{
    public static void Carried(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.False(response.Headers.Contains("Server"));
    }
}
