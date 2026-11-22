using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>The delivery extras the list examples offer, the certified copy optionally inert.</summary>
public static class DeliveryExtras
{
    /// <summary>The extras, in display order.</summary>
    /// <param name="disableCopy">Whether the certified copy stays visible but inert.</param>
    /// <returns>The options of the extras.</returns>
    public static IReadOnlyList<OmniOption<string>> Options(bool disableCopy) =>
    [
        new("receipt", "Acknowledgement of receipt"),
        new("copy", "Certified copy", Disabled: disableCopy),
        new("tracking", "Email tracking"),
        new("insurance", "Insurance")
    ];
}
