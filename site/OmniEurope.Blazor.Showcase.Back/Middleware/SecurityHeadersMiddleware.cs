namespace OmniEurope.Blazor.Showcase.Back.Middleware;

/// <summary>
/// The response headers of an API that serves JSON only: nothing it returns may be framed, sniffed,
/// cached by a shared cache, or run as a page.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        headers.CacheControl = "no-store";
        return next(context);
    }
}
