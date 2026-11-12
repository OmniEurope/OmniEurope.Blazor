using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The backdrops shipped with the library. Each theme comes with its own (or with none), and any theme
/// but the default one can be drawn over any of them through <see cref="OmniThemeScope.Backdrop"/>.
/// </summary>
public static class OmniThemeBackdrops
{
    /// <summary>No backdrop: the plain page of the theme. The default of Essentiel and of every theme without a backdrop of its own.</summary>
    public static OmniThemeBackdrop None { get; } = new(
        BackdropCatalog.NoneName,
        BackdropCatalog.NoneDescription,
        new Dictionary<string, string>(StringComparer.Ordinal),
        new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>Every backdrop of the catalogue, <see cref="None"/> first.</summary>
    public static IReadOnlyList<OmniThemeBackdrop> All { get; } =
    [
        None,
        .. BackdropCatalog.All.Select(backdrop => new OmniThemeBackdrop(backdrop.Name, backdrop.Description, backdrop.Shape, backdrop.DarkShape)),
    ];
}
