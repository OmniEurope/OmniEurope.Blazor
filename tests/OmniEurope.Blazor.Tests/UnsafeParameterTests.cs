using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Parameters that reach the markup without the checks their siblings go through: a details link
/// given straight to a notification, an href slipped into a link's attributes, and the attributes
/// of the components that override the lifecycle step holding the CSP guard.
/// </summary>
public sealed class UnsafeParameterTests : OmniBunitContext
{
    private static readonly string LongMessage = new('x', 2001);

    [Fact]
    public void Notification_DetailsHrefWithAScriptScheme_IsRefusedLikeTheServiceRefusesIt()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, LongMessage)
            .Add(component => component.DetailsHref, "javascript:alert(1)")));

        Assert.Contains("DetailsHref", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Notification_SafeDetailsHref_IsRendered()
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, LongMessage)
            .Add(component => component.DetailsHref, "/journal/42"));

        Assert.Equal("/journal/42", notification.Find(".omni-notification__details").GetAttribute("href"));
    }

    [Fact]
    public void Link_HrefPassedThroughItsAttributes_DoesNotReplaceTheValidatedHref()
    {
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "/projets")
            .AddChildContent("Projets")
            // A loose "href" would bind to Href itself; only a dictionary given as the attributes carries one.
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object>
            {
                ["href"] = "javascript:alert(1)",
                ["data-kind"] = "primary",
            }));

        var anchor = link.Find("a");
        Assert.Equal("/projets", anchor.GetAttribute("href"));
        Assert.Equal("primary", anchor.GetAttribute("data-kind"));
    }

    [Fact]
    public void TabsItem_InlineStyleOrHandlerAttribute_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniTabsItem>(parameters => parameters
            .Add(component => component.Title, "Un")
            .AddUnmatched("style", "color: red")));
        Assert.Throws<InvalidOperationException>(() => Render<OmniTabsItem>(parameters => parameters
            .Add(component => component.Title, "Un")
            .AddUnmatched("onclick", "alert(1)")));
    }

    [Fact]
    public void TreeItem_InlineStyleOrHandlerAttribute_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "root")
            .Add(component => component.Text, "Racine")
            .AddUnmatched("style", "color: red")));
        Assert.Throws<InvalidOperationException>(() => Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "root")
            .Add(component => component.Text, "Racine")
            .AddUnmatched("onclick", "alert(1)")));
    }
}
