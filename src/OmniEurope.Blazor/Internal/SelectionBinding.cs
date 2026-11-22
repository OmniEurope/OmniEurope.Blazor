namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The check shared by the selection components that bind one value or several (<c>OmniListBox</c>,
/// <c>OmniSelectableCardGroup</c>): the bound type must fit the <c>Multiple</c> parameter.
/// </summary>
internal static class SelectionBinding
{
    /// <summary>
    /// Throws when <typeparamref name="TSelection"/> does not fit <paramref name="multiple"/>: with it, the bound type must
    /// hold a <typeparamref name="TValue"/> array; without it, it must be <typeparamref name="TValue"/> or its nullable form.
    /// </summary>
    /// <param name="multiple">The component's <c>Multiple</c> parameter.</param>
    /// <param name="component">The component name the message starts with.</param>
    internal static void EnsureSelectionType<TValue, TSelection>(bool multiple, string component)
    {
        var single = typeof(TSelection) == typeof(TValue) || Nullable.GetUnderlyingType(typeof(TSelection)) == typeof(TValue);
        var many = typeof(TSelection).IsAssignableFrom(typeof(TValue[]));
        if (multiple && !many)
        {
            throw new InvalidOperationException(
                $"{component} with Multiple binds a collection of {typeof(TValue).Name} (IReadOnlyList<{typeof(TValue).Name}>), not a {typeof(TSelection).Name}.");
        }

        if (!multiple && !single)
        {
            throw new InvalidOperationException(
                $"{component} binds one {typeof(TValue).Name}, not a {typeof(TSelection).Name}: set Multiple=\"true\" to bind several values.");
        }
    }
}
