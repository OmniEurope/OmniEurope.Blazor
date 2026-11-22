using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The page size and numbered page choices the Data grid and Data list pages offer for their pager.</summary>
internal static class PagerChoices
{
    /// <summary>One option per page size, written as a number.</summary>
    /// <param name="sizes">The page sizes offered.</param>
    /// <returns>The options, in the order of the sizes.</returns>
    public static IReadOnlyList<OmniOption<int>> PageSizes(IEnumerable<int> sizes) =>
        [.. sizes.Select(size => new OmniOption<int>(size, size.ToString(CultureInfo.CurrentCulture)))];

    /// <summary>One option per count of numbered pages, zero written as the given word.</summary>
    /// <param name="counts">The counts offered.</param>
    /// <param name="none">The translated word for no numbered page.</param>
    /// <returns>The options, in the order of the counts.</returns>
    public static IReadOnlyList<OmniOption<int>> NumericPages(IEnumerable<int> counts, string none) =>
    [
        .. counts.Select(count => new OmniOption<int>(
            count,
            count == 0 ? none : count.ToString(CultureInfo.CurrentCulture)))
    ];
}
