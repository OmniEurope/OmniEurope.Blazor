namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The accessible name of an element that splats the host's attributes and then writes its own
/// <c>aria-label</c>: written after the splat, the component's value wins, and a null one erased the
/// <c>aria-label</c> the host had passed. The component's <c>Label</c> still wins when it is given;
/// without it, the host's own <c>aria-label</c> is kept.
/// </summary>
internal static class OmniAriaLabel
{
    internal static string? Of(string? label, IReadOnlyDictionary<string, object>? attributes) =>
        !string.IsNullOrWhiteSpace(label)
            ? label
            : attributes is not null && attributes.TryGetValue("aria-label", out var host) ? host?.ToString() : null;
}
