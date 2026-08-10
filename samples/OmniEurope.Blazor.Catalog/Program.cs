using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection;
using OmniEurope.Blazor.Catalog.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "OmniEurope.Blazor.Catalog.Keys"));
builder.Services.AddDataProtection().PersistKeysToFileSystem(keyDirectory).SetApplicationName("OmniEurope.Blazor.Catalog");
builder.Services.AddSingleton<CspViolationStore>();

var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
        "style-src 'self'; script-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self' ws: wss:; form-action 'self'; report-uri /csp-report";
    await next();
});
app.UseAntiforgery();
app.MapPost("/csp-report", async (HttpRequest request, CspViolationStore store) =>
{
    using var reader = new StreamReader(request.Body);
    store.Add(await reader.ReadToEndAsync());
    return Results.NoContent();
}).DisableAntiforgery();
app.MapGet("/csp-status", (CspViolationStore store) => store.Violations.IsEmpty
    ? Results.Ok(new { status = "pass", violations = 0 })
    : Results.Json(new { status = "fail", violations = store.Violations.Count, reports = store.Violations }, statusCode: 500));
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public sealed class CspViolationStore
{
    public ConcurrentQueue<string> Violations { get; } = new();
    public void Add(string report) => Violations.Enqueue(report);
}

public partial class Program;
