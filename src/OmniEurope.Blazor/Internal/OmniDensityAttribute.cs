using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The value of <c>data-omni-density</c> for a component that carries a density of its own: the
/// density's name, or null so the attribute is left out and the component inherits the density of
/// its scope or section.
/// </summary>
internal static class OmniDensityAttribute
{
    internal static string? Of(OmniDensity? density) => density?.ToString().ToLowerInvariant();
}
