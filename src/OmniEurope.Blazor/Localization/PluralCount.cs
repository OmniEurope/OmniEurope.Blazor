namespace OmniEurope.Blazor.Localization;

/// <summary>
/// A whole number passed to a package text with the format it is written in: the plural block counts
/// <see cref="Value"/>, the text shows it in <see cref="Format"/> (grouped thousands, say).
/// </summary>
internal readonly record struct PluralCount(long Value, string Format) : IFormattable
{
    /// <summary>The number in <paramref name="format"/>, or in <see cref="Format"/> when the text gives none.</summary>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(string.IsNullOrEmpty(format) ? Format : format, formatProvider);

    /// <summary>The number in <see cref="Format"/>, in the current culture.</summary>
    public override string ToString() => ToString(null, null);
}
