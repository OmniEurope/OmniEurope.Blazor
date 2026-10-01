namespace OmniEurope.Blazor.Components;

/// <summary>A language offered by <see cref="OmniAppMenu"/>.</summary>
/// <param name="Code">The culture code the host applies (<c>fr</c>, <c>en-GB</c>...).</param>
/// <param name="Name">The name shown, in that language (English, Deutsch...).</param>
/// <param name="FlagSource">The image of its flag, shown when <see cref="OmniAppMenu.ShowFlags"/> is set; null for none.</param>
public sealed record OmniAppMenuLanguage(string Code, string Name, string? FlagSource = null);
