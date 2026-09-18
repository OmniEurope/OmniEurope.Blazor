namespace OmniEurope.Blazor.Components;

/// <summary>One field of an <see cref="OmniDynamicForm"/>, described by data rather than markup.</summary>
/// <param name="Name">The key of its value in <see cref="OmniDynamicForm.Values"/>.</param>
/// <param name="Label">What the field is called on screen.</param>
/// <param name="Kind">The control it is edited with.</param>
public sealed record OmniDynamicField(string Name, string Label, OmniDynamicFieldKind Kind = OmniDynamicFieldKind.Text)
{
    /// <summary>Whether a value must be given. The label carries the required marker.</summary>
    public bool Required { get; init; }

    /// <summary>A help text under the label, read by screen readers with the control.</summary>
    public string? Description { get; init; }

    /// <summary>A hint shown in an empty text or number field.</summary>
    public string? Placeholder { get; init; }

    /// <summary>
    /// The value given when <see cref="OmniDynamicForm.Values"/> has none, written like the values
    /// themselves: <c>true</c> or <c>false</c>, a number with a dot, the value of an option.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>The choices of a <see cref="OmniDynamicFieldKind.Choice"/> field: the value stored and the text shown.</summary>
    public IReadOnlyList<OmniOption<string>> Options { get; init; } = Array.Empty<OmniOption<string>>();

    /// <summary>The smallest number accepted.</summary>
    public decimal? Minimum { get; init; }

    /// <summary>The largest number accepted.</summary>
    public decimal? Maximum { get; init; }

    /// <summary>The step of the number field's arrows.</summary>
    public decimal? Step { get; init; }

    /// <summary>The visible lines of a <see cref="OmniDynamicFieldKind.MultilineText"/> field.</summary>
    public int Rows { get; init; } = 4;
}
