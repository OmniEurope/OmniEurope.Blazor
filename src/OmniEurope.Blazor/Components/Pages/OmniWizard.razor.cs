namespace OmniEurope.Blazor.Components;

/// <summary>
/// A guided sequence of <see cref="OmniWizardStep"/>: progress, the list of steps, the current step,
/// then Previous, Next or Finish. Going back is always allowed; going forward asks the current step
/// first, through its <see cref="OmniWizardStep.CanContinue"/> and <see cref="OmniWizardStep.Validate"/>.
/// A step already reached can be clicked in the list. The wizard draws no overlay: inside an
/// <see cref="OmniDialog"/>, closing and Escape belong to the dialog.
/// </summary>
public partial class OmniWizard
{
    private readonly string _generatedId = $"omni-wizard-{Guid.NewGuid():N}";
    private readonly OmniWizardContext _context;
    private ElementReference _body;
    private int _current;
    private int _highest;
    private int? _lastValue;
    private bool _validating;
    private bool _focusBody;

    /// <summary>Creates the wizard and the context its steps register with.</summary>
    public OmniWizard() => _context = new OmniWizardContext(StateHasChanged);

    /// <summary>The steps, <see cref="OmniWizardStep"/> in the order they run.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Index of the current step, from 0. Bind it to follow or to set the step from the host.</summary>
    [Parameter]
    public int Value { get; set; }

    /// <summary>Raised when the current step changes.</summary>
    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    /// <summary>Accessible name of the list of steps; the localized "Wizard steps" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Text of the button of the last step; the localized "Finish" when empty.</summary>
    [Parameter]
    public string? FinishText { get; set; }

    /// <summary>Raised by Finish once the last step lets the user go on.</summary>
    [Parameter]
    public EventCallback OnFinish { get; set; }

    /// <summary>Raised by Cancel. Without a handler the wizard shows no Cancel button.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    private int Count => _context.Steps.Count;

    private bool IsLast => _current >= Count - 1;

    private OmniWizardStep? CurrentStep => _current < Count ? _context.Steps[_current] : null;

    private bool CurrentCanContinue => CurrentStep?.CanContinue ?? true;

    private double Progress => Count <= 1 ? 100 : Math.Round((double)_current / (Count - 1) * 100, 0);

    private string EffectiveId => Id ?? _generatedId;

    private string BodyId => $"{EffectiveId}-body";

    private string CurrentButtonId => $"{StepItemId(_current)}-button";

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("WizardLabel") : Label;

    private string EffectiveFinishText => string.IsNullOrWhiteSpace(FinishText) ? Localize("WizardFinish") : FinishText;

    private string PositionText => Localize("WizardPosition", _current + 1, Math.Max(Count, 1));

    private string Announcement => CurrentStep is { } step
        ? Localize("WizardAnnouncement", _current + 1, Count, step.Title)
        : string.Empty;

    private string StepItemId(int index) => $"{EffectiveId}-step-{index}";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Only a new value from the host moves the wizard: an unbound wizard keeps its own position
        // when its parent redraws with the same Value as before.
        if (_lastValue != Value)
        {
            _lastValue = Value;
            _current = Math.Max(0, Value);
            _highest = Math.Max(_highest, _current);
            _context.CurrentIndex = _current;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_current >= Count && Count > 0)
        {
            // A step left the list from under the current position: stay on the last one there is.
            await MoveAsync(Count - 1, focus: false);
            return;
        }

        if (_focusBody)
        {
            _focusBody = false;
            await _body.FocusAsync(preventScroll: true);
        }
    }

    private Task PreviousAsync() => _current > 0 ? MoveAsync(_current - 1, focus: true) : Task.CompletedTask;

    private async Task NextAsync()
    {
        if (!IsLast && await CurrentStepLetsGoAsync())
        {
            await MoveAsync(_current + 1, focus: true);
        }
    }

    private async Task FinishAsync()
    {
        if (IsLast && await CurrentStepLetsGoAsync())
        {
            await OnFinish.InvokeAsync();
        }
    }

    private Task CancelAsync() => OnCancel.InvokeAsync();

    /// <summary>The step list's gate: back is free, forward past the current step asks it first.</summary>
    private async Task<bool> CanNavigateAsync(int target) =>
        target <= _current || (target <= _highest && await CurrentStepLetsGoAsync());

    private Task GoToAsync(int target) => target == _current ? Task.CompletedTask : MoveAsync(target, focus: true);

    private async Task<bool> CurrentStepLetsGoAsync()
    {
        if (CurrentStep is not { } step)
        {
            return true;
        }

        if (!step.CanContinue)
        {
            return false;
        }

        if (step.Validate is null)
        {
            return true;
        }

        _validating = true;
        StateHasChanged();
        try
        {
            return await step.Validate();
        }
        finally
        {
            _validating = false;
            StateHasChanged();
        }
    }

    private async Task MoveAsync(int target, bool focus)
    {
        _current = Math.Clamp(target, 0, Math.Max(Count - 1, 0));
        _highest = Math.Max(_highest, _current);
        _context.CurrentIndex = _current;
        _focusBody = focus;
        foreach (var step in _context.Steps)
        {
            step.Redraw();
        }

        StateHasChanged();
        await ValueChanged.InvokeAsync(_current);
    }
}
