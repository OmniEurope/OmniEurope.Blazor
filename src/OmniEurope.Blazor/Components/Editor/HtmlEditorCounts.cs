using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The word and character counts of the value of an <see cref="OmniHtmlEditor"/>, computed again only
/// when the value changes: a render reads them several times, and counting parses the markup.
/// </summary>
internal sealed class HtmlEditorCounts
{
    private string? _value;
    private bool _counted;
    private (int Words, int Characters) _counts;

    /// <summary>The counts of <paramref name="value"/>, read as <see cref="OmniHtmlText.Count"/> does.</summary>
    internal (int Words, int Characters) Of(string? value, OmniHtmlSanitizerPolicy? policy, bool isSanitized)
    {
        if (!_counted || !string.Equals(value, _value, StringComparison.Ordinal))
        {
            _value = value;
            _counted = true;
            _counts = OmniHtmlText.Count(value, policy, isSanitized);
        }

        return _counts;
    }
}
