namespace OmniEurope.Blazor.Components;

/// <summary>
/// The optional host services read by both component bases, <see cref="OmniComponentBase"/> and
/// <see cref="OmniInputBase{TValue}"/>, which derive from different framework bases and so cannot share
/// a member.
/// </summary>
internal static class PackageServices
{
    /// <summary>
    /// The host's registered <see cref="TimeProvider"/>, the system clock when it registers none. Resolved
    /// on each call, so a test swaps it in the container.
    /// </summary>
    /// <param name="services">The component's service provider.</param>
    internal static TimeProvider Clock(IServiceProvider services) =>
        services.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
}
