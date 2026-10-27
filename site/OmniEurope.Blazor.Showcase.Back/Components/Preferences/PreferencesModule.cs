using System.Diagnostics.CodeAnalysis;
using OmniEurope.Blazor.Showcase.Back.Components.Shared;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

[ExcludeFromCodeCoverage]
public static class PreferencesModule
{
    public static IServiceCollection AddPreferencesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PreferenceLimitsOptions>().Bind(configuration.GetSection(PreferenceLimitsOptions.Section))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<VisitorRetentionOptions>().Bind(configuration.GetSection(VisitorRetentionOptions.Section))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IVisitorPreferenceRepository, VisitorPreferenceRepository>();
        services.AddScoped<IVisitorPreferenceService, VisitorPreferenceService>();
        services.AddSingleton<IVisitorCreationGate, VisitorCreationGate>();
        services.AddHostedService<VisitorRetentionService>();
        return services;
    }
}
