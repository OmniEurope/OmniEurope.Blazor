using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniLink"/> without an address is an action button, and with
/// <see cref="OmniLink.StopPropagation"/> its click and key press never reach a data grid row.
/// </summary>
public sealed class LinkActionAndGridTests : OmniBunitContext
{
    private sealed record Row(int Id, string Name);

    private IRenderedComponent<OmniDataGrid<Row>> RenderGridWithLink(bool stop, List<string> calls, string? href = "doc")
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "act");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Action");
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Template), (RenderFragment<Row>)(row => inner =>
            {
                inner.OpenComponent<OmniLink>(0);
                inner.AddComponentParameter(1, nameof(OmniLink.Href), href);
                inner.AddComponentParameter(2, nameof(OmniLink.StopPropagation), stop);
                inner.AddComponentParameter(3, nameof(OmniLink.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => calls.Add("link")));
                inner.AddComponentParameter(4, nameof(OmniLink.ChildContent), (RenderFragment)(c => c.AddContent(0, row.Name)));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(g => g.Items, [new Row(1, "Un")])
            .Add(g => g.Columns, columns)
            .Add(g => g.KeyOf, row => row.Id)
            .Add(g => g.OnRowClick, _ => calls.Add("row")));
    }

    [Theory]
    [InlineData("doc", "a")]
    [InlineData(null, "button")]
    public void StopPropagation_ClickOnTheLinkOrAction_NeverRaisesOnRowClick(string? href, string tag)
    {
        var calls = new List<string>();
        var grid = RenderGridWithLink(stop: true, calls, href);

        grid.Find("tbody " + tag + ".omni-link").Click();

        Assert.Equal(["link"], calls);
    }

    [Fact]
    public void StopPropagation_RegistersTheStopForTheKeyPress_SoTheRowNeverSeesIt()
    {
        var calls = new List<string>();
        var grid = RenderGridWithLink(stop: true, calls);
        var link = grid.Find("tbody a.omni-link");

        Assert.True(link.HasAttribute("blazor:onclick:stopPropagation"));
        Assert.True(link.HasAttribute("blazor:onkeydown:stopPropagation"));
        // bUnit cannot dispatch a key event to an element without a key handler; the browser honours the
        // registered stop (the link itself has no key handler), and Enter on a link becomes a click, covered above.
        Assert.Empty(calls);
    }

    [Fact]
    public void WithoutStopPropagation_TheClickStillReachesTheRow_AndNoStopAttributeIsRendered()
    {
        var calls = new List<string>();
        var grid = RenderGridWithLink(stop: false, calls);
        var link = grid.Find("tbody a.omni-link");

        Assert.False(link.HasAttribute("blazor:onclick:stopPropagation"));
        Assert.False(link.HasAttribute("blazor:onkeydown:stopPropagation"));
        link.Click();

        Assert.Equal(["link", "row"], calls);
    }

    [Fact]
    public void WithoutHref_RendersAButtonOfTheLinkLook_AndOnlyRunsOnClick()
    {
        var clicks = 0;
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.OnClick, () => clicks++)
            .Add(component => component.Label, "Ouvrir")
            .AddChildContent("Ouvrir"));

        Assert.Empty(link.FindAll("a"));
        var button = link.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Contains("omni-link", button.ClassList);
        Assert.Equal("Ouvrir", button.GetAttribute("aria-label"));
        Assert.False(button.HasAttribute("href"));

        button.Click();

        Assert.Equal(1, clicks);
    }
}
