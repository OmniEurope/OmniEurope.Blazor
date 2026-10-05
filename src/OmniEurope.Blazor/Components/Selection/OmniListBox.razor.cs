namespace OmniEurope.Blazor.Components;

/// <summary>
/// An always-open native list (<c>select</c> with a <c>size</c>) of <see cref="VisibleRows"/> rows. It
/// selects one option, or several with <see cref="Multiple"/>.
/// </summary>
/// <typeparam name="TValue">The value of each option.</typeparam>
/// <typeparam name="TSelection">
/// What <c>@bind-Value</c> binds, inferred from it: a <typeparamref name="TValue"/> (or its nullable form)
/// for a single selection, a collection that a <typeparamref name="TValue"/> array can be assigned to
/// (<see cref="IReadOnlyList{T}"/>, an array) when <see cref="Multiple"/> is set. Any other pairing
/// throws <see cref="InvalidOperationException"/>.
/// </typeparam>
public partial class OmniListBox<TValue, TSelection>
{
    /// <summary>The options, in order; a disabled option is listed but cannot be selected.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    /// <summary>The height of the list, in rows.</summary>
    [Parameter]
    public int VisibleRows { get; set; } = 5;

    /// <summary>
    /// Lets the reader select several options (Ctrl or Shift with a click, Shift with the arrows): the
    /// list becomes a <c>select multiple</c> and binds the selected values, in the order of
    /// <see cref="Options"/>. Off by default, which binds one value.
    /// </summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Whether the list is disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Identifiers of the elements that describe the list, written as <c>aria-describedby</c>; none when null.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private bool IsSelected(TValue value) => Multiple
        ? CurrentValue is IEnumerable<TValue> selected && selected.Contains(value, EqualityComparer<TValue>.Default)
        // Without Multiple the selection is a TValue or its nullable form (EnsureSelectionType).
        : CurrentValue is null ? value is null : EqualityComparer<TValue>.Default.Equals((TValue)(object)CurrentValue, value);

    /// <summary>Checks that <typeparamref name="TSelection"/> fits <see cref="Multiple"/>.</summary>
    /// <exception cref="InvalidOperationException">
    /// With <see cref="Multiple"/>, <typeparamref name="TSelection"/> cannot hold a <typeparamref name="TValue"/> array;
    /// without it, <typeparamref name="TSelection"/> is neither <typeparamref name="TValue"/> nor its nullable form.
    /// </exception>
    protected override void OnParametersSet()
    {
        EnsureSelectionType();
        base.OnParametersSet();
    }

    private void EnsureSelectionType()
    {
        var single = typeof(TSelection) == typeof(TValue) || Nullable.GetUnderlyingType(typeof(TSelection)) == typeof(TValue);
        var many = typeof(TSelection).IsAssignableFrom(typeof(TValue[]));
        if (Multiple && !many)
        {
            throw new InvalidOperationException(
                $"OmniListBox with Multiple binds a collection of {typeof(TValue).Name} (IReadOnlyList<{typeof(TValue).Name}>), not a {typeof(TSelection).Name}.");
        }

        if (!Multiple && !single)
        {
            throw new InvalidOperationException(
                $"OmniListBox binds one {typeof(TValue).Name}, not a {typeof(TSelection).Name}: set Multiple=\"true\" to bind several values.");
        }
    }

    private void HandleChange(ChangeEventArgs args)
    {
        if (Multiple)
        {
            var keys = args.Value switch
            {
                string[] values => values,
                IEnumerable<string> values => values.ToArray(),
                string value => [value],
                _ => Array.Empty<string>()
            };

            TValue[] selected = [.. keys
                .Select(key => int.TryParse(key, out var index) ? index : -1)
                .Where(index => index >= 0 && index < Options.Count && !Options[index].Disabled)
                .Select(index => Options[index].Value)];
            CurrentValue = (TSelection)(object)selected;
            return;
        }

        if (int.TryParse(args.Value?.ToString(), out var position) && position >= 0 && position < Options.Count && !Options[position].Disabled)
        {
            CurrentValue = (TSelection)(object)Options[position].Value!;
        }
    }

    /// <summary>
    /// For a single selection, reads the text as the index of an option in <see cref="Options"/> and takes
    /// that option's value; a disabled option cannot be chosen this way, and a multiple selection is never
    /// parsed from text.
    /// </summary>
    /// <param name="value">The index of the option, as text.</param>
    /// <param name="result">The value of the option, or the default value on failure.</param>
    /// <param name="validationErrorMessage">Null on success; on failure, the localized "invalid selection" message.</param>
    /// <returns>True when the list is single and the text is the index of an enabled option.</returns>
    protected override bool TryParseValueFromString(string? value, out TSelection result, out string validationErrorMessage)
    {
        if (!Multiple && int.TryParse(value, out var index) && index >= 0 && index < Options.Count && !Options[index].Disabled)
        {
            result = (TSelection)(object)Options[index].Value!;
            validationErrorMessage = null!;
            return true;
        }

        result = default!;
        validationErrorMessage = Localize("ListBoxInvalid");
        return false;
    }
}
