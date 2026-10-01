using System.Globalization;
using System.Text;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The name of an exported file: what it holds, then when it was made, readable by a person
/// (<c>aetheus-logs-2026-10-01-0840.md</c>). Shared by the export bar of the grid and the Markdown
/// export button.
/// </summary>
internal static class ExportFileName
{
    /// <summary>
    /// <paramref name="name"/> as given (<c>export</c> when blank), the generation time as
    /// <c>yyyy-MM-dd-HHmm</c> in UTC, then <paramref name="extension"/>.
    /// </summary>
    internal static string Stamp(string? name, DateTimeOffset generatedAt, string extension) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{(string.IsNullOrWhiteSpace(name) ? "export" : name.Trim())}-{generatedAt.UtcDateTime:yyyy-MM-dd-HHmm}.{extension}");

    /// <summary>Lowercase ASCII letters and digits, any other run of characters turned into one hyphen.</summary>
    internal static string Slug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var pendingHyphen = false;
        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(character);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(lower);
                pendingHyphen = false;
            }
            else
            {
                pendingHyphen = true;
            }
        }

        return builder.ToString();
    }
}
