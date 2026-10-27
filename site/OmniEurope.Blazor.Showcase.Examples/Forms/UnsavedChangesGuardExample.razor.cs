using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A guard that asks before the page is left while it holds pending changes.</summary>
public partial class UnsavedChangesGuardExample
{
    /// <summary>Whether the page holds changes the user has not saved.</summary>
    [Parameter]
    public bool HasChanges { get; set; } = true;

    /// <summary>Whether the guard asks with the texts of the host instead of the localized texts of the library.</summary>
    [Parameter]
    public bool CustomTexts { get; set; }

    /// <summary>The address the link leaves to.</summary>
    [Parameter]
    public string LeaveHref { get; set; } = "/";
}
