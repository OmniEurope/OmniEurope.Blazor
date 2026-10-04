namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The address shape <see cref="Components.OmniEmailValidator"/> accepts: a practical subset of the
/// addresses people type, not the whole RFC 5322 grammar (no quoted local part, no comment, no IP
/// literal). Letters of any script are allowed, for internationalized addresses.
/// </summary>
internal static class EmailAddressRule
{
    private const string LocalSymbols = "!#$%&'*+/=?^_`{|}~-.";

    /// <summary>
    /// True for <c>local@domain</c>: one <c>@</c>; a local part of 1 to 64 characters made of letters,
    /// digits and <c>!#$%&amp;'*+/=?^_`{|}~-.</c>, without a dot at either end or two dots in a row; a
    /// domain of at most 253 characters with at least two labels, each label 1 to 63 letters, digits
    /// or hyphens, not starting or ending with a hyphen, and a last label that is not all digits.
    /// </summary>
    internal static bool IsValid(string text)
    {
        var at = text.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != text.LastIndexOf('@') || at == text.Length - 1)
        {
            return false;
        }

        return IsLocalPart(text.AsSpan(0, at)) && IsDomain(text.AsSpan(at + 1));
    }

    private static bool IsLocalPart(ReadOnlySpan<char> local)
    {
        if (local.Length > 64 || local[0] == '.' || local[^1] == '.' || local.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var character in local)
        {
            if (!char.IsLetterOrDigit(character) && !LocalSymbols.Contains(character, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDomain(ReadOnlySpan<char> domain)
    {
        if (domain.Length > 253)
        {
            return false;
        }

        var labels = 0;
        var lastAllDigits = false;
        foreach (var range in domain.Split('.'))
        {
            var label = domain[range];
            if (label.Length is 0 or > 63 || label[0] == '-' || label[^1] == '-')
            {
                return false;
            }

            var allDigits = true;
            foreach (var character in label)
            {
                if (!char.IsLetterOrDigit(character) && character != '-')
                {
                    return false;
                }

                allDigits &= char.IsAsciiDigit(character);
            }

            labels++;
            lastAllDigits = allDigits;
        }

        return labels >= 2 && !lastAllDigits;
    }
}
