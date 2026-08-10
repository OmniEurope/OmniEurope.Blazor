using Microsoft.AspNetCore.DataProtection;
using OmniEurope.Blazor.AutoSmoke.Client;
using OmniEurope.Blazor.AutoSmoke.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "OmniEurope.Blazor.AutoSmoke.Keys"));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keyDirectory)
    .SetApplicationName("OmniEurope.Blazor.AutoSmoke");
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
        "style-src 'self'; script-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self' ws: wss:; form-action 'self'";
    await next();
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AutoProbe).Assembly);

app.Run();
