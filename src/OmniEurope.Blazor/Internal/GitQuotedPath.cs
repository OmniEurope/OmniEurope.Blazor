using System.Text;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The paths Git writes between double quotes in a diff header (<c>core.quotePath</c>, on by default):
/// a name with a byte past ASCII, a control character, a quote or a backslash comes as
/// <c>"a/r\303\251union.md"</c>, a C string whose octal escapes are the bytes of its UTF-8.
/// </summary>
internal static class GitQuotedPath
{
    /// <summary>The path a token stands for: a quoted token decoded, any other token as it is.</summary>
    internal static string Unquote(string token)
    {
        if (token.Length < 2 || token[0] != '"' || token[^1] != '"')
        {
            return token;
        }

        var bytes = new List<byte>(token.Length);
        var end = token.Length - 1;
        for (var index = 1; index < end; index++)
        {
            index = ReadCharacter(token, index, end, bytes);
        }

        return Encoding.UTF8.GetString([.. bytes]);
    }

    // Appends what the character at index stands for and returns the index of its last character: a
    // surrogate pair is read whole, a backslash with what it escapes, a backslash closing the token as itself.
    private static int ReadCharacter(string token, int index, int end, List<byte> bytes)
    {
        var character = token[index];
        if (char.IsHighSurrogate(character) && char.IsLowSurrogate(token[index + 1]))
        {
            AppendUtf8(bytes, token.AsSpan(index, 2));
            return index + 1;
        }

        if (character != '\\' || index == end - 1)
        {
            AppendUtf8(bytes, [character]);
            return index;
        }

        return ReadEscape(token, index + 1, end, bytes);
    }

    // The escape starting at index: three octal digits are one byte, a letter its control character.
    private static int ReadEscape(string token, int index, int end, List<byte> bytes)
    {
        if (index + 2 < end && IsOctal(token[index]) && IsOctal(token[index + 1]) && IsOctal(token[index + 2]))
        {
            bytes.Add((byte)(((token[index] - '0') * 64) + ((token[index + 1] - '0') * 8) + (token[index + 2] - '0')));
            return index + 2;
        }

        AppendUtf8(bytes, [Unescaped(token[index])]);
        return index;
    }

    private static char Unescaped(char escaped) => escaped switch
    {
        'a' => '\a',
        'b' => '\b',
        't' => '\t',
        'n' => '\n',
        'v' => '\v',
        'f' => '\f',
        'r' => '\r',
        _ => escaped,
    };

    /// <summary>
    /// The two sides of <c>diff --git</c> when one of them is quoted; false when neither is, the caller
    /// then splitting them on the last <c> b/</c>.
    /// </summary>
    internal static bool TrySplit(string paths, out string oldPath, out string newPath)
    {
        if (paths.StartsWith('"'))
        {
            var end = ClosingQuote(paths);
            if (end > 0)
            {
                oldPath = Unquote(paths[..(end + 1)]);
                newPath = Unquote(paths[(end + 1)..].Trim());
                return newPath.Length > 0;
            }
        }
        else if (paths.EndsWith('"'))
        {
            var start = paths.LastIndexOf(" \"", StringComparison.Ordinal);
            if (start > 0)
            {
                oldPath = paths[..start];
                newPath = Unquote(paths[(start + 1)..]);
                return true;
            }
        }

        oldPath = newPath = string.Empty;
        return false;
    }

    // The quote that closes the token opened at 0, past escaped characters; -1 when it never closes.
    private static int ClosingQuote(string paths)
    {
        for (var index = 1; index < paths.Length; index++)
        {
            if (paths[index] == '\\')
            {
                index++;
            }
            else if (paths[index] == '"')
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsOctal(char character) => character is >= '0' and <= '7';

    private static void AppendUtf8(List<byte> bytes, ReadOnlySpan<char> characters)
    {
        Span<byte> buffer = stackalloc byte[4];
        var written = Encoding.UTF8.GetBytes(characters, buffer);
        for (var index = 0; index < written; index++)
        {
            bytes.Add(buffer[index]);
        }
    }
}
