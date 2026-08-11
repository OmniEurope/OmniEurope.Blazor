using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OmniEurope.Blazor.WasmSmoke;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddOmniEuropeBlazor();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
