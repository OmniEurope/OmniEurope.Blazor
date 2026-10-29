namespace OmniEurope.Blazor.Components;

/// <summary>
/// The summary of a form's validation errors: a danger alert titled with the number of errors ("1 error to
/// correct", "2 errors to correct", in the language of the page) over the list of the messages of the
/// enclosing <see cref="EditForm"/>'s edit context. It is drawn only while there is an error, and follows
/// the context: fixing a field removes its message, and the last one removes the alert.
/// </summary>
/// <remarks>
/// <para>
/// The component sits in the form's markup, so the host decides where the summary goes. Where the host lets
/// the user choose, it places two: one before the fields (<see cref="Slot"/>
/// <see cref="OmniValidationSummaryPosition.Top"/>) and one just above <see cref="OmniFormActions"/>
/// (<see cref="OmniValidationSummaryPosition.Bottom"/>, the default), and gives both the same
/// <see cref="Position"/>: only the one whose slot is the position draws, and
/// <see cref="OmniValidationSummaryPosition.Hidden"/> silences both.
/// </para>
/// <para>
/// <see cref="OmniComponentBase.Id"/>, <see cref="OmniComponentBase.Class"/> and the additional attributes go
/// on the alert.
/// </para>
/// </remarks>
public partial class OmniValidationSummary : IDisposable
{
    private EditContext? _subscribed;
    private IReadOnlyList<string> _messages = [];

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    /// <summary>
    /// The position the host chose for the summary; <see cref="OmniValidationSummaryPosition.Bottom"/> by
    /// default. The component draws only when this is its <see cref="Slot"/>.
    /// </summary>
    [Parameter]
    public OmniValidationSummaryPosition Position { get; set; } = OmniValidationSummaryPosition.Bottom;

    /// <summary>
    /// The position this instance occupies in the markup: <see cref="OmniValidationSummaryPosition.Top"/>
    /// before the fields, <see cref="OmniValidationSummaryPosition.Bottom"/> (the default) just above the
    /// actions. <see cref="OmniValidationSummaryPosition.Hidden"/> is not a place: such an instance never draws.
    /// </summary>
    [Parameter]
    public OmniValidationSummaryPosition Slot { get; set; } = OmniValidationSummaryPosition.Bottom;

    /// <summary>The title of the alert; null or blank, the default, writes the localized count of the errors.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The fill of the alert: <see cref="OmniFill.Solid"/> by default, to stand out over the fields.</summary>
    [Parameter]
    public OmniFill Fill { get; set; } = OmniFill.Solid;

    /// <summary>Takes the edit context of the enclosing form, listens to it, and refuses to stand outside a form.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException("OmniValidationSummary requires a cascading EditContext: place it inside an EditForm or an OmniTemplateForm.");
        }

        if (!ReferenceEquals(CurrentEditContext, _subscribed))
        {
            Unsubscribe();
            _subscribed = CurrentEditContext;
            _subscribed.OnValidationStateChanged += HandleValidationStateChanged;
        }

        _messages = [.. CurrentEditContext.GetValidationMessages()];
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        _messages = [.. _subscribed!.GetValidationMessages()];
        _ = InvokeAsync(StateHasChanged);
    }

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.OnValidationStateChanged -= HandleValidationStateChanged;
            _subscribed = null;
        }
    }

    /// <summary>Stops listening to the edit context.</summary>
    public void Dispose() => Unsubscribe();
}
