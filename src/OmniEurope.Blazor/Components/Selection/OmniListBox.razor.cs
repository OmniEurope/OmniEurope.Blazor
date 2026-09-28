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

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private bool IsSelected(TValue value) => Multiple
        ? CurrentValue is IEnumerable<TValue> selected && selected.Contains(value, EqualityComparer<TValue>.Default)
        : CurrentValue is null ? value is null : CurrentValue is TValue current && EqualityComparer<TValue>.Default.Equals(current, value);

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

    protected override bool TryParseValueFromString(string? value, out TSelection result, out string validationErrorMessage)
    {
        if (!Multiple && int.TryParse(value, out var index) && index >= 0 && index < Options.Count)
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
