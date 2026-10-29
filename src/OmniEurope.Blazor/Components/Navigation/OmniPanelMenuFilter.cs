using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The text a panel menu is narrowed by. A new instance stands for a new text, which is how the entries
/// notice the filter moved and drop what they folded or unfolded while the previous one was applied.
/// </summary>
internal sealed class OmniPanelMenuFilter(string? text)
{
    /// <summary>The filter that is not filtering: every entry shown as its own state says.</summary>
    public static OmniPanelMenuFilter None { get; } = new(null);

    public string? Text { get; } = string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    public bool Active => Text is not null;

    /// <summary>Same rule as every other search of the package: the text is contained, ignoring case and accents.</summary>
    public bool Matches(string? entryText) => Active && OmniTextMatch.Contains(entryText ?? string.Empty, Text);
}
