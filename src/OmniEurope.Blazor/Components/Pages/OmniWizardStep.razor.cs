namespace OmniEurope.Blazor.Components;

/// <summary>
/// One step of an <see cref="OmniWizard"/>, declared inside it in the order the steps run. Only the
/// current step renders its content. A step decides whether the user may leave it forward: at once
/// through <see cref="CanContinue"/>, or on the click through <see cref="Validate"/>.
/// </summary>
/// <remarks>
/// Steps take their place in the order they are first rendered. A step shown later under a condition
/// goes to the end of the list, so a wizard whose steps vary declares all of them and lets the
/// content of each say when it does not apply.
/// </remarks>
public partial class OmniWizardStep : IDisposable
{
    private string? _announcedTitle;
    private bool _announcedCanContinue = true;

    [CascadingParameter]
    private OmniWizardContext? Context { get; set; }

    /// <summary>Name of the step in the step list and in the announcement of the step.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>The content of the step, rendered while it is current.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the user may leave this step forward. False disables Next, or Finish on the last step,
    /// until the step is complete.
    /// </summary>
    [Parameter]
    public bool CanContinue { get; set; } = true;

    /// <summary>
    /// Asked when the user leaves this step forward, by Next, Finish or a later step of the list; false
    /// keeps the step, which is then expected to show why. Leaves Next enabled, so the user gets an
    /// answer rather than a button that does nothing.
    /// </summary>
    [Parameter]
    public Func<Task<bool>>? Validate { get; set; }

    protected override void OnInitialized()
    {
        _announcedTitle = Title;
        _announcedCanContinue = CanContinue;
        Context?.Register(this);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Only what the wizard draws outside the step is worth a redraw; comparing it keeps a redraw
        // of the wizard, which sets these parameters again, from asking for yet another one.
        if (Context is not null && (!string.Equals(Title, _announcedTitle, StringComparison.Ordinal) || CanContinue != _announcedCanContinue))
        {
            _announcedTitle = Title;
            _announcedCanContinue = CanContinue;
            Context.StepChanged();
        }
    }

    /// <summary>
    /// Redraws the step when the wizard moves: a step whose parameters did not change would otherwise
    /// keep showing, or hiding, its content.
    /// </summary>
    internal void Redraw() => StateHasChanged();

    /// <summary>Leaves the wizard's list of steps.</summary>
    public void Dispose()
    {
        Context?.Unregister(this);
        GC.SuppressFinalize(this);
    }
}
