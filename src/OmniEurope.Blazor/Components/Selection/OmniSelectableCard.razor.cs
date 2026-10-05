namespace OmniEurope.Blazor.Components;

/// <summary>
/// A card the user picks, for a choice that deserves more than a radio button: an icon, a title and a
/// line of explanation. Alone, it binds a <see cref="bool"/>: <see cref="Multiple"/> false (the default)
/// makes it one choice among several (<c>role="radio"</c>), true an option of its own
/// (<c>role="checkbox"</c>). Inside an <see cref="OmniSelectableCardGroup{TValue, TSelection}"/> it stands
/// for its <see cref="Choice"/> and the group binds the selection: <see cref="Value"/>,
/// <see cref="ValueChanged"/> and <see cref="Multiple"/> are then the group's. The card is one button:
/// Enter or Space picks it.
/// </summary>
public partial class OmniSelectableCard
{
    private ElementReference _element;
    private IOmniSelectableCardGroup? _registeredIn;

    /// <summary>The group around the card, which then owns the selection.</summary>
    [CascadingParameter]
    private IOmniSelectableCardGroup? Group { get; set; }

    /// <summary>What the card offers: "Linux".</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>A line under the title, muted: what the choice brings.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The icon before the text, in general an <see cref="OmniIcon"/>; decorative.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>More text under the description. Inside a button: text and badges only, nothing interactive.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether a card on its own is chosen. Ignored inside a group, which knows it from its value.</summary>
    [Parameter]
    public bool Value { get; set; }

    /// <summary>
    /// Raised by a click, Enter or Space on a card on its own. A check box card sends the opposite of
    /// <see cref="Value"/>; a radio card always sends true, so picking the chosen card again still reaches
    /// the host (to go on to the next step, for example). Not raised inside a group.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    /// <summary>
    /// False, the default: one choice among several. True: an option that is on or off. Inside a group,
    /// the group's <see cref="OmniSelectableCardGroup{TValue, TSelection}.Multiple"/> decides.
    /// </summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    /// The card keeps its state and ignores clicks, as a choice another one forces on. It stays
    /// focusable and announced as unavailable.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// The value the card stands for inside an <see cref="OmniSelectableCardGroup{TValue, TSelection}"/>,
    /// of the group's value type. Ignored on a card on its own.
    /// </summary>
    [Parameter]
    public object? Choice { get; set; }

    private bool IsSelected => Group?.IsSelected(this) ?? Value;

    private bool IsMultiple => Group?.Multiple ?? Multiple;

    internal bool IsDisabled => Disabled || Group?.Disabled == true;

    // Only a radio group keeps a single card in the tab order; elsewhere every card is a plain button.
    private string? TabIndex => Group is { Multiple: false } group ? (group.IsTabStop(this) ? "0" : "-1") : null;

    /// <summary>Joins the group around the card, once: the group cascades itself as a fixed value.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_registeredIn is null && Group is { } group)
        {
            _registeredIn = group;
            group.Register(this);
        }
    }

    internal ValueTask FocusAsync() => _element.FocusAsync();

    internal void Refresh() => StateHasChanged();

    private Task ToggleAsync()
    {
        if (IsDisabled)
        {
            return Task.CompletedTask;
        }

        return Group is { } group ? group.PickAsync(this) : ValueChanged.InvokeAsync(!Multiple || !Value);
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args) =>
        Group is { Multiple: false } group ? group.MoveAsync(this, args.Key) : Task.CompletedTask;

    /// <summary>Leaves the group.</summary>
    public void Dispose()
    {
        _registeredIn?.Unregister(this);
        _registeredIn = null;
        GC.SuppressFinalize(this);
    }
}
