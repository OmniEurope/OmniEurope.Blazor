using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>The app menu, whose rows are switched on and off by parameters.</summary>
public partial class AppMenuExample
{
    /// <summary>The make-believe holder of the account, sample data.</summary>
    private const string UserName = "Camille Durand";

    /// <summary>Whether the menu shows the signed-in user and their role.</summary>
    [Parameter]
    public bool WithAccount { get; set; } = true;

    /// <summary>Whether three languages are offered: the language row shows from two.</summary>
    [Parameter]
    public bool WithLanguages { get; set; } = true;

    /// <summary>Whether the language row shows the flag of the language in force.</summary>
    [Parameter]
    public bool ShowFlags { get; set; }

    /// <summary>Whether the settings row shows.</summary>
    [Parameter]
    public bool WithSettings { get; set; } = true;

    /// <summary>Whether the sign-out button shows.</summary>
    [Parameter]
    public bool WithSignOut { get; set; } = true;

    private string Language { get; set; } = "fr";

    private OmniAppearance Mode { get; set; } = OmniAppearance.System;

    private string? LastChoice { get; set; }

    /// <summary>A bound callback makes its row appear: switched off, the example passes none.</summary>
    private EventCallback SettingsCallback => WithSettings ? EventCallback.Factory.Create(this, () => Choose("Settings")) : default;

    private EventCallback SignOutCallback => WithSignOut ? EventCallback.Factory.Create(this, () => Choose("Sign out")) : default;

    private void Choose(string item) => LastChoice = item;
}
