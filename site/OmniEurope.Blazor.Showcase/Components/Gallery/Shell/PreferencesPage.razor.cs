using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Shell;

/// <summary>The Preferences page: a value saved through the preference service the showcase registers, read back from the browser and from the store.</summary>
public partial class PreferencesPage
{
    [Inject]
    private IStringLocalizer<ShellStrings> Text { get; set; } = default!;
}
