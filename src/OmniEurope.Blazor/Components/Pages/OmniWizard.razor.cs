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
    private OmniStepsSharedPanel? _sharedPanel;
    private int? _lastValue;
    private bool _validating;
    private bool _focusBody;
    private IJSObjectReference? _module;
    private string? _appliedBodyHeight;
    private bool _disposed;

    // Counts the moves and cancellations: a validation answered after one of them is stale.
    private int _navigation;

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

    /// <summary>
    /// The height of the body of the steps, as a CSS length (<c>20rem</c>, <c>400px</c>, <c>50vh</c>), or
    /// <c>auto</c> to let the body follow its content. Null, the default, keeps the height of the
    /// stylesheet, <c>min(24rem, 60vh)</c>, which a host changes for every wizard through the
    /// <c>--omni-wizard-body-height</c> property. The body keeps this height whatever the step and
    /// scrolls when a step is taller, so Previous, Next, Finish and Cancel stay at the same place. With
    /// <c>auto</c> the foot follows the content again, and sticks to the bottom of the container when the
    /// wizard sits in a column that is taller. The value is written by the interop script as a CSS custom
    /// property (never as a style attribute), so a wizard rendered without interactivity keeps the
    /// stylesheet height.
    /// </summary>
    [Parameter]
    public string? BodyHeight { get; set; }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private bool IsFlexibleBody => string.Equals(BodyHeight, "auto", StringComparison.Ordinal);

    private int Count => _context.Steps.Count;

    private bool IsLast => _current >= Count - 1;

    private OmniWizardStep? CurrentStep => _current < Count ? _context.Steps[_current] : null;

    private bool CurrentCanContinue => CurrentStep?.CanContinue ?? true;

    private double Progress => Count <= 1 ? 100 : Math.Round((double)_current / (Count - 1) * 100, 0);

    private string EffectiveId => Id ?? _generatedId;

    private string BodyId => $"{EffectiveId}-body";

    /// <summary>The body, cascaded to the step items so their buttons control it; kept while its id holds.</summary>
    private OmniStepsSharedPanel SharedPanel => _sharedPanel is { } panel && panel.Id == BodyId
        ? panel
        : _sharedPanel = new OmniStepsSharedPanel(BodyId);

    private string CurrentButtonId => $"{StepItemId(_current)}-button";

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("WizardLabel") : Label;

    private string EffectiveFinishText => string.IsNullOrWhiteSpace(FinishText) ? Localize("WizardFinish") : FinishText;

    private string PositionText => Localize("WizardPosition", _current + 1, Math.Max(Count, 1));

    private string Announcement => CurrentStep is { } step
        ? Localize("WizardAnnouncement", _current + 1, Count, step.Title)
        : string.Empty;

    private string StepItemId(int index) => $"{EffectiveId}-step-{index}";

    /// <summary>
    /// Moves to the step of a new <see cref="Value"/> (a negative value means the first step); a parent
    /// that redraws with the same value leaves the wizard where the user brought it.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="BodyHeight"/> is neither a CSS length nor <c>auto</c>.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (BodyHeight is not null and not "auto" && !Internal.CssLength.IsHeight(BodyHeight))
        {
            throw new ArgumentException($"'{BodyHeight}' is not a CSS length {nameof(OmniWizard)} accepts for {nameof(BodyHeight)} (a number followed by px, rem, em, vh or %, or auto).", nameof(BodyHeight));
        }

        // Only a new value from the host moves the wizard: an unbound wizard keeps its own position
        // when its parent redraws with the same Value as before.
        if (_lastValue != Value)
        {
            _lastValue = Value;
            _navigation++;
            _current = Math.Max(0, Value);
            _highest = Math.Max(_highest, _current);
            _context.CurrentIndex = _current;
        }
    }

    /// <summary>
    /// Moves back to the last step when the current one left the list, else focuses the body after a
    /// move by the user.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the position and the focus are settled.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await ApplyBodyHeightAsync();

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

    /// <summary>Releases the interop script, if it was loaded.</summary>
    /// <returns>The task of the release.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The page is gone with the script.
        }
    }

    // Hands a BodyHeight that changed to the interop script, loaded only once a length has to be
    // written; a wizard that keeps the stylesheet height loads no script. A lost circuit is ignored.
    private async Task ApplyBodyHeightAsync()
    {
        var height = IsFlexibleBody ? null : BodyHeight;
        if (_disposed || string.Equals(height, _appliedBodyHeight, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", OmniModules.Interop);
            if (!_disposed)
            {
                await _module.InvokeVoidAsync("applyWizardBodyHeight", _body, height);
                _appliedBodyHeight = height;
            }
        }
        catch (JSDisconnectedException)
        {
            // The page is gone; nothing left to size.
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

    private Task CancelAsync()
    {
        _navigation++;
        return OnCancel.InvokeAsync();
    }

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

        // One validation at a time; and an answer that comes after the user moved or cancelled is
        // for a step they have left, so it lets nothing go on.
        if (_validating)
        {
            return false;
        }

        var navigation = _navigation;
        _validating = true;
        StateHasChanged();
        try
        {
            return await step.Validate() && navigation == _navigation;
        }
        finally
        {
            _validating = false;
            StateHasChanged();
        }
    }

    private async Task MoveAsync(int target, bool focus)
    {
        _navigation++;
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
