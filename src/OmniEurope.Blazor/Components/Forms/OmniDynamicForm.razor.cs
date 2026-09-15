namespace OmniEurope.Blazor.Components;

/// <summary>
/// A form drawn from a schema: one <see cref="OmniDynamicField"/> per field, each edited with the
/// control of its kind (text, several lines, number, yes or no, choice), its label with the required
/// marker, its help text and its validation message. Values go in and out as one dictionary of
/// strings, bound in both directions.
/// </summary>
/// <remarks>
/// <para>
/// Values are written as a form would send them: <c>true</c> or <c>false</c>, a number with a dot as
/// the decimal separator, the value of the chosen option, the text. A field left empty has no entry.
/// A required yes or no field with no default stays unanswered until the switch is touched, so the
/// required check cannot be satisfied by a default the user never saw.
/// </para>
/// <para>
/// <see cref="Validate"/> checks every field and shows every message. Inside an
/// <see cref="EditForm"/>, the form's own validation does the same, so a submit is refused while a
/// field is invalid; outside one, the component keeps an edit context of its own. A field is also
/// checked as soon as it changes. <see cref="Errors"/> adds messages the host found itself, a server
/// refusal for instance.
/// </para>
/// </remarks>
public partial class OmniDynamicForm : IDisposable
{
    private readonly string _generatedId = $"omni-dynamic-form-{Guid.NewGuid():N}";
    private readonly Dictionary<string, FieldState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _messages = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _shown = new(StringComparer.OrdinalIgnoreCase);
    private EditContext _context = default!;
    private ValidationMessageStore _store = default!;
    private IReadOnlyList<OmniDynamicField>? _observedFields;
    private IReadOnlyDictionary<string, string>? _observedValues;
    private IReadOnlyDictionary<string, string>? _emittedValues;
    private bool _seedPending;

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    /// <summary>The fields, in the order they are shown.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniDynamicField> Fields { get; set; } = Array.Empty<OmniDynamicField>();

    /// <summary>The values by field name, written as strings. A field without an entry has no value.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string>? Values { get; set; }

    /// <summary>Raised with a new dictionary whenever a value changes, and once for the defaults given at start.</summary>
    [Parameter]
    public EventCallback<IReadOnlyDictionary<string, string>> ValuesChanged { get; set; }

    /// <summary>Messages of the host by field name, shown under their field until it changes.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string>? Errors { get; set; }

    /// <summary>Disables every control.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The values the form holds now, written as <see cref="ValuesChanged"/> reports them.</summary>
    public IReadOnlyDictionary<string, string> CurrentValues => Snapshot();

    /// <summary>Whether every field currently passes its checks, without showing any message.</summary>
    public bool IsValid => Fields.All(candidate => Check(candidate) is null);

    protected override void OnInitialized()
    {
        _context = CascadedEditContext ?? new EditContext(_states);
        _store = new ValidationMessageStore(_context);
        _context.OnValidationRequested += HandleValidationRequested;
    }

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Fields);
        if (Fields.Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Fields.Count)
        {
            throw new ArgumentException($"Every {nameof(OmniDynamicField)} of an {nameof(OmniDynamicForm)} needs a distinct {nameof(OmniDynamicField.Name)}.", nameof(Fields));
        }

        var fieldsChanged = !ReferenceEquals(Fields, _observedFields);
        var valuesChanged = !ReferenceEquals(Values, _observedValues) && !ReferenceEquals(Values, _emittedValues);
        _observedFields = Fields;
        _observedValues = Values;

        if (fieldsChanged)
        {
            foreach (var name in _states.Keys.Where(name => Fields.All(field => !string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))).ToArray())
            {
                _states.Remove(name);
                _messages.Remove(name);
                _shown.Remove(name);
            }
        }

        if (fieldsChanged || valuesChanged)
        {
            foreach (var field in Fields)
            {
                if (!_states.TryGetValue(field.Name, out var state))
                {
                    state = new FieldState();
                    _states[field.Name] = state;
                }
                else if (!valuesChanged)
                {
                    continue;
                }

                if (Values is not null && Values.TryGetValue(field.Name, out var value))
                {
                    state.Load(field.Kind, value);
                }
                else if (field.DefaultValue is not null)
                {
                    state.Load(field.Kind, field.DefaultValue);
                    _seedPending = true;
                }
                else
                {
                    state.Load(field.Kind, null);
                }
            }

            _store.Clear();
            foreach (var field in Fields.Where(field => _shown.Contains(field.Name)))
            {
                Revalidate(field);
            }
        }

        if (_seedPending)
        {
            _seedPending = false;
            await EmitAsync();
        }
    }

    /// <summary>Checks every field, shows every message, and tells whether the form can be sent.</summary>
    public bool Validate()
    {
        foreach (var field in Fields)
        {
            _shown.Add(field.Name);
            Revalidate(field, notify: false);
        }

        _context.NotifyValidationStateChanged();
        StateHasChanged();
        return _messages.Count == 0;
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
    {
        foreach (var field in Fields)
        {
            _shown.Add(field.Name);
            Revalidate(field, notify: false);
        }

        _ = InvokeAsync(StateHasChanged);
    }

    private Task AnsweredAsync(OmniDynamicField field, FieldState state)
    {
        state.Answered = true;
        return ChangedAsync(field);
    }

    private async Task ChangedAsync(OmniDynamicField field)
    {
        _states[field.Name].InvalidNumber = null;
        _shown.Add(field.Name);
        Revalidate(field);
        await EmitAsync();
    }

    private async Task EmitAsync()
    {
        var snapshot = Snapshot();
        _emittedValues = snapshot;
        await ValuesChanged.InvokeAsync(snapshot);
    }

    private Dictionary<string, string> Snapshot()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in Fields)
        {
            if (_states.TryGetValue(field.Name, out var state) && state.Write(field.Kind) is { } value)
            {
                values[field.Name] = value;
            }
        }

        return values;
    }

    private void Revalidate(OmniDynamicField field, bool notify = true)
    {
        var state = _states[field.Name];
        var identifier = state.Identifier(field.Kind);
        _store.Clear(identifier);
        if (Check(field) is { } message)
        {
            _messages[field.Name] = message;
            _store.Add(identifier, message);
        }
        else
        {
            _messages.Remove(field.Name);
        }

        if (notify)
        {
            _context.NotifyValidationStateChanged();
        }
    }

    /// <summary>The message a field deserves now, or null when it passes.</summary>
    private string? Check(OmniDynamicField field)
    {
        var state = _states[field.Name];
        switch (field.Kind)
        {
            case OmniDynamicFieldKind.Boolean:
                return field.Required && !state.Answered ? Localize("DynamicFormRequired", field.Label) : null;
            case OmniDynamicFieldKind.Number:
                if (state.InvalidNumber is not null)
                {
                    return Localize("DynamicFormNumber", field.Label);
                }

                if (state.Number is not { } number)
                {
                    return field.Required ? Localize("DynamicFormRequired", field.Label) : null;
                }

                if (field.Minimum is { } minimum && number < minimum)
                {
                    return Localize("DynamicFormMinimum", field.Label, minimum.ToString(CultureInfo.CurrentCulture));
                }

                return field.Maximum is { } maximum && number > maximum
                    ? Localize("DynamicFormMaximum", field.Label, maximum.ToString(CultureInfo.CurrentCulture))
                    : null;
            case OmniDynamicFieldKind.Choice:
                if (string.IsNullOrEmpty(state.Text))
                {
                    return field.Required ? Localize("DynamicFormRequired", field.Label) : null;
                }

                return field.Options.Any(option => string.Equals(option.Value, state.Text, StringComparison.Ordinal))
                    ? null
                    : Localize("DynamicFormChoice", field.Label);
            default:
                return field.Required && string.IsNullOrWhiteSpace(state.Text) ? Localize("DynamicFormRequired", field.Label) : null;
        }
    }

    private string? ErrorOf(OmniDynamicField field)
    {
        if (_shown.Contains(field.Name) && _messages.TryGetValue(field.Name, out var message))
        {
            return message;
        }

        return Errors is not null && Errors.TryGetValue(field.Name, out var external) && !_shown.Contains(field.Name) ? external : null;
    }

    private string FieldId(int index) => $"{Id ?? _generatedId}-{index}";

    private static string? DescribedBy(string fieldId, OmniDynamicField field, string? error)
    {
        var ids = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(field.Description))
        {
            ids.Add($"{fieldId}-description");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            ids.Add($"{fieldId}-error");
        }

        return ids.Count == 0 ? null : string.Join(' ', ids);
    }

    private static string? RequiredAttribute(OmniDynamicField field) => field.Required ? "true" : null;

    private static string? Invariant(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static string KindName(OmniDynamicFieldKind kind) => kind switch
    {
        OmniDynamicFieldKind.MultilineText => "multiline",
        _ => kind.ToString().ToLowerInvariant()
    };

    public void Dispose()
    {
        _context.OnValidationRequested -= HandleValidationRequested;
        _store.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The edited value of one field. Each kind binds its own member, so that the edit context sees a
    /// real model member (the field identifier of the control) and not a computed expression.
    /// </summary>
    private sealed class FieldState
    {
        public string? Text { get; set; }

        public decimal? Number { get; set; }

        public bool Flag { get; set; }

        public bool Answered { get; set; }

        public string? InvalidNumber { get; set; }

        public FieldIdentifier Identifier(OmniDynamicFieldKind kind) => kind switch
        {
            OmniDynamicFieldKind.Boolean => new FieldIdentifier(this, nameof(Flag)),
            OmniDynamicFieldKind.Number => new FieldIdentifier(this, nameof(Number)),
            _ => new FieldIdentifier(this, nameof(Text))
        };

        public void Load(OmniDynamicFieldKind kind, string? value)
        {
            Text = null;
            Number = null;
            Flag = false;
            Answered = false;
            InvalidNumber = null;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            switch (kind)
            {
                case OmniDynamicFieldKind.Boolean:
                    if (bool.TryParse(value, out var flag))
                    {
                        Flag = flag;
                        Answered = true;
                    }

                    break;
                case OmniDynamicFieldKind.Number:
                    if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    {
                        Number = number;
                    }
                    else
                    {
                        InvalidNumber = value;
                    }

                    break;
                default:
                    Text = value;
                    break;
            }
        }

        public string? Write(OmniDynamicFieldKind kind) => kind switch
        {
            OmniDynamicFieldKind.Boolean => Answered ? (Flag ? "true" : "false") : null,
            OmniDynamicFieldKind.Number => Number?.ToString(CultureInfo.InvariantCulture) ?? InvalidNumber,
            _ => string.IsNullOrEmpty(Text) ? null : Text
        };
    }
}
