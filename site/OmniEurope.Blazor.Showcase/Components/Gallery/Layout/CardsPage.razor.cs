using OmniEurope.Blazor.Showcase.Examples.Layout;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Cards page: one card whose areas are settings, then the arrangements, the icon-disc cards and the densities.</summary>
public partial class CardsPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private CardHeaderKind SelectedHeader { get; set; } = CardHeaderKind.TitleAndBadge;

    private CardFooterKind SelectedFooter { get; set; } = CardFooterKind.Actions;

    private bool LongBody { get; set; }

    private bool Borderless { get; set; }

    private IReadOnlyList<OmniOption<CardHeaderKind>> HeaderOptions =>
    [
        new(CardHeaderKind.None, Text["CardsHeaderNone"]),
        new(CardHeaderKind.Title, Text["CardsHeaderTitle"]),
        new(CardHeaderKind.TitleAndBadge, Text["CardsHeaderTitleAndBadge"])
    ];

    private IReadOnlyList<OmniOption<CardFooterKind>> FooterOptions =>
    [
        new(CardFooterKind.None, Text["CardsFooterNone"]),
        new(CardFooterKind.Text, Text["CardsFooterText"]),
        new(CardFooterKind.Actions, Text["CardsFooterActions"])
    ];
}
