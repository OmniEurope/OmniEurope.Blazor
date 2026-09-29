using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="OmniSelectableCard"/> needs from the <see cref="OmniSelectableCardGroup{TValue, TSelection}"/>
/// around it, free of the group's type arguments: whether its choice is selected, whether it is the tab
/// stop of a radio group, and the pick and arrow-key moves it forwards.
/// </summary>
internal interface IOmniSelectableCardGroup
{
    /// <summary>True for a group of check boxes, false for a radio group.</summary>
    bool Multiple { get; }

    /// <summary>Whether the whole group is disabled.</summary>
    bool Disabled { get; }

    /// <summary>Adds a card, in rendering order; refuses a choice that is not of the group's value type.</summary>
    void Register(OmniSelectableCard card);

    /// <summary>Removes a card that left the page.</summary>
    void Unregister(OmniSelectableCard card);

    /// <summary>Whether the card's choice is part of the group's value.</summary>
    bool IsSelected(OmniSelectableCard card);

    /// <summary>In a radio group, whether the card is the one Tab reaches (roving tab index).</summary>
    bool IsTabStop(OmniSelectableCard card);

    /// <summary>Picks the card: selects it in a radio group, toggles it in a group of check boxes.</summary>
    Task PickAsync(OmniSelectableCard card);

    /// <summary>In a radio group, moves the selection and the focus for an arrow, Home or End key.</summary>
    Task MoveAsync(OmniSelectableCard card, string key);
}
