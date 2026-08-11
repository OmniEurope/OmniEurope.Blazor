namespace OmniEurope.Blazor.Components;

public partial class OmniDataGridColumn<TItem>
{
    private OmniDataGridContext<TItem>? _registeredContext;
    private OmniDataGridColumnDefinition<TItem>? _definition;
    private string? _registeredKey;

    [CascadingParameter]
    private OmniDataGridContext<TItem>? Context { get; set; }

    [Parameter, EditorRequired]
    public string Key { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public Func<TItem, object?> Value { get; set; } = default!;

    [Parameter]
    public RenderFragment<TItem>? Template { get; set; }

    [Parameter]
    public RenderFragment<TItem>? EditTemplate { get; set; }

    [Parameter]
    public Func<TItem, string, bool>? FilterPredicate { get; set; }

    [Parameter]
    public Func<object?, string>? Format { get; set; }

    [Parameter]
    public bool Sortable { get; set; } = true;

    [Parameter]
    public bool Filterable { get; set; }

    [Parameter]
    public OmniDataGridFilterOperator FilterOperator { get; set; }

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public bool Resizable { get; set; }

    [Parameter]
    public OmniDataGridColumnWidth Width { get; set; }

    protected override void OnParametersSet()
    {
        if (_registeredContext is not null && (!ReferenceEquals(_registeredContext, Context) || !string.Equals(_registeredKey, Key, StringComparison.Ordinal)))
        {
            _registeredContext.Unregister(_registeredKey!);
            _definition = null;
        }

        var definition = new OmniDataGridColumnDefinition<TItem>
        {
            Key = Key,
            Title = Title,
            Value = Value,
            Template = Template,
            EditTemplate = EditTemplate,
            FilterPredicate = FilterPredicate,
            Format = Format,
            Sortable = Sortable,
            Filterable = Filterable,
            FilterOperator = FilterOperator,
            Visible = Visible,
            Resizable = Resizable,
            Width = Width
        };
        if (Context is not null && !Matches(_definition, definition))
        {
            Context.Register(definition);
            _registeredContext = Context;
            _registeredKey = Key;
            _definition = definition;
        }
    }

    public void Dispose() => _registeredContext?.Unregister(_registeredKey!);

    private static bool Matches(OmniDataGridColumnDefinition<TItem>? left, OmniDataGridColumnDefinition<TItem> right) =>
        left is not null
        && left.Key == right.Key
        && left.Title == right.Title
        && Equals(left.Value, right.Value)
        && Equals(left.Template, right.Template)
        && Equals(left.EditTemplate, right.EditTemplate)
        && Equals(left.FilterPredicate, right.FilterPredicate)
        && Equals(left.Format, right.Format)
        && left.Sortable == right.Sortable
        && left.Filterable == right.Filterable
        && left.FilterOperator == right.FilterOperator
        && left.Visible == right.Visible
        && left.Resizable == right.Resizable
        && left.Width == right.Width;
}
