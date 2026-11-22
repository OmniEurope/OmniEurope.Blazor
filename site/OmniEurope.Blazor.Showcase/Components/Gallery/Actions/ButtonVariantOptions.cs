using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Actions;

/// <summary>The button variant choices the Buttons and Split buttons pages offer in their settings.</summary>
internal static class ButtonVariantOptions
{
    /// <summary>Every button variant, with its translated name.</summary>
    /// <param name="text">The strings of the actions pages.</param>
    /// <returns>The options, in display order.</returns>
    public static IReadOnlyList<OmniOption<OmniButtonVariant>> For(IStringLocalizer<ActionsStrings> text) =>
    [
        new(OmniButtonVariant.Primary, text["ButtonsVariantPrimary"]),
        new(OmniButtonVariant.Secondary, text["ButtonsVariantSecondary"]),
        new(OmniButtonVariant.Ghost, text["ButtonsVariantGhost"]),
        new(OmniButtonVariant.Success, text["ButtonsVariantSuccess"]),
        new(OmniButtonVariant.Warning, text["ButtonsVariantWarning"]),
        new(OmniButtonVariant.Info, text["ButtonsVariantInfo"]),
        new(OmniButtonVariant.Danger, text["ButtonsVariantDanger"]),
        new(OmniButtonVariant.GhostDanger, text["ButtonsVariantGhostDanger"])
    ];
}
