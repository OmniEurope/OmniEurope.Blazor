namespace OmniEurope.Blazor.Internal;

/// <summary>The detachment of the frozen columns as the grid's other collaborators use it, from <see cref="GridFrozenState{TItem}"/>.</summary>
internal interface IGridFrozenState
{
    bool FrozenDetached { get; }

    bool HorizontallyScrolled { get; }

    void EndDetachmentWithoutFrozenColumns();

    void ApplyHorizontalScroll(bool scrolled);
}
