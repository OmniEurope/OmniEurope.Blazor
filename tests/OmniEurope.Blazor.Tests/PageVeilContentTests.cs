using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The page veil covers the content of the page only (recette R1-29): inside an <see cref="OmniMain"/> it is
/// drawn by that main area over its own frame, never in place, so the header and the sidebar stay visible and
/// no ancestor of the veil (a positioned card, a scrolling box) can capture it.
/// </summary>
public sealed class PageVeilContentTests : OmniBunitContext
{
    private static readonly RenderFragment Mark = builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "app-logo");
        builder.CloseElement();
    };

    /// <summary>A page component that owns the veil's state, so a test changes it without rendering the main area again.</summary>
    private sealed class WaitingPage : ComponentBase
    {
        [Parameter]
        public bool Busy { get; set; }

        [Parameter]
        public RenderFragment? Logo { get; set; }

        [Parameter]
        public OmniPageVeilScope Scope { get; set; }

        /// <summary>Changes the state the page declares its veil with and renders the page alone, as a page does when its own state moves.</summary>
        public void Update(bool? busy = null, RenderFragment? logo = null, bool clearLogo = false, OmniPageVeilScope? scope = null)
        {
            Busy = busy ?? Busy;
            Logo = clearLogo ? null : logo ?? Logo;
            Scope = scope ?? Scope;
            _ = InvokeAsync(StateHasChanged);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "page-card");
            builder.OpenComponent<OmniPageVeil>(2);
            builder.AddComponentParameter(3, nameof(OmniPageVeil.Busy), Busy);
            builder.AddComponentParameter(4, nameof(OmniPageVeil.Logo), Logo);
            builder.AddComponentParameter(5, nameof(OmniPageVeil.Scope), Scope);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    private IRenderedComponent<OmniMain> RenderMain(bool scrollable) =>
        Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, scrollable)
            .AddChildContent<WaitingPage>(page => page.Add(item => item.Busy, true)));

    [Fact]
    public void InAScrollableMain_TheVeilIsTheLastChildOfTheFrame_NotInTheLandmarkNorWhereItIsDeclared()
    {
        var main = RenderMain(scrollable: true);

        var veil = main.Find(".omni-main-scroll > .omni-page-veil");
        Assert.Contains("omni-page-veil--frame", veil.ClassList);
        Assert.Equal(veil.OuterHtml, main.Find(".omni-main-scroll").LastElementChild!.OuterHtml);
        Assert.Empty(main.FindAll("main .omni-page-veil"));
        Assert.Empty(main.FindAll(".page-card .omni-page-veil"));
        Assert.Contains("omni-main-scroll--veiled", main.Find(".omni-main-scroll").ClassList);
        Assert.Equal("status", veil.GetAttribute("role"));
        Assert.Single(main.FindAll(".omni-page-veil"));
    }

    [Fact]
    public void InAMainThatDoesNotScroll_TheVeilIsTheLastChildOfTheLandmark()
    {
        var main = RenderMain(scrollable: false);

        var veil = main.Find("main > .omni-page-veil");
        Assert.Contains("omni-page-veil--flow", veil.ClassList);
        Assert.Equal(veil.OuterHtml, main.Find("main").LastElementChild!.OuterHtml);
        Assert.Empty(main.FindAll(".page-card .omni-page-veil"));
        Assert.Contains("omni-main--veiled", main.Find("main").ClassList);
        Assert.Empty(main.FindAll(".omni-main-scroll"));
    }

    [Fact]
    public void WithoutAVeil_TheMainAreaIsUnchanged()
    {
        var main = Render<OmniMain>(parameters => parameters.Add(component => component.Scrollable, true).AddChildContent("page"));

        Assert.Equal("omni-main-scroll", main.Find(".omni-main-scroll").ClassName);
        Assert.DoesNotContain("omni-main--veiled", main.Find("main").ClassList);
    }

    [Fact]
    public void OutsideAnyMain_TheVeilIsDrawnInPlaceOverTheViewport()
    {
        var page = Render<WaitingPage>(parameters => parameters.Add(component => component.Busy, true));

        var veil = page.Find(".page-card > .omni-page-veil");
        Assert.DoesNotContain("omni-page-veil--frame", veil.ClassList);
        Assert.DoesNotContain("omni-page-veil--flow", veil.ClassList);
    }

    [Fact]
    public void TheViewportScope_DrawsTheVeilInPlaceEvenInsideAMain()
    {
        var main = Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, true)
            .AddChildContent<WaitingPage>(page => page.Add(item => item.Busy, true).Add(item => item.Scope, OmniPageVeilScope.Viewport)));

        var veil = main.Find("main .page-card > .omni-page-veil");
        Assert.DoesNotContain("omni-page-veil--frame", veil.ClassList);
        Assert.Empty(main.FindAll(".omni-main-scroll > .omni-page-veil"));
        Assert.DoesNotContain("omni-main-scroll--veiled", main.Find(".omni-main-scroll").ClassList);
    }

    [Fact]
    public void ChangingTheScope_MovesTheVeilBetweenTheMainAndItsPlace()
    {
        var main = RenderMain(scrollable: true);
        var page = main.FindComponent<WaitingPage>();
        Assert.Single(main.FindAll(".omni-main-scroll > .omni-page-veil"));

        page.Instance.Update(scope: OmniPageVeilScope.Viewport);
        Assert.Empty(main.FindAll(".omni-main-scroll > .omni-page-veil"));
        Assert.Single(main.FindAll(".page-card > .omni-page-veil"));

        page.Instance.Update(scope: OmniPageVeilScope.Content);
        Assert.Single(main.FindAll(".omni-main-scroll > .omni-page-veil"));
        Assert.Empty(main.FindAll(".page-card > .omni-page-veil"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TogglingBusy_DrawsAndRemovesTheVeil_WhenOnlyTheVeilsParentRendersAgain(bool scrollable)
    {
        var main = RenderMain(scrollable);
        var page = main.FindComponent<WaitingPage>();

        page.Instance.Update(busy: false);
        Assert.Empty(main.FindAll(".omni-page-veil"));

        page.Instance.Update(busy: true);
        Assert.Single(main.FindAll(".omni-page-veil"));
    }

    [Fact]
    public void ChangingTheLogo_DrawsTheVeilAgain_WhenOnlyTheVeilsParentRendersAgain()
    {
        var main = RenderMain(scrollable: true);
        var page = main.FindComponent<WaitingPage>();
        Assert.Empty(main.FindAll(".omni-page-veil__logo"));

        page.Instance.Update(logo: Mark);
        Assert.NotNull(main.Find(".omni-main-scroll > .omni-page-veil .omni-page-veil__logo svg.app-logo"));

        page.Instance.Update(clearLogo: true);
        Assert.Empty(main.FindAll(".omni-page-veil__logo"));
    }

    [Fact]
    public void TheLabelAndTheAttributes_FollowTheVeilIntoTheMain()
    {
        var main = Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, true)
            .AddChildContent<OmniPageVeil>(veil => veil
                .Add(item => item.Busy, true)
                .Add(item => item.Id, "page-wait")
                .Add(item => item.Class, "app-veil")
                .Add(item => item.Label, "Ouverture du dossier")
                .AddUnmatched("data-testid", "veil")));

        var veil = main.Find(".omni-main-scroll > #page-wait");
        Assert.Contains("omni-page-veil", veil.ClassList);
        Assert.Contains("app-veil", veil.ClassList);
        Assert.Equal("veil", veil.GetAttribute("data-testid"));
        Assert.Equal("Ouverture du dossier", veil.QuerySelector(".omni-visually-hidden")!.TextContent);
    }

    [Fact]
    public void WhenTheVeilIsDisposed_TheMainAreaDrawsItNoMoreAndLosesItsVeiledClasses()
    {
        var main = RenderMain(scrollable: true);
        Assert.Single(main.FindAll(".omni-page-veil"));

        main.Render(parameters => parameters
            .Add(component => component.Scrollable, true)
            .AddChildContent("page"));

        Assert.Empty(main.FindAll(".omni-page-veil"));
        Assert.Equal("omni-main-scroll", main.Find(".omni-main-scroll").ClassName);
    }

    [Fact]
    public void ANestedMain_HoldsTheVeilDeclaredInItsOwnContent()
    {
        RenderFragment inner = builder =>
        {
            builder.OpenComponent<OmniMain>(0);
            builder.AddAttribute(1, nameof(OmniMain.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<WaitingPage>(0);
                content.AddComponentParameter(1, nameof(WaitingPage.Busy), true);
                content.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var outer = Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, true)
            .AddChildContent(inner));

        Assert.Single(outer.FindAll(".omni-page-veil"));
        Assert.Empty(outer.FindAll(".omni-main-scroll > .omni-page-veil"));
        Assert.Single(outer.FindAll("main main > .omni-page-veil--flow"));
    }
}
