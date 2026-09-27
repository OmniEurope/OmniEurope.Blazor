namespace OmniEurope.Blazor.Components;

/// <summary>
/// The characters <see cref="OmniHtmlEditorAction.InsertSpecialCharacter"/> offers, by category. Each is
/// named in the resources under <c>HtmlEditorChar</c> followed by its code point (<c>HtmlEditorChar00A9</c>),
/// and a category under <c>HtmlEditorCharCategory</c> followed by its name.
/// </summary>
internal static class HtmlEditorCharacters
{
    internal const string DefaultCategory = "Common";

    internal static IReadOnlyList<string> Categories { get; } = ["Common", "Currency", "Arrows", "Math", "Greek", "Legal"];

    internal static IReadOnlyList<(string Category, int CodePoint)> All { get; } =
    [
        .. In("Common", 0x00A9, 0x00AE, 0x2122, 0x00A7, 0x00B6, 0x2020, 0x2021, 0x2022, 0x2026, 0x2013, 0x2014,
            0x00AB, 0x00BB, 0x2039, 0x203A, 0x2018, 0x2019, 0x201C, 0x201D, 0x00B0, 0x00B1, 0x00D7, 0x00F7, 0x00A0),
        .. In("Currency", 0x20AC, 0x00A3, 0x00A5, 0x00A2, 0x20BD, 0x20B9, 0x20A9, 0x20AA, 0x20B1, 0x20BA),
        .. In("Arrows", 0x2190, 0x2192, 0x2191, 0x2193, 0x2194, 0x2195, 0x21D0, 0x21D2, 0x21D1, 0x21D3, 0x21B5, 0x21B6),
        .. In("Math", 0x2200, 0x2203, 0x2205, 0x2208, 0x2209, 0x2282, 0x2283, 0x222A, 0x2229, 0x2227, 0x2228, 0x00AC,
            0x2248, 0x2260, 0x2264, 0x2265, 0x221E, 0x221A, 0x2211, 0x220F, 0x2202, 0x222B),
        .. In("Greek", 0x03B1, 0x03B2, 0x03B3, 0x03B4, 0x03B5, 0x03B6, 0x03B7, 0x03B8, 0x03B9, 0x03BA, 0x03BB, 0x03BC,
            0x03BD, 0x03BE, 0x03C0, 0x03C1, 0x03C3, 0x03C4, 0x03C5, 0x03C6, 0x03C7, 0x03C8, 0x03C9),
        .. In("Legal", 0x00A7, 0x00B6, 0x2020, 0x2021, 0x00A9, 0x00AE, 0x2122, 0x2116)
    ];

    internal static string NameKey(int codePoint) => $"HtmlEditorChar{codePoint:X4}";

    internal static string Text(int codePoint) => char.ConvertFromUtf32(codePoint);

    private static IEnumerable<(string, int)> In(string category, params int[] codePoints) =>
        codePoints.Select(codePoint => (category, codePoint));
}
