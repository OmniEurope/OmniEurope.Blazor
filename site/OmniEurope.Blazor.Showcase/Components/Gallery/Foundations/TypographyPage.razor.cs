using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Foundations;

/// <summary>The Typography page: the settings of a heading and of a paragraph, then every level, element and tone.</summary>
public partial class TypographyPage
{
    private static readonly OmniHeadingLevel[] AllLevels = Enum.GetValues<OmniHeadingLevel>();

    private static readonly OmniTextElement[] AllElements = Enum.GetValues<OmniTextElement>();

    private static readonly OmniTextTone[] AllTones = Enum.GetValues<OmniTextTone>();

    [Inject]
    private IStringLocalizer<FoundationsStrings> Text { get; set; } = default!;

    private string HeadingText { get; set; } = string.Empty;

    private OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H2;

    private OmniTextTone HeadingTone { get; set; } = OmniTextTone.Neutral;

    private OmniTextElement Element { get; set; } = OmniTextElement.Paragraph;

    private OmniTextTone TextTone { get; set; } = OmniTextTone.Neutral;

    private bool Truncate { get; set; }

    // The HTML tag names are technical data, not interface text.
    private IReadOnlyList<OmniOption<OmniHeadingLevel>> LevelOptions =>
        [.. AllLevels.Select(level => new OmniOption<OmniHeadingLevel>(level, $"h{(int)level}"))];

    private IReadOnlyList<OmniOption<OmniTextTone>> ToneOptions =>
        [.. AllTones.Select(tone => new OmniOption<OmniTextTone>(tone, ToneLabel(tone)))];

    private IReadOnlyList<OmniOption<OmniTextElement>> ElementOptions =>
        [.. AllElements.Select(element => new OmniOption<OmniTextElement>(element, ElementLabel(element)))];

    protected override void OnInitialized() => HeadingText = Text["TypographyHeadingDefault"];

    private string ToneLabel(OmniTextTone tone) => tone switch
    {
        OmniTextTone.Muted => Text["TypographyToneMuted"],
        OmniTextTone.Accent => Text["TypographyToneAccent"],
        OmniTextTone.Success => Text["TypographyToneSuccess"],
        OmniTextTone.Danger => Text["TypographyToneDanger"],
        OmniTextTone.Info => Text["TypographyToneInfo"],
        OmniTextTone.Warning => Text["TypographyToneWarning"],
        _ => Text["TypographyToneDefault"]
    };

    private string ElementLabel(OmniTextElement element) => element switch
    {
        OmniTextElement.Paragraph => Text["TypographyElementParagraph"],
        OmniTextElement.Strong => Text["TypographyElementStrong"],
        OmniTextElement.Emphasis => Text["TypographyElementEmphasis"],
        OmniTextElement.Small => Text["TypographyElementSmall"],
        OmniTextElement.Title => Text["TypographyElementTitle"],
        OmniTextElement.Caption => Text["TypographyElementCaption"],
        _ => Text["TypographyElementSpan"]
    };
}
