using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniAccordion"/> and <see cref="OmniAccordionItem"/>: the ARIA wiring, single and
/// multiple opening, the initial and bound state, the bodies dropped or kept, and the header whose
/// extra controls stay beside the toggle. What a browser alone proves (Enter and Space on the button,
/// Up, Down, Home and End between the toggles, the stretched click area) is covered by the
/// showcase keyboard probe, not here.
/// </summary>
public sealed class AccordionTests : OmniBunitContext
{
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";

    private IRenderedComponent<OmniAccordion> RenderAccordion(Action<ComponentParameterCollectionBuilder<OmniAccordion>>? configure = null, RenderFragment? rows = null) =>
        Render<OmniAccordion>(parameters =>
        {
            configure?.Invoke(parameters);
            parameters.Add(component => component.ChildContent, rows ?? Rows(open: false));
        });

    [Fact]
    public void Closed_ExposesTheToggleAsAButtonControllingAHiddenRegionNamedByIt()
    {
        var accordion = RenderAccordion();

        var toggle = accordion.Find("button.omni-accordion__toggle");
        var body = accordion.Find(".omni-accordion__body");
        Assert.Equal("button", toggle.LocalName);
        Assert.Equal("button", toggle.GetAttribute("type"));
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Equal(body.Id, toggle.GetAttribute("aria-controls"));
        Assert.Equal("region", body.GetAttribute("role"));
        Assert.Equal(toggle.Id, body.GetAttribute("aria-labelledby"));
        Assert.True(body.HasAttribute("hidden"));
        Assert.Empty(accordion.FindAll(".omni-accordion__content"));
        Assert.DoesNotContain("Corps premier", accordion.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheToggle_IsWrappedInAHeadingOfTheChosenLevel()
    {
        var byDefault = RenderAccordion();
        var h2 = RenderAccordion(parameters => parameters.Add(component => component.HeadingLevel, OmniHeadingLevel.H2));

        var heading = byDefault.Find(".omni-accordion__heading");
        Assert.Equal("heading", heading.GetAttribute("role"));
        Assert.Equal("3", heading.GetAttribute("aria-level"));
        Assert.NotNull(heading.QuerySelector("button.omni-accordion__toggle"));
        Assert.Equal("2", h2.Find(".omni-accordion__heading").GetAttribute("aria-level"));
    }

    [Fact]
    public void ClickingTheToggle_OpensThenClosesTheRow()
    {
        var accordion = RenderAccordion();
        var toggle = accordion.Find("button.omni-accordion__toggle");

        toggle.Click();

        toggle = accordion.Find("button.omni-accordion__toggle");
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
        Assert.False(accordion.Find(".omni-accordion__body").HasAttribute("hidden"));
        Assert.Contains("Corps premier", accordion.Find(".omni-accordion__content").TextContent, StringComparison.Ordinal);
        Assert.Contains("omni-accordion__item--open", accordion.Find(".omni-accordion__item").ClassList);

        toggle.Click();

        Assert.Equal("false", accordion.Find("button.omni-accordion__toggle").GetAttribute("aria-expanded"));
        Assert.True(accordion.Find(".omni-accordion__body").HasAttribute("hidden"));
        Assert.Empty(accordion.FindAll(".omni-accordion__content"));
    }

    [Fact]
    public void TheToggle_IsANativeButton_SoEnterAndSpaceActivateItWithoutScript()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;

        var accordion = RenderAccordion();

        var toggle = accordion.Find(".omni-accordion__toggle");
        Assert.Equal("button", toggle.LocalName);
        Assert.False(toggle.HasAttribute("disabled"));
        Assert.False(toggle.HasAttribute("role"));
        Assert.NotEqual("-1", toggle.GetAttribute("tabindex"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void FocusEnteringTheGroup_TurnsOnThePageKeysOnce_AndNotBefore()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.SetupVoid("enablePageKeys");
        var accordion = RenderAccordion();

        Assert.Empty(module.Invocations["enablePageKeys"]);
        Assert.NotNull(accordion.Find("[data-omni-accordion]"));
        Assert.NotNull(accordion.Find("[data-omni-accordion-toggle]"));

        accordion.Find("button.omni-accordion__toggle").FocusIn();
        accordion.Find("button.omni-accordion__toggle").FocusIn();

        Assert.Single(module.Invocations["enablePageKeys"]);
    }

    [Fact]
    public void ByDefault_OpeningARowClosesTheOneThatWasOpen()
    {
        var accordion = RenderTwoRows(multiple: false);

        Toggle(accordion, 0).Click();
        Assert.Equal(["true", "false"], ExpandedStates(accordion));

        Toggle(accordion, 1).Click();
        Assert.Equal(["false", "true"], ExpandedStates(accordion));
        Assert.DoesNotContain("Corps premier", accordion.Markup, StringComparison.Ordinal);
        Assert.Contains("Corps second", accordion.Markup, StringComparison.Ordinal);

        Toggle(accordion, 1).Click();
        Assert.Equal(["false", "false"], ExpandedStates(accordion));
    }

    [Fact]
    public void Multiple_KeepsEveryOpenedRowOpen()
    {
        var accordion = RenderTwoRows(multiple: true);

        Toggle(accordion, 0).Click();
        Toggle(accordion, 1).Click();

        Assert.Equal(["true", "true"], ExpandedStates(accordion));
        Assert.Contains("Corps premier", accordion.Markup, StringComparison.Ordinal);
        Assert.Contains("Corps second", accordion.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Expanded_OpensTheRowFromTheFirstRender_ItsBodyIncluded()
    {
        var accordion = RenderAccordion(rows: Rows(open: true));

        Assert.Equal("true", accordion.Find("button.omni-accordion__toggle").GetAttribute("aria-expanded"));
        Assert.False(accordion.Find(".omni-accordion__body").HasAttribute("hidden"));
        Assert.Contains("Corps premier", accordion.Find(".omni-accordion__content").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ClosedBodies_AreDroppedByDefault_AndKeptHiddenWithKeepContent()
    {
        var dropped = RenderTwoRows(multiple: true);
        var kept = RenderTwoRows(multiple: true, keepContent: true);

        foreach (var accordion in new[] { dropped, kept })
        {
            Toggle(accordion, 0).Click();
            Toggle(accordion, 0).Click();
        }

        Assert.DoesNotContain("Corps premier", dropped.Markup, StringComparison.Ordinal);
        Assert.Contains("Corps premier", kept.Markup, StringComparison.Ordinal);
        Assert.True(kept.FindAll(".omni-accordion__body")[0].HasAttribute("hidden"));
        // A row never opened has nothing to keep.
        Assert.DoesNotContain("Corps second", kept.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheHelpMark_SitsBesideTheToggle_NeverInsideIt_AndDoesNotToggleTheRow()
    {
        var accordion = RenderAccordion(rows: builder =>
        {
            builder.OpenComponent<OmniAccordionItem>(0);
            builder.AddAttribute(1, nameof(OmniAccordionItem.Title), "Qualité");
            builder.AddAttribute(2, nameof(OmniAccordionItem.Help), "Fiabilité du code.");
            builder.AddAttribute(3, nameof(OmniAccordionItem.ChildContent), (RenderFragment)(content => content.AddContent(0, "Corps")));
            builder.CloseComponent();
        });

        var toggle = accordion.Find("button.omni-accordion__toggle");
        var tooltip = accordion.Find(".omni-accordion__tools .omni-tooltip");
        Assert.Equal("Fiabilité du code.", tooltip.QuerySelector("[role=tooltip]")!.TextContent);
        Assert.NotNull(tooltip.QuerySelector(".omni-tooltip__trigger[tabindex='0']"));
        Assert.False(toggle.Contains(tooltip));
        Assert.Empty(toggle.QuerySelectorAll("a, button, input, select, textarea, summary, [tabindex], [role=button], .omni-tooltip"));
        // No ancestor of the mark listens to a click or a key (the toggle's handler is on the button
        // alone), so neither can toggle the row: bUnit finds no handler to run.
        var trigger = tooltip.QuerySelector(".omni-tooltip__trigger")!;
        Assert.Throws<MissingEventHandlerException>(() => trigger.Click());
        Assert.Throws<MissingEventHandlerException>(() => trigger.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        Assert.Throws<MissingEventHandlerException>(() => trigger.KeyDown(new KeyboardEventArgs { Key = " " }));
        Assert.Equal("false", accordion.Find("button.omni-accordion__toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ActionsContent_IsDrawnBesideTheToggle_AndAClickOnItDoesNotToggleTheRow()
    {
        var clicks = 0;
        var accordion = RenderAccordion(rows: builder =>
        {
            builder.OpenComponent<OmniAccordionItem>(0);
            builder.AddAttribute(1, nameof(OmniAccordionItem.Title), "Qualité");
            builder.AddAttribute(2, nameof(OmniAccordionItem.ActionsContent), (RenderFragment)(content =>
            {
                content.OpenElement(0, "button");
                content.AddAttribute(1, "type", "button");
                content.AddAttribute(2, "class", "mine");
                content.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => clicks++));
                content.AddContent(4, "Ouvrir");
                content.CloseElement();
            }));
            builder.CloseComponent();
        });

        accordion.Find(".omni-accordion__tools button.mine").Click();

        Assert.Equal(1, clicks);
        Assert.Equal("false", accordion.Find("button.omni-accordion__toggle").GetAttribute("aria-expanded"));
        Assert.Empty(accordion.Find("button.omni-accordion__toggle").QuerySelectorAll("button"));
    }

    [Fact]
    public void HeaderContent_ReplacesTheTitleInsideTheToggle_AndSummaryDescribesIt()
    {
        var accordion = RenderAccordion(rows: builder =>
        {
            builder.OpenComponent<OmniAccordionItem>(0);
            builder.AddAttribute(1, nameof(OmniAccordionItem.Title), "Ignoré");
            builder.AddAttribute(2, nameof(OmniAccordionItem.HeaderContent), (RenderFragment)(content => content.AddMarkupContent(0, "<span class=\"mine\">Libellé libre</span>")));
            builder.AddAttribute(3, nameof(OmniAccordionItem.SummaryContent), (RenderFragment)(content => content.AddContent(0, "A")));
            builder.CloseComponent();
        });

        var toggle = accordion.Find("button.omni-accordion__toggle");
        var summary = accordion.Find(".omni-accordion__summary");
        Assert.Equal("Libellé libre", toggle.TextContent);
        Assert.DoesNotContain("Ignoré", accordion.Markup, StringComparison.Ordinal);
        Assert.Equal(summary.Id, toggle.GetAttribute("aria-describedby"));
        Assert.False(toggle.Contains(summary));
        // The chevron is the component's own, decorative, and turns by class.
        Assert.Equal("true", accordion.Find(".omni-accordion__chevron").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void WithoutSummary_TheToggleHasNoDescription()
    {
        var accordion = RenderAccordion();

        Assert.False(accordion.Find("button.omni-accordion__toggle").HasAttribute("aria-describedby"));
        Assert.Empty(accordion.FindAll(".omni-accordion__summary"));
        Assert.Empty(accordion.FindAll(".omni-accordion__tools"));
    }

    [Fact]
    public async Task BoundExpanded_FollowsTheReader_AndTheHostCanOpenARow()
    {
        var host = Render<AccordionBindingTestHost>();

        host.FindAll("button.omni-accordion__toggle")[0].Click();
        Assert.True(host.Instance.First);
        Assert.False(host.Instance.Second);

        // Single mode: opening the second closes the first, and the first reports it to its binding.
        host.FindAll("button.omni-accordion__toggle")[1].Click();
        Assert.False(host.Instance.First);
        Assert.True(host.Instance.Second);
        Assert.Equal(["false", "true"], host.FindAll("button.omni-accordion__toggle").Select(button => button.GetAttribute("aria-expanded")));

        // The host opens the first row from its own code: the row follows.
        await host.InvokeAsync(() => host.Instance.SetFirst(true));
        Assert.Equal("true", host.FindAll("button.omni-accordion__toggle")[0].GetAttribute("aria-expanded"));
        Assert.Contains("Corps premier", host.Markup, StringComparison.Ordinal);

        // The reader closes it, the binding follows.
        host.FindAll("button.omni-accordion__toggle")[0].Click();
        Assert.False(host.Instance.First);
        Assert.Equal("false", host.FindAll("button.omni-accordion__toggle")[0].GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ExpandedChanged_ReportsTheRowClosedByAPeer()
    {
        var reported = new List<string>();
        var accordion = RenderAccordion(rows: builder =>
        {
            foreach (var (index, title) in new[] { "A", "B" }.Index())
            {
                builder.OpenComponent<OmniAccordionItem>(index * 3);
                builder.AddAttribute((index * 3) + 1, nameof(OmniAccordionItem.Title), title);
                builder.AddAttribute((index * 3) + 2, nameof(OmniAccordionItem.ExpandedChanged), EventCallback.Factory.Create<bool>(this, open => reported.Add($"{title}:{open}")));
                builder.CloseComponent();
            }
        });

        Toggle(accordion, 0).Click();
        Toggle(accordion, 1).Click();

        Assert.Equal(["A:True", "A:False", "B:True"], reported);
    }

    [Fact]
    public void ARowAlone_IsADisclosureThatOpensAndClosesByItself()
    {
        var item = Render<OmniAccordionItem>(parameters => parameters
            .Add(component => component.Title, "Détails")
            .AddChildContent("Corps seul"));

        var toggle = item.Find("button.omni-accordion__toggle");
        Assert.Equal("3", item.Find(".omni-accordion__heading").GetAttribute("aria-level"));
        toggle.Click();

        Assert.Equal("true", item.Find("button.omni-accordion__toggle").GetAttribute("aria-expanded"));
        Assert.Contains("Corps seul", item.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ARowWithoutTitleNorHeaderContent_DrawsAnEmptyToggleThatStillWorks()
    {
        var item = Render<OmniAccordionItem>(parameters => parameters.AddChildContent("Corps sans libellé"));

        var toggle = item.Find("button.omni-accordion__toggle");
        Assert.Equal(string.Empty, toggle.TextContent);
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));

        toggle.Click();

        Assert.Equal("true", item.Find("button.omni-accordion__toggle").GetAttribute("aria-expanded"));
        Assert.Contains("Corps sans libellé", item.Find(".omni-accordion__content").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void IdClassAndAttributes_GoWhereTheConventionPutsThem()
    {
        var item = Render<OmniAccordionItem>(parameters => parameters
            .Add(component => component.Id, "ligne-un")
            .Add(component => component.Class, "mine")
            .Add(component => component.Title, "Un")
            .AddUnmatched("data-probe", "x")
            .AddChildContent("Corps"));

        var toggle = item.Find("button.omni-accordion__toggle");
        var outer = item.Find(".omni-accordion__item");
        Assert.Equal("ligne-un", toggle.Id);
        Assert.Equal("ligne-un-body", item.Find(".omni-accordion__body").Id);
        Assert.Equal("ligne-un", item.Find(".omni-accordion__body").GetAttribute("aria-labelledby"));
        Assert.Contains("mine", outer.ClassList);
        Assert.Equal("x", outer.GetAttribute("data-probe"));
    }

    [Fact]
    public void Density_IsWrittenOnTheContainer_AndNoInlineStyleIsEmitted()
    {
        var accordion = RenderAccordion(parameters => parameters.Add(component => component.Density, OmniDensity.Compact), Rows(open: true));

        Assert.Equal("compact", accordion.Find(".omni-accordion").GetAttribute("data-omni-density"));
        Assert.DoesNotContain("style=", accordion.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private IRenderedComponent<OmniAccordion> RenderTwoRows(bool multiple, bool keepContent = false) =>
        RenderAccordion(
            parameters => parameters
                .Add(component => component.Multiple, multiple)
                .Add(component => component.KeepContent, keepContent),
            builder =>
            {
                Row(builder, 0, "Premier", "Corps premier", open: false);
                Row(builder, 10, "Second", "Corps second", open: false);
            });

    private static RenderFragment Rows(bool open) => builder => Row(builder, 0, "Premier", "Corps premier", open);

    private static void Row(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence, string title, string body, bool open)
    {
        builder.OpenComponent<OmniAccordionItem>(sequence);
        builder.AddAttribute(sequence + 1, nameof(OmniAccordionItem.Title), title);
        builder.AddAttribute(sequence + 2, nameof(OmniAccordionItem.Expanded), open);
        builder.AddAttribute(sequence + 3, nameof(OmniAccordionItem.ChildContent), (RenderFragment)(content => content.AddContent(0, body)));
        builder.CloseComponent();
    }

    private static IElement Toggle(IRenderedComponent<OmniAccordion> accordion, int index) =>
        accordion.FindAll("button.omni-accordion__toggle")[index];

    private static string?[] ExpandedStates(IRenderedComponent<OmniAccordion> accordion) =>
        [.. accordion.FindAll("button.omni-accordion__toggle").Select(button => button.GetAttribute("aria-expanded"))];
}
