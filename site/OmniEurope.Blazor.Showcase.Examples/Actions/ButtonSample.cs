using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>One button variant with the action it typically carries: its name, its label and its icon.</summary>
/// <param name="Variant">The variant of the button.</param>
/// <param name="Name">The name of the variant, as a row header.</param>
/// <param name="Action">The label of the action the variant carries.</param>
/// <param name="Icon">The icon shown before the label.</param>
public sealed record ButtonSample(OmniButtonVariant Variant, string Name, string Action, OmniIconName Icon)
{
    /// <summary>Every variant of <see cref="OmniButtonVariant"/>, each with an action that suits its role.</summary>
    public static IReadOnlyList<ButtonSample> All { get; } =
    [
        new(OmniButtonVariant.Primary, "Primary", "Save", OmniIconName.Save),
        new(OmniButtonVariant.Secondary, "Secondary", "Edit", OmniIconName.Edit),
        new(OmniButtonVariant.Ghost, "Ghost", "Refresh", OmniIconName.Refresh),
        new(OmniButtonVariant.Success, "Success", "Confirm", OmniIconName.Check),
        new(OmniButtonVariant.Warning, "Warning", "Suspend", OmniIconName.Warning),
        new(OmniButtonVariant.Info, "Info", "Details", OmniIconName.Info),
        new(OmniButtonVariant.Danger, "Danger", "Delete", OmniIconName.Delete)
    ];

    /// <summary>The sample of a variant, the primary one for a value the list does not hold.</summary>
    public static ButtonSample Of(OmniButtonVariant variant) =>
        All.FirstOrDefault(sample => sample.Variant == variant) ?? All[0];
}
