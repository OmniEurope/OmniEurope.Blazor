using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OmniEurope.Blazor.Showcase;
using OmniEurope.Blazor.Showcase.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddOmniEuropeBlazor();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ThemeTokenReader>();
builder.Services.AddScoped<ThemeState>();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
