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
            // Class and Id are parameters, placed by the component on the right element (Class on the
            // outermost, Id on the focusable control). A lowercase one handed over in a dictionary would
            // bypass that and replace the component's own class or id.
            if (string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Key, "id", StringComparison.OrdinalIgnoreCase))
            {
                var parameter = attribute.Key.Length == 5 ? "Class" : "Id";
                throw new InvalidOperationException(
                    $"{ComponentName(component)} does not take a '{attribute.Key}' attribute: use its '{parameter}' parameter.");
            }

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
