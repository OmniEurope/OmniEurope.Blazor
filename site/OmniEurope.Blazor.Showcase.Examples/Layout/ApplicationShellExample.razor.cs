using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A whole application shell: top bar, side menu that collapses to an icon rail, and content.</summary>
public partial class ApplicationShellExample
{
    /// <summary>The sections of the side menu, the first one shown at first.</summary>
    private static readonly (string Key, string Text, OmniIconName Icon)[] Sections =
    [
        ("dashboard", "Dashboard", OmniIconName.SquaresFour),
        ("pipelines", "Pipelines", OmniIconName.GitFork),
        ("servers", "Servers", OmniIconName.HardDrive),
        ("projects", "Projects", OmniIconName.Folder),
        ("settings", "Settings", OmniIconName.Settings),
        ("help", "Help centre", OmniIconName.Question),
        ("administration", "Administration", OmniIconName.ShieldCheck)
    ];

    private static readonly IReadOnlyList<OmniAppMenuLanguage> Languages =
        [new("fr", "Français"), new("en", "English"), new("de", "Deutsch")];

    private bool MenuOpen { get; set; } = true;

    private string Selected { get; set; } = Sections[0].Key;

    private string? Search { get; set; }

    private string? Last { get; set; }

    private OmniAppearance MenuAppearance { get; set; } = OmniAppearance.System;

    private string LanguageCode { get; set; } = "en";

    private bool Maximized { get; set; }

    /// <summary>The unread notifications the bell announces.</summary>
    private static int Unread => 3;

    /// <summary>The menu's guard: the address names the section after its <c>#</c>; the example shows it in place and never navigates.</summary>
    /// <param name="href">The address of the item chosen.</param>
    /// <returns>Always false: the navigation is refused.</returns>
    private Task<bool> SelectAsync(string href)
    {
        Selected = href[(href.IndexOf('#', StringComparison.Ordinal) + 1)..];
        StateHasChanged();
        return Task.FromResult(false);
    }
}
