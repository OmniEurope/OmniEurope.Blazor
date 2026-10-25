namespace OmniEurope.Blazor.Components;

/// <summary>One choice of a selection component (drop-down, list box, radio or check box list, select bar...).</summary>
/// <typeparam name="TValue">The type of the value the choice stands for.</typeparam>
/// <param name="Value">The value the component takes when the choice is picked.</param>
/// <param name="Text">What the choice shows.</param>
/// <param name="Disabled">Whether the choice is shown but cannot be picked; false by default.</param>
/// <param name="Group">The heading <see cref="OmniDropDown{TValue}"/> lists the choice under (an <c>optgroup</c> in the native list, a heading in the filterable one, as <see cref="OmniAutocomplete{TValue}"/> draws it); null (the default) for none. The native list gathers the options of a group under one heading; <see cref="OmniAutocomplete{TValue}"/> keeps the order of the options and puts a heading before each run of consecutive options of the same group. The other components ignore it.</param>
public sealed record OmniOption<TValue>(TValue Value, string Text, bool Disabled = false, string? Group = null);
