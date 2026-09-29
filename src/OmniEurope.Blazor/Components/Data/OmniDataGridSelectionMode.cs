namespace OmniEurope.Blazor.Components;

/// <summary>Whether and how rows of an <see cref="OmniDataGrid{TItem}"/> can be selected (its <c>SelectionMode</c> parameter).</summary>
public enum OmniDataGridSelectionMode
{
    /// <summary>No selection and no checkbox column. The default.</summary>
    None,

    /// <summary>A checkbox column where selecting a row clears the previous selection.</summary>
    Single,

    /// <summary>A checkbox column where several rows can be selected at once.</summary>
    Multiple
}
