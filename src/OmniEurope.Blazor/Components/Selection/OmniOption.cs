namespace OmniEurope.Blazor.Components;

/// <summary>One choice of a selection component (drop-down, list box, radio or check box list, select bar...).</summary>
/// <typeparam name="TValue">The type of the value the choice stands for.</typeparam>
/// <param name="Value">The value the component takes when the choice is picked.</param>
/// <param name="Text">What the choice shows.</param>
/// <param name="Disabled">Whether the choice is shown but cannot be picked; false by default.</param>
/// <param name="Group">The heading <see cref="OmniDropDown{TValue}"/> lists the choice under; null (the default) for none. The other components ignore it.</param>
public sealed record OmniOption<TValue>(TValue Value, string Text, bool Disabled = false, string? Group = null);
