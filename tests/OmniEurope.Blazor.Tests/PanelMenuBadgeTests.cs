using Bunit;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="Components.OmniPanelMenuItem.Badge"/> renders after the text, in its own end slot, on
/// every kind of entry, and an entry without one renders no empty slot.
/// </summary>
public sealed class PanelMenuBadgeTests : OmniBunitContext
{
    [Theory]
    [InlineData("#badge-group .omni-panel-menu__summary .omni-panel-menu__link", "3")]
    [InlineData("#badge-link", "12")]
    [InlineData("#badge-action", "1")]
    public void TheBadgeFollowsTheTextOfItsEntry(string entry, string count)
    {
        var host = Render<PanelMenuBadgeTestHost>();

        var element = host.Find(entry);
        var badge = element.QuerySelector(":scope > .omni-panel-menu__badge");
        Assert.NotNull(badge);
        Assert.Equal(count, badge!.TextContent.Trim());
        Assert.Same(element.QuerySelector(":scope > .omni-panel-menu__text"), badge.PreviousElementSibling);
    }

    [Fact]
    public void AnEntryWithoutBadgeRendersNoBadgeSlot()
    {
        var host = Render<PanelMenuBadgeTestHost>();

        Assert.Empty(host.Find("#plain-label").QuerySelectorAll(".omni-panel-menu__badge"));
    }
}
