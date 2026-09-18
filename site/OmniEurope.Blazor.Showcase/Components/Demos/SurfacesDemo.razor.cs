using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SurfacesDemo
{
    private static readonly string[] TabKeys = ["overview", "logs", "artifacts", "settings"];

    private static readonly (string Key, string TextKey)[] MenuEntries =
    [
        ("tableau-de-bord", "SurfMenuDashboard"),
        ("serveurs", "SurfMenuServers"),
        ("projets", "SurfMenuProjects"),
        ("administration", "SurfMenuAdministration")
    ];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private string? Tab { get; set; } = "overview";

    private string MenuCurrent { get; set; } = MenuEntries[0].Key;

    private string? Last { get; set; }

    /// <summary>Marks the entry chosen and refuses the navigation: the demonstration stays on its page.</summary>
    private Task<bool> SelectAsync(string key)
    {
        MenuCurrent = key;
        StateHasChanged();
        return Task.FromResult(false);
    }
}
