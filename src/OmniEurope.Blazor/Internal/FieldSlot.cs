namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The value a field's input shows, held in an object so that the input's value expression is a member access
/// (<c>slot.Value</c>), which is all a form field identifier reads; a method call such as <c>ValueOf(field)</c> is refused.
/// </summary>
/// <typeparam name="T">The type of the value the input binds.</typeparam>
/// <param name="value">The value at the time the editor of the field was drawn.</param>
internal sealed class FieldSlot<T>(T? value)
{
    /// <summary>The value, as drawn.</summary>
    internal T? Value { get; set; } = value;
}
