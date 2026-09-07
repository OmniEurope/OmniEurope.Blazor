using OmniEurope.Blazor.AutoSmoke.Client;
using OmniEurope.Blazor.AutoSmoke.Components;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddOmniEuropeBlazor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
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
        "style-src 'self'; script-src 'self' 'wasm-unsafe-eval'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self'; form-action 'self'";
    await next();
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AutoProbe).Assembly);

app.Run();
