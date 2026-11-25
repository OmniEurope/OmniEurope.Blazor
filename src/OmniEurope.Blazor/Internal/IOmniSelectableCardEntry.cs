namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="IOmniSelectableCardGroup"/> reads from, or asks of, a registered card: the choice
/// it stands for, its title for an error message, whether it is available, the focus for an arrow key,
/// and a redraw when the selection changes.
/// </summary>
internal interface IOmniSelectableCardEntry
{
    /// <summary>The card's title, named in the error raised for a choice of the wrong type.</summary>
    string Title { get; }

    /// <summary>The value the card stands for inside its group.</summary>
    object? Choice { get; }

    /// <summary>Whether the card, or its whole group, is disabled.</summary>
    bool IsDisabled { get; }

    /// <summary>Moves the focus to the card.</summary>
    ValueTask FocusAsync();

    /// <summary>Redraws the card for the group's current value.</summary>
    void Refresh();
}
