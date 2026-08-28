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
        return services;
    }
}
