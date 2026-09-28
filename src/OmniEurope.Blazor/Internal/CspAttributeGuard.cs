namespace OmniEurope.Blazor.Internal;

internal static class CspAttributeGuard
{
    internal static void EnsureSafe(IReadOnlyDictionary<string, object>? attributes, Type component)
    {
        if (attributes is null)
        {
            return;
        }

        foreach (var attribute in attributes)
        {
            // HTML attributes are lowercase: a captured PascalCase name is a parameter the component does
            // not have (removed or misspelled), which would otherwise land in the markup silently. The
            // shipped analyzer (OE0001) reports it at build; this is the backstop for everything else.
            if (attribute.Key.Length > 0 && attribute.Key[0] is >= 'A' and <= 'Z')
            {
                throw new InvalidOperationException($"{ComponentName(component)} has no parameter '{attribute.Key}'.");
            }

            if (string.Equals(attribute.Key, "style", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Inline style attributes are forbidden by the OmniEurope.Blazor CSP contract. Use a CSS class instead.");
            }

            if (attribute.Key.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Inline event handler '{attribute.Key}' is forbidden by the OmniEurope.Blazor CSP contract. Declare the EventCallback on the component instead.");
            }
        }
    }

    private static string ComponentName(Type component)
    {
        var name = component.Name;
        var arity = name.IndexOf('`', StringComparison.Ordinal);
        return arity < 0 ? name : name[..arity];
    }
}
