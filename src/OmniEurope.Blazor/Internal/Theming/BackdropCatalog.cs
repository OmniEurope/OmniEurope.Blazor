namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The backdrops of the library, each defined in its own file under <c>Internal/Theming/Backdrops</c>.
/// Adding one is one entry here plus its definition (and, for a canvas field, its script): the
/// appearance window, the preference and the boot snapshot take it from <see cref="Components.OmniThemeBackdrops.All"/>.
/// A theme names its own in <see cref="ThemeDefinition.DefaultBackdrop"/>.
/// </summary>
internal static class BackdropCatalog
{
    /// <summary>The name of the plain page, <see cref="Components.OmniThemeBackdrops.None"/>.</summary>
    internal const string NoneName = "Aucun";

    /// <summary>The description of the plain page; kept here because the component sources carry no French literal.</summary>
    internal const string NoneDescription = "Page unie, sans fond dessiné derrière le contenu.";

    /// <summary>Every backdrop drawn, in the order of the pickers, after the plain page.</summary>
    public static IReadOnlyList<BackdropDefinition> All { get; } =
    [
        GivreBackdrop.Definition,
        TrouNoirBackdrop.Definition,
        NenupharBackdrop.Definition,
        VeloursBackdrop.Definition,
        ReliefBackdrop.Definition,
        AplatBackdrop.Definition,
        EpureBackdrop.Definition,
    ];

    /// <summary>
    /// The module that draws a canvas kind (a value of <c>--omni-scope-canvas</c>), from the backdrop
    /// that names it in either mode; null for <c>none</c> or a kind no backdrop draws.
    /// </summary>
    internal static string? CanvasScriptFor(string? kind) =>
        kind is null or "none"
            ? null
            : All.FirstOrDefault(backdrop => backdrop.CanvasScript is not null
                && (backdrop.Shape.GetValueOrDefault("--omni-scope-canvas") == kind || backdrop.DarkShape.GetValueOrDefault("--omni-scope-canvas") == kind))?.CanvasScript;
}
