using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Wiring shared by the preference tests: the doubles in the container and the record of the changes raised.</summary>
internal static class PreferenceTestSetup
{
    /// <summary>Registers the browser, the clock, the logger and (when given) the store the preferences resolve.</summary>
    public static IServiceCollection AddPreferenceDoubles(
        this IServiceCollection services, IJSRuntime browser, TimeProvider time, ILogger<OmniPreferences> logger, IOmniPreferenceStore? store)
    {
        services.AddSingleton(browser);
        services.AddSingleton(time);
        services.AddSingleton(logger);
        if (store is not null)
        {
            services.AddSingleton(store);
        }

        return services;
    }

    /// <summary>Adds every change the preferences raise to <paramref name="changes"/>, under its lock.</summary>
    public static OmniPreferences RecordChangesInto(this OmniPreferences preferences, List<OmniPreferenceChangedEventArgs> changes)
    {
        preferences.Changed += change =>
        {
            lock (changes)
            {
                changes.Add(change);
            }

            return Task.CompletedTask;
        };
        return preferences;
    }
}
