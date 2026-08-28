using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public partial class OmniDataGridColumn<TItem>
{
    private OmniDataGridContext<TItem>? _registeredContext;
    private OmniDataGridColumnDefinition<TItem>? _definition;
    private string? _registeredKey;

    [CascadingParameter]
    private OmniDataGridContext<TItem>? Context { get; set; }

    /// <summary>Stable identity of the column. Defaults to <see cref="Property"/> when omitted.</summary>
    [Parameter]
    public string Key { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Reads the cell value. Optional when <see cref="Property"/> is set.</summary>
    [Parameter]
    public Func<TItem, object?>? Value { get; set; }

    /// <summary>Dotted property path read from the item, for example <c>Customer.Name</c>.</summary>
    [Parameter]
    public string? Property { get; set; }

    /// <summary>Property path used for sorting when it differs from <see cref="Property"/>.</summary>
    [Parameter]
    public string? SortProperty { get; set; }

    [Parameter]
    public RenderFragment<TItem>? Template { get; set; }

    [Parameter]
    public RenderFragment<TItem>? EditTemplate { get; set; }

    [Parameter]
    public RenderFragment<TItem>? FooterTemplate { get; set; }

    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    [Parameter]
    public Func<TItem, string, bool>? FilterPredicate { get; set; }

    [Parameter]
    public Func<object?, string>? Format { get; set; }

    /// <summary>Composite format applied to the cell value, for example <c>{0:n2}</c>.</summary>
    [Parameter]
    public string? FormatString { get; set; }

    [Parameter]
    public bool Sortable { get; set; } = true;

    /// <summary>Initial sort applied to this column when the grid first renders.</summary>
    [Parameter]
    public OmniDataGridSortOrder? SortOrder { get; set; }

    [Parameter]
    public bool Filterable { get; set; }

    [Parameter]
    public OmniDataGridColumnFilterType FilterType { get; set; }

    [Parameter]
    public OmniDataGridFilterOperator FilterOperator { get; set; }

    [Parameter]
    public OmniDataGridFilterOperator SecondFilterOperator { get; set; }

    [Parameter]
    public OmniDataGridLogicalOperator LogicalFilterOperator { get; set; }

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public bool Resizable { get; set; }

    /// <summary>Keeps the column visible against the inline start edge while the grid scrolls sideways.</summary>
    [Parameter]
    public bool Frozen { get; set; }

    /// <summary>CSS length applied to the column, for example <c>160px</c> or <c>12rem</c>.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>Minimum CSS length the column keeps while the table shrinks.</summary>
    [Parameter]
    public string? MinWidth { get; set; }

    [Parameter]
    public OmniDataGridTextAlign TextAlign { get; set; }

    [Parameter]
    public string? CssClass { get; set; }

    [Parameter]
    public string? HeaderCssClass { get; set; }

    [Parameter]
    public bool Groupable { get; set; } = true;

    private string EffectiveKey => string.IsNullOrWhiteSpace(Key)
        ? Property ?? Title
        : Key;

    protected override void OnParametersSet()
    {
        var key = EffectiveKey;
        if (_registeredContext is not null
            && (!ReferenceEquals(_registeredContext, Context) || !string.Equals(_registeredKey, key, StringComparison.Ordinal)))
        {
            _registeredContext.Unregister(_registeredKey!);
            _definition = null;
        }

        var accessor = Value
            ?? GridPropertyAccessor.Create<TItem>(Property)
            ?? (static _ => null);
        var sortAccessor = GridPropertyAccessor.Create<TItem>(SortProperty);

        var definition = new OmniDataGridColumnDefinition<TItem>
        {
            Key = key,
            Title = Title,
            Value = accessor,
            Property = Property,
            SortProperty = SortProperty,
            SortValue = sortAccessor,
            Template = Template,
            EditTemplate = EditTemplate,
            FooterTemplate = FooterTemplate,
            HeaderTemplate = HeaderTemplate,
            FilterPredicate = FilterPredicate,
            Format = Format,
            FormatString = FormatString,
            Sortable = Sortable,
            SortOrder = SortOrder,
            Filterable = Filterable,
            FilterType = FilterType,
            FilterOperator = FilterOperator,
            SecondFilterOperator = SecondFilterOperator,
            LogicalFilterOperator = LogicalFilterOperator,
            Visible = Visible,
            Resizable = Resizable,
            Frozen = Frozen,
            Width = Width,
            MinWidth = MinWidth,
            TextAlign = TextAlign,
            CssClass = CssClass,
            HeaderCssClass = HeaderCssClass,
            Groupable = Groupable
        };
        if (Context is not null && !Matches(_definition, definition))
        {
            Context.Register(definition);
            _registeredContext = Context;
            _registeredKey = key;
            _definition = definition;
        }
    }

    public void Dispose() => _registeredContext?.Unregister(_registeredKey!);

    private static bool Matches(OmniDataGridColumnDefinition<TItem>? left, OmniDataGridColumnDefinition<TItem> right) =>
        left is not null
        && left.Key == right.Key
        && left.Title == right.Title
        && Equals(left.Value, right.Value)
        && string.Equals(left.Property, right.Property, StringComparison.Ordinal)
        && string.Equals(left.SortProperty, right.SortProperty, StringComparison.Ordinal)
        && Equals(left.Template, right.Template)
        && Equals(left.EditTemplate, right.EditTemplate)
        && Equals(left.FooterTemplate, right.FooterTemplate)
        && Equals(left.HeaderTemplate, right.HeaderTemplate)
        && Equals(left.FilterPredicate, right.FilterPredicate)
        && Equals(left.Format, right.Format)
        && string.Equals(left.FormatString, right.FormatString, StringComparison.Ordinal)
        && left.Sortable == right.Sortable
        && left.SortOrder == right.SortOrder
        && left.Filterable == right.Filterable
        && left.FilterType == right.FilterType
        && left.FilterOperator == right.FilterOperator
        && left.SecondFilterOperator == right.SecondFilterOperator
        && left.LogicalFilterOperator == right.LogicalFilterOperator
        && left.Visible == right.Visible
        && left.Resizable == right.Resizable
        && left.Frozen == right.Frozen
        && string.Equals(left.Width, right.Width, StringComparison.Ordinal)
        && string.Equals(left.MinWidth, right.MinWidth, StringComparison.Ordinal)
        && left.TextAlign == right.TextAlign
        && string.Equals(left.CssClass, right.CssClass, StringComparison.Ordinal)
        && string.Equals(left.HeaderCssClass, right.HeaderCssClass, StringComparison.Ordinal)
        && left.Groupable == right.Groupable;
}
