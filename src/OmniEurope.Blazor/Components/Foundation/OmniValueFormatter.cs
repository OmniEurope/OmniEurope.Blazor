using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Writes file sizes and durations the same way everywhere: short, in the language and with the decimal
/// separator of a culture. Inject it (<c>AddOmniEuropeBlazor</c> registers it): the unit symbols are
/// package texts (<c>FormatByte</c>, <c>FormatSecond</c> and the others, in the 24 languages) read through
/// the injected <see cref="IStringLocalizer{AppStrings}"/>, so a host's text override
/// (<c>AddOmniEuropeTextOverrides</c>) applies to them like to every other package text, and a culture
/// the package does not ship reads the French symbols.
/// </summary>
/// <param name="localizer">The package text localizer.</param>
public sealed class OmniValueFormatter(IStringLocalizer<AppStrings> localizer)
{
    private const int Base = 1024;

    private static readonly string[] SizeKeys = ["FormatByte", "FormatKilobyte", "FormatMegabyte", "FormatGigabyte", "FormatTerabyte"];

    /// <summary>
    /// A size in bytes as a short text: whole bytes under 1024 (<c>512 B</c>), else the largest unit that
    /// keeps the number at or above 1, with one decimal when it has one. The base is 1024, as in the file
    /// managers of Windows: 1 KB is 1024 bytes, 1 MB is 1024 KB, up to terabytes. A decimal that would round
    /// to 1024 moves to the next unit (<c>1 MB</c>, not <c>1024 KB</c>). The number and the unit are
    /// separated by a space.
    /// </summary>
    /// <param name="bytes">The size, zero or more.</param>
    /// <param name="culture">The language of the unit and the decimal separator; null, the default, is the current UI culture.</param>
    /// <returns>For example <c>512 o</c>, <c>1,5 Ko</c> and <c>3,7 Go</c> in French, <c>512 B</c>, <c>1.5 KB</c> and <c>3.7 GB</c> in English.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/> is negative.</exception>
    public string FileSize(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return CultureText.In(culture, () => FileSize(bytes, culture, key => localizer[key].Value));
    }

    /// <summary>
    /// A duration as a short text without spaces, keeping the two most useful units. Truncated to whole
    /// seconds, then: under a minute, seconds (<c>45s</c>); under an hour, minutes, with the seconds only
    /// under ten minutes and when there are some (<c>9min59s</c>, <c>10min</c>, <c>42min</c>); under a day,
    /// hours and minutes, the minutes dropped when zero (<c>1h44min</c>, <c>2h</c>); a day or more, days and
    /// hours, the hours dropped when zero (<c>1d2h</c>, <c>3d</c>). The symbols follow the language.
    /// </summary>
    /// <param name="value">The duration, zero or more.</param>
    /// <param name="culture">The language of the unit symbols; null, the default, is the current UI culture.</param>
    /// <returns>For example <c>45s</c>, <c>42min</c> and <c>1h44min</c>; in French <c>1j2h</c> for a day and two hours.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public string Duration(TimeSpan value, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return CultureText.In(culture, () => Duration(value, culture, key => localizer[key].Value));
    }

    /// <summary>The size writer, with the unit symbols read through <paramref name="unit"/> (a component's own localizer).</summary>
    internal static string FileSize(long bytes, CultureInfo culture, Func<string, string> unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        if (bytes < Base)
        {
            return $"{bytes.ToString("0", culture)} {unit(SizeKeys[0])}";
        }

        var number = (double)bytes;
        var index = 0;
        while (index < SizeKeys.Length - 1 && (number >= Base || Math.Round(number, 1) >= Base))
        {
            number /= Base;
            index++;
        }

        return $"{number.ToString("0.#", culture)} {unit(SizeKeys[index])}";
    }

    /// <summary>The duration writer, with the unit symbols read through <paramref name="unit"/>.</summary>
    internal static string Duration(TimeSpan value, CultureInfo culture, Func<string, string> unit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
        var total = (long)value.TotalSeconds;
        string Part(long amount, string key) => amount.ToString("0", culture) + unit(key);

        if (total < 60)
        {
            return Part(total, "FormatSecond");
        }

        if (total < 3600)
        {
            var minutes = total / 60;
            var seconds = total % 60;
            return minutes < 10 && seconds > 0
                ? Part(minutes, "FormatMinute") + Part(seconds, "FormatSecond")
                : Part(minutes, "FormatMinute");
        }

        if (total < 86400)
        {
            var minutes = total % 3600 / 60;
            return minutes > 0
                ? Part(total / 3600, "FormatHour") + Part(minutes, "FormatMinute")
                : Part(total / 3600, "FormatHour");
        }

        var hours = total % 86400 / 3600;
        return hours > 0
            ? Part(total / 86400, "FormatDay") + Part(hours, "FormatHour")
            : Part(total / 86400, "FormatDay");
    }
}
