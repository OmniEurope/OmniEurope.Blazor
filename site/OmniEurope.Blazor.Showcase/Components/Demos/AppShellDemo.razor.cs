using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class AppShellDemo
{
    /// <summary>The sections of the side menu, the first one shown at first.</summary>
    private static readonly (string Key, string TextKey, OmniIconName Icon)[] Sections =
    [
        ("tableau-de-bord", "ShellDashboard", OmniIconName.SquaresFour),
        ("pipelines", "ShellPipelines", OmniIconName.GitFork),
        ("serveurs", "ShellServers", OmniIconName.HardDrive),
        ("projets", "ShellProjects", OmniIconName.Folder),
        ("parametres", "ShellSettings", OmniIconName.Settings),
        ("aide", "ShellHelpCenter", OmniIconName.Question),
        ("administration", "ShellAdministration", OmniIconName.ShieldCheck)
    ];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private bool MenuOpen { get; set; } = true;

    private string Selected { get; set; } = Sections[0].Key;

    private string? Search { get; set; }

    private string? Last { get; set; }

    /// <summary>The unread notifications the bell announces.</summary>
    private static int Unread => 3;

    /// <summary>Marks the section chosen and refuses the navigation: the demonstration stays on its page.</summary>
    private Task<bool> SelectAsync(string key)
    {
        Selected = key;
        StateHasChanged();
        return Task.FromResult(false);
    }
}
