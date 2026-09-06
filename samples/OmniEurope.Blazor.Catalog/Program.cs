using OmniEurope.Blazor.Catalog;
using OmniEurope.Blazor.Catalog.Components;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

const int MaxCspReportCharacters = 16 * 1024;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Services.AddOmniEuropeBlazor();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<CspViolationStore>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("fr"), new CultureInfo("en") };
    options.DefaultRequestCulture = new RequestCulture("fr");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseRequestLocalization();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
        "style-src 'self'; script-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self'; form-action 'self'; report-uri /csp-report";
    await next();
});
app.UseAntiforgery();
app.MapPost("/csp-report", async (HttpRequest request, CspViolationStore store) =>
{
    var mediaType = request.ContentType?.Split(';', 2)[0].Trim();
    if (mediaType is not ("application/csp-report" or "application/reports+json" or "application/json"))
    {
        return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
    }

    if (request.ContentLength is > MaxCspReportCharacters)
    {
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    var report = await ReadCspReportAsync(request, MaxCspReportCharacters);
    if (report is null)
    {
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    store.Add(report);
    return Results.NoContent();
}).DisableAntiforgery();
app.MapGet("/csp-status", (CspViolationStore store) => store.Count == 0
    ? Results.Ok(new { status = "pass", violations = 0 })
    : Results.Json(new { status = "fail", violations = store.Count }, statusCode: 500));
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

static async Task<string?> ReadCspReportAsync(HttpRequest request, int maximumCharacters)
{
    using var reader = new StreamReader(request.Body, leaveOpen: true);
    var buffer = new char[maximumCharacters + 1];
    var length = 0;

    while (length < buffer.Length)
    {
        var read = await reader.ReadAsync(buffer.AsMemory(length, buffer.Length - length), request.HttpContext.RequestAborted);
        if (read == 0)
        {
            break;
        }

        length += read;
    }

    return length > maximumCharacters ? null : new string(buffer, 0, length);
}
