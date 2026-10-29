namespace OmniEurope.Blazor.Showcase.Back.Middleware;

/// <summary>
/// The response headers of an API that serves JSON only: nothing it returns may be framed, sniffed,
/// cached by a shared cache, or run as a page. They are written when the response starts, so a
/// refusal converted from an exception (which clears the response) carries them too.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.OnStarting(static state =>
        {
            Apply(((HttpContext)state).Response.Headers);
            return Task.CompletedTask;
        }, context);
        return next(context);
    }

    private static void Apply(IHeaderDictionary headers)
    {
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        headers.CacheControl = "no-store";
    }
}
