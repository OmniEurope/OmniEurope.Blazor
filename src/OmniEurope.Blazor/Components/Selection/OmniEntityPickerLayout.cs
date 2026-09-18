namespace OmniEurope.Blazor.Components;

/// <summary>How an <see cref="OmniEntityPicker{TItem, TKey}"/> lays out what can be chosen and what is chosen.</summary>
public enum OmniEntityPickerLayout
{
    /// <summary>The chosen items as removable chips above the search and the list of every item, chosen ones marked.</summary>
    Stacked,

    /// <summary>Two columns: the items still available on one side, the chosen ones on the other.</summary>
    TwoColumns
}
