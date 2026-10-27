using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Every tone with each ink of the solid badge.</summary>
public partial class BadgeInkExample
{
    private static readonly OmniTone[] Tones = Enum.GetValues<OmniTone>();

    private static readonly OmniBadgeInk[] Inks = [OmniBadgeInk.White, OmniBadgeInk.WhiteOutlined, OmniBadgeInk.Black];
}
