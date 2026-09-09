using Microsoft.Extensions.DependencyInjection.Extensions;
using OmniEurope.Blazor.Components;

namespace Microsoft.Extensions.DependencyInjection;

public static class OmniEuropeBlazorServiceCollectionExtensions
{
    public static IServiceCollection AddOmniEuropeBlazor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddLocalization();
        // TryAdd: a host that already registered its own IOmniDataGridStateStore (a database-backed
        // one, say) keeps it; this is only the fallback for grids that opt into StateKey without
        // supplying one.
        services.TryAddScoped<IOmniDataGridStateStore, OmniLocalStorageDataGridStateStore>();
        // Registered so the overlay service can be injected anywhere, dialog content included.
        // OmniComponentsHost cascades its own instance to ChildContent only, and the dialog host it
        // renders sits outside that cascade: content opened through OpenDialog cannot receive the
        // service as a cascading parameter, so it had no way to close the dialog it lives in.
        services.TryAddScoped<OmniOverlayService>();
        // Scoped: the tooltip listeners live on the document, and each circuit owns the document it
        // rendered into.
        services.TryAddScoped<OmniTooltipInterop>();
        // Scoped for the same reason: the loading bar reflects one circuit's work, and a singleton
        // would have one user's page load light another user's bar.
        services.TryAddScoped<OmniLoadingState>();
        return services;
    }
}
