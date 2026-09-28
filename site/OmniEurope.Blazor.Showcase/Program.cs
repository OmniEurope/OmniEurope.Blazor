using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OmniEurope.Blazor.Showcase;
using OmniEurope.Blazor.Showcase.Theming;

// The showcase is written in French and formats its amounts and dates in French, whatever the
// visitor's browser language: without this, WebAssembly takes the browser's culture, or the invariant
// one, and a {0:C0} amount shows "¤" instead of "€". Only the ICU shard holding French is shipped
// (BlazorIcuDataFileName in the project).
var culture = CultureInfo.GetCultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddOmniEuropeBlazor();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ThemeTokenReader>();
builder.Services.AddScoped<ThemeState>();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
