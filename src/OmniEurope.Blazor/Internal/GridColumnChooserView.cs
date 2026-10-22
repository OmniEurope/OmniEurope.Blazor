using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The markup of the column chooser (<see cref="GridColumnChooser{TItem}"/>), at the end of the grid's header
/// bar: an <see cref="OmniPopover"/> whose icon trigger opens a list of one row per column, an
/// <see cref="OmniCheckBox{TValue}"/> named by the column (<c>AllowColumnHide</c>), its two arrow
/// <see cref="OmniButton"/>s (<c>AllowColumnReorder</c>), then the reset of the layout (<c>ShowResetLayout</c>).
/// Written in C#, the grid's markup file being at its size budget. Rows are keyed by column, so a moved row
/// keeps its controls; the focus is then given back to its arrow.
/// </summary>
internal sealed class GridColumnChooserView<TItem>(OmniDataGrid<TItem> grid)
{
    private GridColumnChooser<TItem> Chooser => grid.Chooser;

    /// <summary>The chooser, rendered by the header bar while <see cref="GridColumnChooser{TItem}.Shown"/>.</summary>
    internal RenderFragment Render => builder =>
    {
        var label = grid.Text("GridColumnChooser");
        builder.OpenComponent<OmniPopover>(0);
        builder.AddComponentParameter(1, nameof(OmniPopover.Class), "omni-data-grid__chooser");
        builder.AddComponentParameter(2, nameof(OmniPopover.Size), grid.Classes.BarButtonSize);
        builder.AddComponentParameter(3, nameof(OmniPopover.TriggerClass), grid.Classes.ChooserToggleClass);
        builder.AddComponentParameter(4, nameof(OmniPopover.Label), label);
        builder.AddComponentParameter(5, nameof(OmniPopover.Placement), OmniPopoverPlacement.BottomEnd);
        builder.AddComponentParameter(6, nameof(OmniPopover.TriggerContent), Icon(OmniIconName.Columns));
        builder.AddComponentParameter(7, nameof(OmniPopover.ChildContent), Panel(label));
        builder.CloseComponent();
    };

    private RenderFragment Panel(string label) => builder =>
    {
        builder.OpenElement(0, "ul");
        builder.AddAttribute(1, "class", "omni-data-grid__chooser-list");
        builder.AddAttribute(2, "aria-label", label);
        builder.AddElementReferenceCapture(3, reference => Chooser.List = reference);
        foreach (var column in Chooser.Entries)
        {
            builder.OpenElement(4, "li");
            builder.SetKey(column.Key);
            builder.AddAttribute(5, "class", "omni-data-grid__chooser-item");
            builder.AddAttribute(6, "data-omni-chooser-col", column.Key);
            builder.AddContent(7, Row(column));
            builder.CloseElement();
        }

        builder.CloseElement();
        if (grid.ShowResetLayout)
        {
            builder.OpenComponent<OmniButton>(8);
            builder.AddComponentParameter(9, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
            builder.AddComponentParameter(10, nameof(OmniButton.Size), OmniControlSize.Small);
            builder.AddComponentParameter(11, nameof(OmniButton.Class), "omni-data-grid__chooser-reset");
            builder.AddComponentParameter(12, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(grid, Chooser.ResetAsync));
            builder.AddComponentParameter(13, nameof(OmniButton.Icon), Icon(OmniIconName.Reset));
            builder.AddComponentParameter(14, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
            {
                content.OpenElement(0, "span");
                content.AddContent(1, grid.Text("GridResetLayout"));
                content.CloseElement();
            }));
            builder.CloseComponent();
        }
    };

    private RenderFragment Row(OmniDataGridColumnDefinition<TItem> column) => builder =>
    {
        var name = GridColumnChooser<TItem>.NameOf(column);
        if (grid.AllowColumnHide)
        {
            var shown = Chooser.IsVisible(column);
            builder.OpenComponent<OmniCheckBox<bool>>(0);
            builder.AddComponentParameter(1, nameof(OmniCheckBox<bool>.Class), "omni-data-grid__chooser-check");
            builder.AddComponentParameter(2, nameof(OmniCheckBox<bool>.Value), shown);
            builder.AddComponentParameter(3, nameof(OmniCheckBox<bool>.ValueExpression), (Expression<Func<bool>>)(() => shown));
            builder.AddComponentParameter(4, nameof(OmniCheckBox<bool>.ValueChanged), EventCallback.Factory.Create<bool>(grid, visible => Chooser.ToggleAsync(column, visible)));
            builder.AddComponentParameter(5, nameof(OmniCheckBox<bool>.Disabled), !Chooser.CanToggle(column));
            builder.AddComponentParameter(6, nameof(OmniCheckBox<bool>.ChildContent), (RenderFragment)(content => content.AddContent(0, name)));
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(7, "span");
            builder.AddAttribute(8, "class", "omni-data-grid__chooser-name");
            builder.AddContent(9, name);
            builder.CloseElement();
        }

        if (grid.AllowColumnReorder)
        {
            builder.OpenElement(10, "span");
            builder.AddAttribute(11, "class", "omni-data-grid__chooser-moves");
            builder.AddContent(12, Arrow(column, name, -1));
            builder.AddContent(13, Arrow(column, name, 1));
            builder.CloseElement();
        }
    };

    private RenderFragment Arrow(OmniDataGridColumnDefinition<TItem> column, string name, int step) => builder =>
    {
        var text = grid.Text(step < 0 ? "GridColumnMoveLeft" : "GridColumnMoveRight", name);
        builder.OpenComponent<OmniButton>(0);
        builder.AddComponentParameter(1, nameof(OmniButton.Variant), OmniButtonVariant.Ghost);
        builder.AddComponentParameter(2, nameof(OmniButton.Size), OmniControlSize.Small);
        builder.AddComponentParameter(3, nameof(OmniButton.Class), "omni-data-grid__chooser-move");
        builder.AddComponentParameter(4, nameof(OmniButton.Label), text);
        builder.AddComponentParameter(5, nameof(OmniButton.Disabled), !Chooser.CanMove(column, step));
        builder.AddComponentParameter(6, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(grid, () => Chooser.MoveAsync(column, step)));
        builder.AddComponentParameter(7, nameof(OmniButton.Icon), Icon(step < 0 ? OmniIconName.ArrowLeft : OmniIconName.ArrowRight));
        builder.AddComponentParameter(8, "title", text);
        builder.AddComponentParameter(9, "data-omni-move", step.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.AddComponentParameter(10, "data-omni-col", column.Key);
        builder.CloseComponent();
    };

    private static RenderFragment Icon(OmniIconName icon) => builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), icon);
        builder.CloseComponent();
    };
}
