namespace OmniEurope.Blazor.Internal;

internal static class CspAttributeGuard
{
    internal static void EnsureSafe(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        foreach (var attribute in attributes)
        {
            if (string.Equals(attribute.Key, "style", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Inline style attributes are forbidden by the OmniEurope.Blazor CSP contract. Use a CSS class instead.");
            }

            if (attribute.Key.StartsWith("on", StringComparison.OrdinalIgnoreCase) && attribute.Value is string)
            {
                throw new InvalidOperationException(
                    $"Inline event handler '{attribute.Key}' is forbidden by the OmniEurope.Blazor CSP contract. Use an EventCallback instead.");
            }
        }
    }
}

