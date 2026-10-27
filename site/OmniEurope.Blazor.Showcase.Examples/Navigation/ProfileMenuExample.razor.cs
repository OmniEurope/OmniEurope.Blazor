using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>An account menu whose icons, disabled entry, link entry, density and closing behavior are parameters.</summary>
public partial class ProfileMenuExample
{
    /// <summary>The make-believe holder of the account, sample data.</summary>
    private const string UserName = "Camille Durand";

    /// <summary>Whether each entry shows an icon before its name.</summary>
    [Parameter]
    public bool ShowIcons { get; set; } = true;

    /// <summary>Whether the Administration entry is disabled.</summary>
    [Parameter]
    public bool DisableAdministration { get; set; } = true;

    /// <summary>Whether the first entry, which has an address, is shown: it is rendered as a link.</summary>
    [Parameter]
    public bool ShowLinkItem { get; set; } = true;

    /// <summary>Whether the menu cannot open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether a press outside the open menu closes it.</summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

    /// <summary>Whether the menu takes the compact density.</summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>The host keeps the state of the menu: OpenChanged reports it, Open gives it back.</summary>
    private bool MenuOpen { get; set; }

    private string? LastChoice { get; set; }

    private void Choose(string item) => LastChoice = item;
}
