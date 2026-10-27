using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Navigation tiles page: three tiles shaped by their own settings, one tile per tone, and a grid with a double-width tile.</summary>
public partial class NavTilesPage
{
    private static readonly OmniTone[] Tones =
        [OmniTone.Neutral, OmniTone.Accent, OmniTone.Info, OmniTone.Success, OmniTone.Warning, OmniTone.Danger];

    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private OmniTone Tone { get; set; } = OmniTone.Accent;

    private bool ShowDescription { get; set; } = true;

    private IReadOnlyList<OmniOption<OmniTone>> ToneOptions =>
        [.. Tones.Select(tone => new OmniOption<OmniTone>(tone, Text[$"NavTilesTone{tone}"]))];
}
