using System.Text;
using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The <c>{{name}}</c> tokens of a mail template: finding them, and replacing them in plain text. Each
/// name without a value is recorded once, in the order it first appears.
/// </summary>
internal sealed partial class MailTemplateText(IReadOnlyDictionary<string, string?> values)
{
    private readonly List<string> _missing = [];

    /// <summary>The names written in a template but given no value, each once.</summary>
    internal IReadOnlyList<string> Missing => _missing;

    /// <summary>The tokens of the text, in order.</summary>
    internal static MatchCollection Matches(string text) => Token().Matches(text);

    /// <summary>Whether the text holds at least one token.</summary>
    internal static bool HasToken(string text) => Token().IsMatch(text);

    /// <summary>The value of a name, recording it as missing when it has none.</summary>
    internal string ValueOf(string name)
    {
        if (values.TryGetValue(name, out var value) && value is not null)
        {
            return value;
        }

        NoteMissing(name);
        return string.Empty;
    }

    /// <summary>Records a name given no value, once.</summary>
    internal void NoteMissing(string name)
    {
        if (!_missing.Contains(name, StringComparer.Ordinal))
        {
            _missing.Add(name);
        }
    }

    /// <summary>Plain text with each token replaced by its value, as is (the caller encodes for its context).</summary>
    internal string Replace(string text) => Token().Replace(text, match => ValueOf(match.Groups["name"].Value));

    /// <summary>
    /// Splits a text into its literal runs and its tokens' values, so the caller can write the literal
    /// text and each value as separate text nodes.
    /// </summary>
    internal IEnumerable<string> Pieces(string text)
    {
        var position = 0;
        foreach (Match match in Token().Matches(text))
        {
            if (match.Index > position)
            {
                yield return text[position..match.Index];
            }

            yield return ValueOf(match.Groups["name"].Value);
            position = match.Index + match.Length;
        }

        if (position < text.Length)
        {
            yield return text[position..];
        }
    }

    /// <summary>One line: line breaks and runs of spaces become one space.</summary>
    internal static string OneLine(string text)
    {
        var builder = new StringBuilder(text.Length);
        var space = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                space = builder.Length > 0;
                continue;
            }

            if (space)
            {
                builder.Append(' ');
                space = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"\{\{\s*(?<name>[A-Za-z][A-Za-z0-9_.\-]*)\s*\}\}")]
    private static partial Regex Token();
}
