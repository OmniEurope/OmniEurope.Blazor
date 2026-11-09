using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A country shown by its flag and its ISO 3166-1 alpha-2 code. The library hard-codes no image host: the
/// picture comes from <see cref="ImageUrlTemplate"/>, which the host sets on the component or once for
/// every flag through a preset (<c>AddOmniEuropePreset</c>). Without a template the flag is the code in a
/// small badge.
/// </summary>
/// <remarks>
/// The component is one image for assistive technologies, named by the country in the current UI culture
/// ("France" in French) or by the code when it names no known
/// country; the same text is its <c>title</c>. The flag picture is decorative (<c>alt=""</c>, hidden from
/// assistive technologies), sent without a referrer and loaded lazily. A host under a strict CSP
/// allows the image host of its template in <c>img-src</c>.
/// </remarks>
public partial class OmniCountryFlag
{
    /// <summary>
    /// The country, an ISO 3166-1 alpha-2 code (<c>fr</c> and <c> FR </c> are both <c>FR</c>). Blank draws
    /// nothing. A text that is not two letters is shown as typed, trimmed, with no picture.
    /// </summary>
    [Parameter]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Whether the code is written beside the picture; true by default. A flag without a picture (no template,
    /// or an invalid code) always shows its code, which is then all it has.
    /// </summary>
    [Parameter]
    public bool ShowCode { get; set; } = true;

    /// <summary>
    /// The address of the flag picture, in which <c>{code}</c> is replaced by the lower-case code and
    /// <c>{CODE}</c> by the upper-case one (for example <c>https://flagcdn.com/{code}.svg</c>). Null or blank,
    /// the default, draws no picture. An address that is not <c>https://</c>, <c>http://</c> or rooted at
    /// <c>/</c> once filled is ignored.
    /// </summary>
    [Parameter]
    public string? ImageUrlTemplate { get; set; }

    private string? Code => string.IsNullOrWhiteSpace(CountryCode) ? null : CountryCode.Trim().ToUpperInvariant();

    // Two letters the runtime knows as a region: "ZZ" and "F1" are codes, not countries.
    private RegionInfo? Region
    {
        get
        {
            if (Code is not { Length: 2 } code || !code.All(letter => letter is >= 'A' and <= 'Z'))
            {
                return null;
            }

            try
            {
                return new RegionInfo(code);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    private bool IsValidCode => Region is not null;

    private string? ImageUrl
    {
        get
        {
            if (!IsValidCode || string.IsNullOrWhiteSpace(ImageUrlTemplate))
            {
                return null;
            }

            var url = ImageUrlTemplate.Replace("{code}", Code!.ToLowerInvariant(), StringComparison.Ordinal)
                .Replace("{CODE}", Code, StringComparison.Ordinal);
            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal)
                ? url
                : null;
        }
    }

    private string AccessibleName => CountryName ?? Code!;

    // RegionInfo.DisplayName is the region's own language on this runtime, not the page's. The display
    // name of the culture "{page language}-{CODE}" is localized in the page language (for example
    // "(Germany)"): the country is what its parentheses hold.
    private string? CountryName
    {
        get
        {
            if (Region is not { } region)
            {
                return null;
            }

            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            try
            {
                var display = CultureInfo.GetCultureInfo($"{(language.Length == 2 && language != "iv" ? language : "en")}-{region.TwoLetterISORegionName}").DisplayName;
                var open = display.LastIndexOf('(');
                var close = display.LastIndexOf(')');
                if (open >= 0 && close > open + 1)
                {
                    return display[(open + 1)..close].Trim();
                }
            }
            catch (CultureNotFoundException)
            {
                // No such culture on this runtime: the region's own name below.
            }

            return string.IsNullOrWhiteSpace(region.DisplayName) ? null : region.DisplayName;
        }
    }
}
