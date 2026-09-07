namespace OmniEurope.Blazor.Internal;

internal static class CssClassBuilder
{
    internal static string Combine(IEnumerable<string?> values) =>
        string.Join(' ', values.Where(static value => !string.IsNullOrWhiteSpace(value)));
}

