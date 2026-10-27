using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>Account menus with a text trigger, an icon trigger and an avatar trigger with an identity header.</summary>
public partial class ProfileMenuGalleryExample
{
    /// <summary>The make-believe holder of the account, sample data.</summary>
    private const string UserName = "Camille Durand";

    private string? LastChoice { get; set; }

    private void Choose(string item) => LastChoice = item;
}
