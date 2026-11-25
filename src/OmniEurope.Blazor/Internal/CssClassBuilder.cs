namespace OmniEurope.Blazor.Components;

internal static class CssClassBuilder
{
    internal static string Combine(IEnumerable<string?> values) =>
        string.Join(' ', values.Where(static value => !string.IsNullOrWhiteSpace(value)));
}

