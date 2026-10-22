namespace OmniEurope.Blazor.Components;

/// <summary>
/// A group of <see cref="OmniSelectableCard"/>s that binds the selection: one choice
/// (<c>role="radiogroup"</c>, the default) or several (<see cref="Multiple"/>, <c>role="group"</c> of
/// check boxes). The cards come from <see cref="Options"/>, or are written inside the group with their
/// <see cref="OmniSelectableCard.Choice"/>, or both (options first). In a radio group only one card is
/// in the tab order (the chosen one, else the first available); the arrow keys, Home and End move the
/// choice and the focus among the available cards, as in a native radio group.
/// </summary>
/// <typeparam name="TValue">The value each card stands for.</typeparam>
/// <typeparam name="TSelection">
/// What <c>@bind-Value</c> binds, inferred from it: a <typeparamref name="TValue"/> (or its nullable form)
/// for one choice, a collection that a <typeparamref name="TValue"/> array can be assigned to
/// (<see cref="IReadOnlyList{T}"/>, an array) when <see cref="Multiple"/> is set, as
/// <see cref="OmniListBox{TValue, TSelection}"/> does. Any other pairing throws
/// <see cref="InvalidOperationException"/>. With cards written inside the group and no
/// <see cref="Options"/>, both type arguments are written explicitly.
/// </typeparam>
public partial class OmniSelectableCardGroup<TValue, TSelection> : IOmniSelectableCardGroup
{
    private readonly List<OmniSelectableCard> _cards = [];

    /// <summary>Cards drawn from options: the text is the title, a disabled option a disabled card.</summary>
    [Parameter]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = [];

    /// <summary>Cards written inside the group, each with its <see cref="OmniSelectableCard.Choice"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Several choices: the cards are check boxes and the group binds the chosen values, in the order of
    /// the cards (values the cards do not show are kept after them). Off by default: one choice.
    /// </summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Disables every card of the group; they stay focusable and are announced as unavailable.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the selection can be read and the cards focused but not changed: a click or a key leaves the
    /// value as it is, and the cards are not dimmed. Off by default.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Accessible name of the group (<c>aria-label</c>), which a radio group needs. Null names it by the
    /// label of an enclosing <see cref="OmniFormField"/> whose <c>For</c> is
    /// <see cref="OmniInputBase{TValue}.Id"/> (<c>aria-labelledby</c>), else leaves it unnamed.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Whether the cards of a group of check boxes (<see cref="Multiple"/>) draw a visible box, checked when
    /// the card is chosen (recette R2-4), decorative beside the card's own <c>role="checkbox"</c>. True by
    /// default; false keeps the frame and tint alone. A radio group never draws a box.
    /// </summary>
    [Parameter]
    public bool ShowCheckBox { get; set; } = true;

    /// <summary>
    /// A number of columns, from 1 to 6, lays the cards out as an <see cref="OmniTileGrid"/> does: equal
    /// widths, one height, <see cref="OmniSelectableCard.DoubleWidth"/> on two columns. Null, the default,
    /// stacks the cards one under the other, as before.
    /// </summary>
    [Parameter]
    public int? Columns { get; set; }

    bool IOmniSelectableCardGroup.ShowCheckBox => ShowCheckBox;

    bool IOmniSelectableCardGroup.Multiple => Multiple;

    bool IOmniSelectableCardGroup.Disabled => Disabled;

    bool IOmniSelectableCardGroup.ReadOnly => ReadOnly;

    /// <summary>Checks the bound type against <see cref="Multiple"/>, and redraws the cards for the value.</summary>
    protected override void OnParametersSet()
    {
        if (Columns is < 1 or > OmniTileGrid.MaxColumns)
        {
            throw new ArgumentOutOfRangeException(nameof(Columns), Columns, "A card group has between 1 and 6 columns.");
        }

        EnsureSelectionType();
        base.OnParametersSet();
        RefreshCards();
    }

    void IOmniSelectableCardGroup.Register(OmniSelectableCard card)
    {
        if (!TryChoice(card, out _))
        {
            throw new InvalidOperationException(
                $"The OmniSelectableCard '{card.Title}' stands for a {card.Choice?.GetType().Name ?? "null"}; its OmniSelectableCardGroup holds {typeof(TValue).Name} values.");
        }

        // A card registers once, when it first renders inside the group.
        _cards.Add(card);
    }

    void IOmniSelectableCardGroup.Unregister(OmniSelectableCard card) => _cards.Remove(card);

    bool IOmniSelectableCardGroup.IsSelected(OmniSelectableCard card) => IsSelected(card);

    bool IOmniSelectableCardGroup.IsTabStop(OmniSelectableCard card) => ReferenceEquals(TabStop(), card);

    Task IOmniSelectableCardGroup.PickAsync(OmniSelectableCard card)
    {
        Pick(card);
        return Task.CompletedTask;
    }

    async Task IOmniSelectableCardGroup.MoveAsync(OmniSelectableCard card, string key)
    {
        // Only a card of a radio group calls it (OmniSelectableCard.HandleKeyDownAsync).
        if (key is not ("ArrowRight" or "ArrowDown" or "ArrowLeft" or "ArrowUp" or "Home" or "End"))
        {
            return;
        }

        var available = _cards.Where(candidate => !candidate.IsDisabled).ToList();
        if (available.Count == 0)
        {
            return;
        }

        var index = available.IndexOf(card);
        var next = key switch
        {
            "Home" => 0,
            "End" => available.Count - 1,
            "ArrowLeft" or "ArrowUp" => index <= 0 ? available.Count - 1 : index - 1,
            _ => index < 0 || index == available.Count - 1 ? 0 : index + 1
        };
        var target = available[next];
        Pick(target);
        await target.FocusAsync();
    }

    private void Pick(OmniSelectableCard card)
    {
        // A read-only group never gets here either: its cards refuse the click and the arrows (IsReadOnly).
        // A disabled card never gets here: it refuses its own click, and the arrows skip it. Its choice may
        // have turned to another type since it registered.
        if (!TryChoice(card, out var choice))
        {
            return;
        }

        if (Multiple)
        {
            var chosen = new List<TValue>();
            foreach (var candidate in _cards)
            {
                if (TryChoice(candidate, out var value) && (ReferenceEquals(candidate, card) ? !Contains(value) : Contains(value)))
                {
                    chosen.Add(value);
                }
            }

            var shown = _cards.Select(candidate => TryChoice(candidate, out var value) ? value : default!).ToList();
            chosen.AddRange(SelectedValues().Where(value => !shown.Contains(value, EqualityComparer<TValue>.Default)));
            CurrentValue = (TSelection)(object)chosen.ToArray();
        }
        else
        {
            CurrentValue = (TSelection)(object?)choice!;
        }

        RefreshCards();
    }

    private bool IsSelected(OmniSelectableCard card) => TryChoice(card, out var choice) && Contains(choice);

    private bool Contains(TValue value) => SelectedValues().Contains(value, EqualityComparer<TValue>.Default);

    private IEnumerable<TValue> SelectedValues() => Multiple
        ? CurrentValue as IEnumerable<TValue> ?? []
        // One choice binds TValue or its nullable form (EnsureSelectionType): a value that is no TValue is null.
        : CurrentValue is TValue current ? [current] : default(TValue) is null ? [default!] : [];

    /// <summary>The card Tab reaches in a radio group: the chosen available card, else the first available one.</summary>
    private OmniSelectableCard? TabStop() =>
        _cards.FirstOrDefault(card => !card.IsDisabled && IsSelected(card))
        ?? _cards.FirstOrDefault(card => !card.IsDisabled)
        ?? _cards.FirstOrDefault();

    private static bool TryChoice(OmniSelectableCard card, out TValue value)
    {
        switch (card.Choice)
        {
            case TValue choice:
                value = choice;
                return true;
            case null when default(TValue) is null:
                value = default!;
                return true;
            default:
                value = default!;
                return false;
        }
    }

    // The cards' own parameters do not change when the selection does, so Blazor would skip them: each
    // is asked to draw its state again.
    private void RefreshCards()
    {
        foreach (var card in _cards)
        {
            card.Refresh();
        }
    }

    private void EnsureSelectionType()
    {
        var single = typeof(TSelection) == typeof(TValue) || Nullable.GetUnderlyingType(typeof(TSelection)) == typeof(TValue);
        var many = typeof(TSelection).IsAssignableFrom(typeof(TValue[]));
        if (Multiple && !many)
        {
            throw new InvalidOperationException(
                $"OmniSelectableCardGroup with Multiple binds a collection of {typeof(TValue).Name} (IReadOnlyList<{typeof(TValue).Name}>), not a {typeof(TSelection).Name}.");
        }

        if (!Multiple && !single)
        {
            throw new InvalidOperationException(
                $"OmniSelectableCardGroup binds one {typeof(TValue).Name}, not a {typeof(TSelection).Name}: set Multiple=\"true\" to bind several values.");
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TSelection result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("SelectableCardGroupInvalid");
        return false;
    }
}
