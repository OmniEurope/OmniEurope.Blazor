using System.Text.Json;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Every setting of the user's appearance at once, as <see cref="OmniAppearanceState"/> holds it. The
/// defaults are the parameter defaults of the components that show and apply it, so an application that
/// moves to the shared state sees no change until the user picks something.
/// </summary>
internal sealed record AppearanceLook(
    OmniAppearance Appearance,
    OmniThemePreset? Preset,
    OmniThemePalette? Palette,
    OmniThemeFont? Font,
    OmniDensity Density,
    bool BackdropMotion,
    int TextSizeLevel,
    int ControlSizeLevel,
    bool FullWidth)
{
    internal const int MinimumLevel = 1;
    internal const int MaximumLevel = 10;
    internal const int DefaultLevel = 5;

    /// <summary>The version written with each preference, for a later shape to recognise this one.</summary>
    internal const int Version = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static AppearanceLook Default { get; } = new(
        OmniAppearance.System, null, null, null, OmniDensity.Comfortable, true, DefaultLevel, DefaultLevel, false);

    /// <summary>The preference written for this look: names for the catalogue entries and the enumerations.</summary>
    internal AppearancePreference ToPreference() => new(
        Version, Appearance.ToString(), Preset?.Name, Palette?.Name, Font?.Name, Density.ToString(),
        BackdropMotion, TextSizeLevel, ControlSizeLevel, FullWidth);

    /// <summary>The look a stored JSON value reads as; a value that does not parse reads as the defaults.</summary>
    internal static AppearanceLook Parse(string? json)
    {
        if (json is null)
        {
            return Default;
        }

        try
        {
            return From(JsonSerializer.Deserialize<AppearancePreference>(json, JsonOptions));
        }
        catch (JsonException)
        {
            return Default;
        }
    }

    /// <summary>
    /// The look a preference reads as, one setting at a time: a name the catalogue no longer has, an
    /// unknown enumeration name or a level out of 1 to 10 keeps that setting's default.
    /// </summary>
    internal static AppearanceLook From(AppearancePreference? preference)
    {
        if (preference is null)
        {
            return Default;
        }

        return new(
            Named<OmniAppearance>(preference.Appearance) ?? Default.Appearance,
            OmniThemePresets.All.FirstOrDefault(theme => theme.Name == preference.Preset),
            OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == preference.Palette),
            OmniThemeFonts.All.FirstOrDefault(font => font.Name == preference.Font),
            Named<OmniDensity>(preference.Density) ?? Default.Density,
            preference.BackdropMotion ?? Default.BackdropMotion,
            Level(preference.TextSize),
            Level(preference.ControlSize),
            preference.FullWidth ?? Default.FullWidth);
    }

    internal static int Clamp(int level) => Math.Clamp(level, MinimumLevel, MaximumLevel);

    private static int Level(int? level) => level is >= MinimumLevel and <= MaximumLevel ? level.Value : DefaultLevel;

    /// <summary>The member an enumeration name stands for, by its name only: a number is not a name.</summary>
    private static T? Named<T>(string? name)
        where T : struct, Enum
    {
        foreach (var value in Enum.GetValues<T>())
        {
            if (string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }
}

/// <summary>
/// The JSON shape of the appearance preference (<see cref="OmniAppearanceState.PreferenceKey"/>), camel
/// case: the enumerations and the catalogue entries by name, the levels as numbers. Every member is
/// optional, so a value written by an older or newer version keeps the defaults of what it lacks.
/// </summary>
internal sealed record AppearancePreference(
    int Version,
    string? Appearance,
    string? Preset,
    string? Palette,
    string? Font,
    string? Density,
    bool? BackdropMotion,
    int? TextSize,
    int? ControlSize,
    bool? FullWidth);
