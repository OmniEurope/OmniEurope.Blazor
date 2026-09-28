namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The value handling shared by the two-state and three-state toggles (<c>OmniCheckBox</c> and
/// <c>OmniSwitch</c>): their value type is <see cref="bool"/> or <see cref="Nullable{T}">bool?</see>,
/// read and written here as a <c>bool?</c> whatever it is.
/// </summary>
internal static class OmniBooleanValue<TValue>
{
    /// <summary>Whether the value type is <c>bool?</c>, which adds the null (indeterminate) state.</summary>
    internal static readonly bool IsNullable = typeof(TValue) == typeof(bool?);

    private static readonly bool IsSupported = IsNullable || typeof(TValue) == typeof(bool);

    /// <summary>Throws when the toggle is bound to anything but a <c>bool</c> or a <c>bool?</c>.</summary>
    internal static void EnsureSupported(string component)
    {
        if (!IsSupported)
        {
            throw new InvalidOperationException(
                $"{component} binds a bool or a bool?, not a {typeof(TValue).Name}: bind @bind-Value to a bool property (two states) or a bool? property (three states).");
        }
    }

    internal static bool? Read(TValue? value) => value is bool state ? state : null;

    /// <summary>The value for <paramref name="state"/>; null only reaches a <c>bool?</c>.</summary>
    internal static TValue Write(bool? state) => state is { } value ? (TValue)(object)value : default!;

    /// <summary>
    /// The state a click leads to: null goes to true, true to false, and false back to null when the
    /// value type has a third state and it is allowed, to true otherwise.
    /// </summary>
    internal static bool? Next(bool? state, bool allowIndeterminate) => state switch
    {
        null => true,
        true => false,
        _ when IsNullable && allowIndeterminate => null,
        _ => true
    };

    /// <summary>
    /// Parses what an <c>EditContext</c> supplies: <c>true</c> or <c>false</c> in any case, and, for a
    /// <c>bool?</c>, an empty string as null.
    /// </summary>
    internal static bool TryParse(string? value, out TValue result)
    {
        if (IsNullable && string.IsNullOrEmpty(value))
        {
            result = default!;
            return true;
        }

        if (bool.TryParse(value, out var parsed))
        {
            result = (TValue)(object)parsed;
            return true;
        }

        result = default!;
        return false;
    }
}
