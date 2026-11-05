using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The markup under the text area of the source face, sanitised again only when the value or the policy
/// changes: every render of the editor reads it, and sanitising parses the whole value, which for a long
/// document (hundreds of kilobytes of tables) is a render that never catches up with the events behind it.
/// A value the editor has just sanitised itself (what a keystroke commits) is shown as it is.
/// </summary>
internal sealed class HtmlEditorPreview(Func<string?, OmniHtmlSanitizerPolicy?, string>? sanitize = null)
{
    private readonly Func<string?, OmniHtmlSanitizerPolicy?, string> _sanitize = sanitize ?? OmniHtmlSanitizer.Sanitize;
    private string? _value;
    private OmniHtmlSanitizerPolicy? _policy;
    private string _markup = string.Empty;
    private bool _computed;

    /// <summary>The sanitised markup of <paramref name="value"/>, computed again only when it or <paramref name="policy"/> changed.</summary>
    internal string Of(string? value, OmniHtmlSanitizerPolicy? policy, bool isSanitized)
    {
        if (isSanitized)
        {
            return value ?? string.Empty;
        }

        if (!_computed || !ReferenceEquals(policy, _policy) || !string.Equals(value, _value, StringComparison.Ordinal))
        {
            _value = value;
            _policy = policy;
            _computed = true;
            _markup = _sanitize(value, policy);
        }

        return _markup;
    }
}
