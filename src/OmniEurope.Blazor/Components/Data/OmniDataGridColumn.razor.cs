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

    /// <summary>
    /// Explicit list of Select/Combo suggestions. Left unset the grid derives them from the values
    /// this column reads, which only works when it reads a single value per row and the grid holds
    /// every row. Set it for a column built from a collection, or for a grid fed page by page.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? FilterValues { get; set; }

    /// <summary>
    /// This column's own filter editor, overriding <see cref="FilterType"/>. Use it for a filter
    /// shape none of the built-in types covers; the context carries the current value, the candidate
    /// values and the callback that applies a new one.
    /// </summary>
    [Parameter]
    public RenderFragment<OmniDataGridFilterContext>? FilterTemplate { get; set; }

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

    /// <summary>
    /// Puts a narrowing box above a MultiSelect filter, for a column whose candidate list is too
    /// long to scan by eye. Ignored by the other filter types.
    /// </summary>
    [Parameter]
    public bool FilterSearchable { get; set; }

    /// <summary>
    /// The text a filter shows for one candidate value (an enum member's translated name, for
    /// example). The value itself is what the filter keeps and sends; only its display changes.
    /// </summary>
    [Parameter]
    public Func<string, string>? FilterValueText { get; set; }

    /// <summary>
    /// Filter applied when the grid first shows this column, so a default narrowing (open items
    /// only, say) is visible and removable in the column's own filter instead of hidden in the
    /// query. A filter restored from the grid's saved state wins over it. For a MultiSelect it is
    /// the encoded list of <see cref="OmniDataGridFilterValues"/>.
    /// </summary>
    [Parameter]
    public string? DefaultFilterValue { get; set; }

    /// <summary>A DateRange filter also picks the hours; without it a day covers all of it.</summary>
    [Parameter]
    public bool FilterIncludesTime { get; set; }

    /// <summary>
    /// The operators this column offers, in this order, when the grid shows an operator choice.
    /// Null keeps every operator the column's value type allows; a list narrows that set, and an
    /// operator the type cannot use is dropped rather than offered.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniDataGridFilterOperator>? FilterOperators { get; set; }

    [Parameter]
    public OmniDataGridFilterOperator FilterOperator { get; set; }

    [Parameter]
    public OmniDataGridFilterOperator SecondFilterOperator { get; set; }

    [Parameter]
    public OmniDataGridLogicalOperator LogicalFilterOperator { get; set; }

    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether this column carries a resize handle. Left unset it follows the grid's
    /// <c>AllowColumnResize</c>, so turning resizing on once at grid level is enough; set it to
    /// false to keep one column fixed while the others can be resized.
    /// </summary>
    [Parameter]
    public bool? Resizable { get; set; }

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
            FilterValues = FilterValues,
            FilterTemplate = FilterTemplate,
            Format = Format,
            FormatString = FormatString,
            Sortable = Sortable,
            SortOrder = SortOrder,
            Filterable = Filterable,
            FilterType = ResolveFilterType(),
            FilterSearchable = FilterSearchable,
            FilterValueText = FilterValueText,
            DefaultFilterValue = DefaultFilterValue,
            FilterIncludesTime = FilterIncludesTime,
            EnumType = GridPropertyAccessor.EnumType<TItem>(Property),
            ValueType = GridPropertyAccessor.ValueType<TItem>(Property),
            FilterOperators = FilterOperators,
            FilterOperator = FilterOperator,
            SecondFilterOperator = SecondFilterOperator,
            LogicalFilterOperator = LogicalFilterOperator,
            Visible = Visible,
            Resizable = Resizable,
            Frozen = Frozen,
            Width = Width,
            MinWidth = MinWidth,
            TextAlign = TextAlign,
            Numeric = GridPropertyAccessor.IsNumeric<TItem>(Property),
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

    private OmniDataGridColumnFilterType ResolveFilterType()
    {
        var type = GridPropertyAccessor.ValueType<TItem>(Property);
        return FilterType == OmniDataGridColumnFilterType.Text
            && (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
            ? OmniDataGridColumnFilterType.DateRange
            : FilterType;
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
        && ReferenceEquals(left.FilterValues, right.FilterValues)
        && Equals(left.FilterTemplate, right.FilterTemplate)
        && Equals(left.Format, right.Format)
        && string.Equals(left.FormatString, right.FormatString, StringComparison.Ordinal)
        && left.Sortable == right.Sortable
        && left.SortOrder == right.SortOrder
        && left.Filterable == right.Filterable
        && left.FilterType == right.FilterType
        && left.FilterSearchable == right.FilterSearchable
        && Equals(left.FilterValueText, right.FilterValueText)
        && string.Equals(left.DefaultFilterValue, right.DefaultFilterValue, StringComparison.Ordinal)
        && left.FilterIncludesTime == right.FilterIncludesTime
        && ReferenceEquals(left.FilterOperators, right.FilterOperators)
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
