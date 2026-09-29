namespace OmniEurope.Blazor.Components;

/// <summary>
/// One step of an <see cref="OmniSteps"/>: a numbered button that selects the step, and the panel shown
/// while it is selected (unless a shared panel shows the steps instead).
/// </summary>
public partial class OmniStepsItem
{
    private readonly string _generatedId = $"omni-step-{Guid.NewGuid():N}";

    [CascadingParameter]
    private OmniStepsContext? Context { get; set; }

    /// <summary>
    /// The position of the step, from zero, compared with <see cref="OmniSteps.Value"/> to mark it selected.
    /// Null, the default, takes the step's place in the order the steps were first rendered, which is
    /// their written order; a step added later takes the next place, so steps inserted or reordered at
    /// run time give their own. The marker shows the position plus one.
    /// </summary>
    [Parameter]
    public int? Index { get; set; }

    /// <summary>The label of the step, shown beside its marker. Required.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Disables the step's button, so the reader cannot select it. False by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// An icon shown in the step's marker instead of its number, which stays readable by assistive
    /// technologies. A slot, as on the other components that take an icon, usually an
    /// <see cref="OmniIcon"/>. Null, the default, shows the number.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// The content of the step's panel, shown while the step is selected. Not rendered when a shared
    /// panel shows the steps, as in a wizard.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// A panel rendered elsewhere that shows this step, as a wizard does with one body for all its
    /// steps: the item then renders no panel of its own and its button controls that one.
    /// </summary>
    [CascadingParameter]
    private OmniStepsSharedPanel? SharedPanel { get; set; }

    private int Position => Index ?? Math.Max(0, Context?.PositionOf(this) ?? 0);
    private bool Selected => Context?.Value == Position;
    private string ButtonId => $"{Id ?? _generatedId}-button";
    private string OwnPanelId => $"{Id ?? _generatedId}-panel";
    private string ControlledPanelId => SharedPanel?.Id ?? OwnPanelId;
    private Task SelectAsync() => Disabled || Context is null ? Task.CompletedTask : Context.SelectAsync(Position);

    /// <summary>Takes the step's place among the steps of its <see cref="OmniSteps"/>.</summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Context?.Register(this);
    }

    /// <summary>Gives the step's place back, so the steps after it move up one.</summary>
    public void Dispose()
    {
        Context?.Unregister(this);
        GC.SuppressFinalize(this);
    }
}
