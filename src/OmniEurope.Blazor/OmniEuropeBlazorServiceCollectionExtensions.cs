using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

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
        // Scoped: the trail follows one circuit's navigation. Its route fallback comes from the
        // host's IOmniBreadcrumbResolver when one is registered, and is empty otherwise.
        services.TryAddScoped<OmniBreadcrumbService>();
        return services;
    }

    /// <summary>
    /// Lets the host replace any package text from its own resources: a key named
    /// <paramref name="prefix"/> plus the package key (<c>Omni_ConnectionReconnectNow</c>) wins over the
    /// package text, in every culture the host translates; a key the host does not define keeps the
    /// package text. Call after <see cref="AddOmniEuropeBlazor"/>.
    /// </summary>
    public static IServiceCollection AddOmniEuropeTextOverrides<THostResource>(this IServiceCollection services, string prefix = "Omni_")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        services.AddLocalization();
        // A closed registration wins over the open IStringLocalizer<> one, so every package component
        // that injects IStringLocalizer<AppStrings> reads through the override.
        services.Replace(ServiceDescriptor.Transient<IStringLocalizer<AppStrings>>(provider =>
        {
            var factory = provider.GetRequiredService<IStringLocalizerFactory>();
            return new OmniTextOverrideLocalizer(
                factory.Create(typeof(AppStrings)),
                provider.GetRequiredService<IStringLocalizer<THostResource>>(),
                prefix);
        }));
        return services;
    }

    /// <summary>
    /// Registers a preset: <paramref name="values"/> (parameter name to value) applied to every
    /// <paramref name="componentType"/> that asks for <paramref name="name"/> through <c>PresetName</c>,
    /// or to all of them when <paramref name="isDefault"/>. A generic component is registered by its
    /// definition (<c>typeof(OmniDataGrid&lt;&gt;)</c>). Explicit parameters always win. An unknown
    /// parameter, a wrongly typed value or a duplicate name or default throws here, at startup.
    /// </summary>
    public static IServiceCollection AddOmniEuropePreset(this IServiceCollection services, Type componentType, string name,
        IReadOnlyDictionary<string, object?> values, bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        var registry = services.FirstOrDefault(d => d.ServiceType == typeof(OmniPresetRegistry))?.ImplementationInstance as OmniPresetRegistry;
        if (registry is null)
        {
            registry = new OmniPresetRegistry();
            services.AddSingleton(registry);
        }
        registry.Add(componentType, name, values, isDefault);
        return services;
    }
}
