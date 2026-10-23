using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The mode picker, one source for the application menu and the appearance window: a radio group of
/// three icon-only buttons joined (light, dark, system), the one in force <see cref="OmniButtonVariant.Primary"/>
/// and read by <c>aria-checked</c>, a single tab stop on it, and the arrows of <c>data-omni-roving</c> moving
/// and picking the mode. A theme drawn in one mode only disables the three and says why on the group.
/// </summary>
internal static class AppearanceModeGroup
{
    /// <summary>The mode buttons in the order they are drawn.</summary>
    internal static readonly OmniAppearance[] Modes = [OmniAppearance.Light, OmniAppearance.Dark, OmniAppearance.System];

    internal static OmniIconName Icon(OmniAppearance mode) => mode switch
    {
        OmniAppearance.Light => OmniIconName.ThemeLight,
        OmniAppearance.Dark => OmniIconName.ThemeDark,
        _ => OmniIconName.ThemeSystem
    };

    /// <summary>The localized name of a mode.</summary>
    internal static string Name(OmniAppearance mode, Func<string, string> localize) => localize(mode switch
    {
        OmniAppearance.Light => "AppearanceLight",
        OmniAppearance.Dark => "AppearanceDark",
        _ => "AppearanceSystem"
    });

    /// <summary>The group, ready to be rendered inside a row.</summary>
    /// <param name="receiver">The component that renders it: the click handlers re-render it.</param>
    /// <param name="shown">The mode in force (the fixed mode of a dark-only theme).</param>
    /// <param name="fixedMode">Whether a theme drawn in one mode only fixes the mode: the three buttons are disabled.</param>
    /// <param name="localize">The localizer of the component.</param>
    /// <param name="pick">Raised with the mode picked.</param>
    internal static RenderFragment Render(object receiver, OmniAppearance shown, bool fixedMode, Func<string, string> localize, Func<OmniAppearance, Task> pick) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-appearance-window__modes");
        builder.AddAttribute(2, "role", "radiogroup");
        builder.AddAttribute(3, "data-omni-roving", true);
        builder.AddAttribute(4, "aria-label", localize("SettingsMode"));
        builder.AddAttribute(5, "aria-disabled", fixedMode ? "true" : null);
        builder.AddAttribute(6, "title", fixedMode ? localize("SettingsDarkOnly") : null);
        // A click inside the group is the buttons': it does not reach a row that steps to the next mode.
        builder.AddEventStopPropagationAttribute(7, "onclick", true);
        foreach (var mode in Modes)
        {
            AddButton(builder, receiver, mode, shown, fixedMode, Name(mode, localize), pick);
        }

        builder.CloseElement();
    };

    private static void AddButton(RenderTreeBuilder builder, object receiver, OmniAppearance mode, OmniAppearance shown, bool fixedMode, string name, Func<OmniAppearance, Task> pick)
    {
        var selected = mode == shown;
        builder.OpenComponent<OmniButton>(8);
        builder.SetKey(mode);
        builder.AddComponentParameter(9, nameof(OmniButton.Variant), selected ? OmniButtonVariant.Primary : OmniButtonVariant.Secondary);
        builder.AddComponentParameter(10, nameof(OmniButton.Class), "omni-appearance-window__mode");
        builder.AddComponentParameter(11, nameof(OmniButton.Disabled), fixedMode);
        builder.AddComponentParameter(12, nameof(OmniButton.Label), name);
        builder.AddComponentParameter(13, "title", name);
        builder.AddComponentParameter(14, "role", "radio");
        builder.AddComponentParameter(15, "aria-checked", selected ? "true" : "false");
        builder.AddComponentParameter(16, "tabindex", selected ? "0" : "-1");
        builder.AddComponentParameter(17, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(receiver, () => pick(mode)));
        builder.AddComponentParameter(18, nameof(OmniButton.Icon), (RenderFragment)(icon =>
        {
            icon.OpenComponent<OmniIcon>(0);
            icon.AddComponentParameter(1, nameof(OmniIcon.Name), Icon(mode));
            icon.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
