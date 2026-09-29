using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the services of the OmniEurope.Blazor component library in a host.</summary>
public static class OmniEuropeBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the components need: localization, and, scoped to each circuit, the overlay service,
    /// the tooltip interop, the loading state, the breadcrumb service, the Markdown table exporter and a
    /// browser-storage fallback for the data grid state store. Each service is added only when the host
    /// has not registered its own, so calling it after the host's registrations keeps them.
    /// </summary>
    /// <param name="services">The host's service collection; must not be null.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
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
        // Markdown table exports: the texts are the package resources, the generation time comes from
        // the host's TimeProvider when it registers one, the system clock otherwise.
        services.TryAddScoped(provider => new OmniMarkdownTableExporter(
            provider.GetRequiredService<IStringLocalizer<AppStrings>>(),
            provider.GetService<TimeProvider>() ?? TimeProvider.System));
        return services;
    }

    /// <summary>
    /// Lets the host replace any package text from its own resources: a key named
    /// <paramref name="prefix"/> plus the package key (<c>Omni_ConnectionReconnectNow</c>) wins over the
    /// package text, in every culture the host translates; a key the host does not define keeps the
    /// package text. Call after <see cref="AddOmniEuropeBlazor"/>.
    /// </summary>
    /// <typeparam name="THostResource">The marker type of the host's resources that hold the overrides.</typeparam>
    /// <param name="services">The host's service collection; must not be null.</param>
    /// <param name="prefix">The prefix of an overriding key; <c>Omni_</c> by default, must not be null or empty.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
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
    /// <param name="services">The host's service collection; must not be null.</param>
    /// <param name="componentType">The component the preset applies to, or its generic definition.</param>
    /// <param name="name">The name a component asks for through <c>PresetName</c>.</param>
    /// <param name="values">The parameter values of the preset, by parameter name.</param>
    /// <param name="isDefault">Whether the preset also applies to every instance that names no preset.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
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
