namespace OmniEurope.Blazor.Components;

/// <summary>
/// A card the user picks, for a choice that deserves more than a radio button: an icon, a title and a
/// line of explanation. <see cref="Multiple"/> false (the default) makes it one choice among several
/// (<c>role="radio"</c>; place the cards in an element with <c>role="radiogroup"</c> and a name);
/// true makes it an option of its own (<c>role="checkbox"</c>). The card is one button: Tab reaches
/// each card, Enter or Space picks it.
/// </summary>
public partial class OmniSelectableCard
{
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

    /// <summary>Whether the card is chosen.</summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    /// Raised by a click, Enter or Space. A check box card sends the opposite of <see cref="Selected"/>;
    /// a radio card always sends true, so picking the chosen card again still reaches the host (to go
    /// on to the next step, for example).
    /// </summary>
    [Parameter]
    public EventCallback<bool> SelectedChanged { get; set; }

    /// <summary>False, the default: one choice among several. True: an option that is on or off.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    /// The card keeps its state and ignores clicks, as a choice another one forces on. It stays
    /// focusable and announced as unavailable.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    private Task ToggleAsync()
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        return SelectedChanged.InvokeAsync(!Multiple || !Selected);
    }
}
