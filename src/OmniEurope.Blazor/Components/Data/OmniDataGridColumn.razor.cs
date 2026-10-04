using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Declares one column of the enclosing <see cref="OmniDataGrid{TItem}"/>, inside its <c>Columns</c>
/// fragment. It renders nothing itself: it registers its definition with the grid, updates it when a
/// parameter changes, and removes it when it is disposed. Outside a grid it does nothing.
/// </summary>
/// <typeparam name="TItem">Type of the grid rows.</typeparam>
public partial class OmniDataGridColumn<TItem>
{
    private OmniDataGridContext<TItem>? _registeredContext;
    private OmniDataGridColumnDefinition<TItem>? _definition;
    private string? _registeredKey;

    [CascadingParameter]
    private OmniDataGridContext<TItem>? Context { get; set; }

    /// <summary>
    /// Stable identity of the column: the key of its sort, filter, width and group, in the grid's saved
    /// state and in <see cref="OmniDataGridLoadRequest"/>. Left empty it is <see cref="Property"/>, or
    /// <see cref="Title"/> when no property is set either.
    /// </summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>The header text, also the label of the cell in the stacked (responsive) layout and of the filter.</summary>
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

    /// <summary>Content of each body cell, in place of the value as text.</summary>
    [Parameter]
    public RenderFragment<TItem>? Template { get; set; }

    /// <summary>Content of the cell while its row is in edit mode; set on any column, the grid adds its edit column.</summary>
    [Parameter]
    public RenderFragment<TItem>? EditTemplate { get; set; }

    /// <summary>Content of this column's cell in the footer row (a total, a creation field); see the grid's <c>FooterPosition</c>.</summary>
    [Parameter]
    public RenderFragment? FooterContent { get; set; }

    /// <summary>Replaces the header's title (and its sort button) with custom content; the grouping, filter and resize controls stay.</summary>
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    /// <summary>
    /// Decides whether a row passes this column's filter, from the row and the filter value, in place of
    /// the built-in comparison. Applied to rows the grid holds (<c>Items</c>); a <c>Load</c> grid filters
    /// on the server.
    /// </summary>
    [Parameter]
    public Func<TItem, string, bool>? FilterPredicate { get; set; }

    /// <summary>
    /// Explicit list of Select/Combo suggestions. Left unset the grid derives them from the values
    /// this column reads, which only works when it reads a single value per row and the grid holds
    /// every row. Set it for a column built from a collection, or for a grid fed page by page.
    /// The values are offered in the order given (duplicates dropped), never sorted: a host lists
    /// them in their own sense, such as severity levels.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? FilterValues { get; set; }

    /// <summary>
    /// This column's own filter editor, overriding <see cref="FilterType"/>. Use it for a filter
    /// shape none of the built-in types covers; the context carries the current value, the candidate
    /// values and the callback that applies a new one.
    /// </summary>
    [Parameter]
    public RenderFragment<OmniDataGridFilterContext>? FilterTemplate { get; set; }

    /// <summary>
    /// The value this column writes in an export of the grid, in place of <see cref="Value"/> or
    /// <see cref="Property"/>: what a column made of a <see cref="Template"/> alone needs to be exported
    /// at all, and what a column whose template shows something else than its value (a translated
    /// status) needs to export that text. A number, a date or a boolean keeps its type in the file.
    /// </summary>
    [Parameter]
    public Func<TItem, object?>? ExportValue { get; set; }

    /// <summary>Whether an export of the grid writes this column. True by default; a column that reads no value is never written.</summary>
    [Parameter]
    public bool Exportable { get; set; } = true;

    /// <summary>Composite format applied to the cell value, for example <c>{0:n2}</c>.</summary>
    [Parameter]
    public string? FormatString { get; set; }

    /// <summary>Whether the header sorts the rows by this column, when the grid's <c>AllowSorting</c> is on. True by default.</summary>
    [Parameter]
    public bool Sortable { get; set; } = true;

    /// <summary>Initial sort applied to this column when the grid first renders.</summary>
    [Parameter]
    public OmniDataGridSortOrder? SortOrder { get; set; }

    /// <summary>
    /// Whether this column has a filter. Left unset it follows the grid's <c>Filterable</c> (on by default),
    /// as long as the column has something to filter on: a <see cref="Property"/>, a <see cref="Value"/>, a
    /// <see cref="FilterPredicate"/> or a <see cref="FilterTemplate"/>; a column made of a template alone
    /// (row actions) gets none. True or false decides for this column alone, whatever the grid says: false
    /// takes one column out of a filtered grid, true gives one column a filter in a grid whose
    /// <c>Filterable</c> is off.
    /// </summary>
    [Parameter]
    public bool? Filterable { get; set; }

    /// <summary>
    /// The filter editor: free text (the default, a date range for a date property), number, closed list,
    /// list with free text (combo), checkable list or date range.
    /// </summary>
    [Parameter]
    public OmniDataGridColumnFilterType FilterType { get; set; }

    /// <summary>
    /// Puts a narrowing box above the checkable list of a MultiSelect filter, for a column whose candidate
    /// list is too long to scan by eye; it is the <c>Filterable</c> option of that list. Ignored by the
    /// other filter types. Named after the column's own filter, which <see cref="Filterable"/> already
    /// switches on.
    /// </summary>
    [Parameter]
    public bool FilterSearchable { get; set; }

    /// <summary>
    /// The text a filter shows for one candidate value (an enum member's translated name, for
    /// example). The value itself is what the filter keeps and sends; only its display changes. The
    /// cell's own text is formatted by <see cref="FormatString"/> or <see cref="Template"/>.
    /// </summary>
    [Parameter]
    public Func<string, string>? FormatFilterValue { get; set; }

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

    /// <summary>
    /// The operator a text or number filter starts with (contains by default), when the column's type
    /// offers it; the first operator it offers otherwise.
    /// </summary>
    [Parameter]
    public OmniDataGridFilterOperator FilterOperator { get; set; }

    /// <summary>Whether the column is shown. A hidden column keeps its sort and filter. True by default.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether this column carries a resize handle. Left unset it follows the grid's
    /// <c>AllowColumnResize</c>, so turning resizing on once at grid level is enough; set it to
    /// false to keep one column fixed while the others can be resized.
    /// </summary>
    [Parameter]
    public bool? Resizable { get; set; }

    /// <summary>
    /// Whether a double click on this column's trailing edge (or Enter on its handle) sizes it to its
    /// widest content, as in Excel. Left unset it follows the grid's <c>AllowColumnAutoFit</c>; true or
    /// false decides for this column alone, whatever the grid says. Independent of
    /// <see cref="Resizable"/>, which only governs the drag.
    /// </summary>
    [Parameter]
    public bool? AutoFit { get; set; }

    /// <summary>Keeps the column visible against the inline start edge while the grid scrolls sideways.</summary>
    [Parameter]
    public bool Frozen { get; set; }

    /// <summary>CSS length applied to the column, for example <c>160px</c> or <c>12rem</c>.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>Minimum CSS length the column keeps while the table shrinks.</summary>
    [Parameter]
    public string? MinWidth { get; set; }

    /// <summary>
    /// Alignment of the header and the cells. Left at the start, a number column aligns at the end in
    /// figures of one width, unless it has a <see cref="Template"/>.
    /// </summary>
    [Parameter]
    public OmniDataGridTextAlign TextAlign { get; set; }

    /// <summary>
    /// CSS class added to each body cell and footer cell of the column. A templated column opts into the
    /// one-line ellipsis of text cells with <c>omni-data-grid__cell--text</c>.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>CSS class added to the column's header cell.</summary>
    [Parameter]
    public string? HeaderClass { get; set; }

    /// <summary>
    /// Whether this column is tinted under the pointer when the grid's <c>HighlightColumnOnHover</c> is on. True
    /// by default; false leaves the column out (a label column, a column of actions): hovering it tints nothing.
    /// </summary>
    [Parameter]
    public bool HighlightOnHover { get; set; } = true;

    /// <summary>Whether the reader can group rows by this column, when the grid's <c>AllowGrouping</c> is on. True by default.</summary>
    [Parameter]
    public bool Groupable { get; set; } = true;

    private string EffectiveKey => string.IsNullOrWhiteSpace(Key)
        ? Property ?? Title
        : Key;

    /// <summary>
    /// Registers the column with its grid, or registers it again when a parameter changed. A change of
    /// grid or of key first removes the previous registration; changed delegates alone are adopted in
    /// place without re-registering.
    /// </summary>
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
            FooterContent = FooterContent,
            HeaderContent = HeaderContent,
            FilterPredicate = FilterPredicate,
            FilterValues = FilterValues,
            FilterTemplate = FilterTemplate,
            FormatString = FormatString,
            Sortable = Sortable,
            SortOrder = SortOrder,
            Filterable = Filterable,
            HasFilterSource = Property is not null || Value is not null || FilterPredicate is not null || FilterTemplate is not null,
            FilterType = ResolveFilterType(),
            FilterSearchable = FilterSearchable,
            FormatFilterValue = FormatFilterValue,
            DefaultFilterValue = DefaultFilterValue,
            FilterIncludesTime = FilterIncludesTime,
            EnumType = GridPropertyAccessor.EnumType<TItem>(Property),
            ValueType = GridPropertyAccessor.ValueType<TItem>(Property),
            FilterOperators = FilterOperators,
            FilterOperator = FilterOperator,
            Visible = Visible,
            Resizable = Resizable,
            AutoFit = AutoFit,
            Frozen = Frozen,
            Width = Width,
            MinWidth = MinWidth,
            TextAlign = TextAlign,
            Numeric = GridPropertyAccessor.IsNumeric<TItem>(Property),
            Class = Class,
            HeaderClass = HeaderClass,
            Groupable = Groupable,
            HighlightOnHover = HighlightOnHover,
            ExportValue = ExportValue,
            Exportable = Exportable,
            HasValueSource = Property is not null || Value is not null
        };
        if (Context is null)
        {
            return;
        }

        if (Matches(_definition, definition))
        {
            if (AdoptDelegates(_definition!, definition))
            {
                Context.DelegatesAdopted();
            }

            return;
        }

        Context.Register(definition);
        _registeredContext = Context;
        _registeredKey = key;
        _definition = definition;
    }

    /// <summary>
    /// Keeps the latest delegates and filter lists of an unchanged column without re-registering it.
    /// A column declared in a <c>@foreach</c> receives new template and accessor delegates on every
    /// render, because each captures the loop variable, and a new list for a collection literal;
    /// re-registering on each of them re-rendered the grid, which rendered its columns again, without
    /// end. Returns whether a delegate now has another target (a closure over another loop value), so
    /// that the grid renders once more with it in the same render batch; a delegate that only captures
    /// the component is equal to the stored one and asks for nothing.
    /// </summary>
    private static bool AdoptDelegates(OmniDataGridColumnDefinition<TItem> registered, OmniDataGridColumnDefinition<TItem> latest)
    {
        var changed = !Equals(registered.Value, latest.Value)
            || !Equals(registered.Template, latest.Template)
            || !Equals(registered.EditTemplate, latest.EditTemplate)
            || !Equals(registered.FooterContent, latest.FooterContent)
            || !Equals(registered.HeaderContent, latest.HeaderContent)
            || !Equals(registered.FilterPredicate, latest.FilterPredicate)
            || !Equals(registered.FilterTemplate, latest.FilterTemplate)
            || !Equals(registered.FormatFilterValue, latest.FormatFilterValue);
        // Read only when an export runs, never while rendering: adopted without asking for a render.
        registered.ExportValue = latest.ExportValue;
        registered.Value = latest.Value;
        registered.Template = latest.Template;
        registered.EditTemplate = latest.EditTemplate;
        registered.FooterContent = latest.FooterContent;
        registered.HeaderContent = latest.HeaderContent;
        registered.FilterPredicate = latest.FilterPredicate;
        registered.FilterTemplate = latest.FilterTemplate;
        registered.FormatFilterValue = latest.FormatFilterValue;
        registered.FilterValues = latest.FilterValues;
        registered.FilterOperators = latest.FilterOperators;
        return changed;
    }

    /// <summary>
    /// Whether two filter lists hold the same values in the same order. A collection literal written
    /// in a <c>@foreach</c> is a new list on every render; comparing it by reference would see a change
    /// each time and re-register the column without end.
    /// </summary>
    private static bool SameContent<T>(IEnumerable<T>? left, IEnumerable<T>? right) =>
        ReferenceEquals(left, right)
        || (left is not null && right is not null && left.SequenceEqual(right));

    /// <summary>
    /// Whether two delegates come from the same lambda: same method and a target of the same type.
    /// Two closures of one lambda written in a loop differ only by the captured loop variable, so
    /// comparing them by reference would see a change on every render.
    /// </summary>
    private static bool Equivalent(Delegate? left, Delegate? right) =>
        Equals(left, right)
        || (left is not null
            && right is not null
            && left.Method == right.Method
            && left.Target?.GetType() == right.Target?.GetType());

    private OmniDataGridColumnFilterType ResolveFilterType()
    {
        var type = GridPropertyAccessor.ValueType<TItem>(Property);
        return FilterType == OmniDataGridColumnFilterType.Text
            && (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
            ? OmniDataGridColumnFilterType.DateRange
            : FilterType;
    }

    /// <summary>Removes the column from its grid.</summary>
    public void Dispose() => _registeredContext?.Unregister(_registeredKey!);

    private static bool Matches(OmniDataGridColumnDefinition<TItem>? left, OmniDataGridColumnDefinition<TItem> right) =>
        left is not null
        && left.Key == right.Key
        && left.Title == right.Title
        && Equivalent(left.Value, right.Value)
        && string.Equals(left.Property, right.Property, StringComparison.Ordinal)
        && string.Equals(left.SortProperty, right.SortProperty, StringComparison.Ordinal)
        && Equivalent(left.Template, right.Template)
        && Equivalent(left.EditTemplate, right.EditTemplate)
        && Equivalent(left.FooterContent, right.FooterContent)
        && Equivalent(left.HeaderContent, right.HeaderContent)
        && Equivalent(left.FilterPredicate, right.FilterPredicate)
        && SameContent(left.FilterValues, right.FilterValues)
        && Equivalent(left.FilterTemplate, right.FilterTemplate)
        && string.Equals(left.FormatString, right.FormatString, StringComparison.Ordinal)
        && left.Sortable == right.Sortable
        && left.SortOrder == right.SortOrder
        && left.Filterable == right.Filterable
        && left.HasFilterSource == right.HasFilterSource
        && left.FilterType == right.FilterType
        && left.FilterSearchable == right.FilterSearchable
        && Equivalent(left.FormatFilterValue, right.FormatFilterValue)
        && string.Equals(left.DefaultFilterValue, right.DefaultFilterValue, StringComparison.Ordinal)
        && left.FilterIncludesTime == right.FilterIncludesTime
        && SameContent(left.FilterOperators, right.FilterOperators)
        && left.FilterOperator == right.FilterOperator
        && left.Visible == right.Visible
        && left.Resizable == right.Resizable
        && left.AutoFit == right.AutoFit
        && left.Frozen == right.Frozen
        && string.Equals(left.Width, right.Width, StringComparison.Ordinal)
        && string.Equals(left.MinWidth, right.MinWidth, StringComparison.Ordinal)
        && left.TextAlign == right.TextAlign
        && string.Equals(left.Class, right.Class, StringComparison.Ordinal)
        && string.Equals(left.HeaderClass, right.HeaderClass, StringComparison.Ordinal)
        && left.Groupable == right.Groupable
        && left.HighlightOnHover == right.HighlightOnHover
        && Equivalent(left.ExportValue, right.ExportValue)
        && left.Exportable == right.Exportable
        && left.HasValueSource == right.HasValueSource;
}
