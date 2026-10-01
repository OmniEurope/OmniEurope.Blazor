namespace OmniEurope.Blazor.Components;

/// <summary>
/// The row of a form's actions, placed inside the form so its submit button submits it: aligned to the
/// end like a dialog footer, the main action last, wrapping under a narrow width. In an
/// <see cref="OmniLoginShell"/>, the sign-in button goes here, at the end of the host's form.
/// </summary>
public partial class OmniFormActions
{
    /// <summary>The buttons, the main action last. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }
}
