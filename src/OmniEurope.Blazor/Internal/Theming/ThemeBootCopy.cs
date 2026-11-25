
namespace OmniEurope.Blazor.Components;

/// <summary>
/// The boot copy of what the outermost <see cref="OmniThemeScope"/> painted, kept beside the appearance preference
/// in the browser cache of <see cref="OmniPreferences"/> (the <c>Boot</c> member of its entry, local to this browser)
/// for <c>omni-boot.js</c> to lay on the document before Blazor starts. Its shape is the snapshot of
/// <see cref="OmniThemeScope.SnapshotKey"/> (version, attributes, both halves of the tokens, the two root levels),
/// plus <see cref="Value"/>, the preference it was drawn for: a copy drawn for another look than the entry holds is
/// left out by the boot script.
/// </summary>
internal sealed record ThemeBootCopy(
    int Version,
    string Value,
    string Appearance,
    string Density,
    bool BackdropMotion,
    IReadOnlyDictionary<string, string>? Light,
    IReadOnlyDictionary<string, string>? Dark,
    int TextSize,
    int ControlSize)
{
    /// <summary>The JSON the boot script reads (web defaults: camel case members, token names as they are).</summary>
    internal string ToJson() => PreferenceJson.Serialize(this);
}
