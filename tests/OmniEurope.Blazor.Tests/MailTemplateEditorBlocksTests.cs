using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The blocks of <see cref="OmniMailTemplateEditor"/>: the « Insert a block » group (a banner in each tone, an action
/// button per link variable), block variables written in a paragraph of their own, and the preview that draws them.
/// </summary>
public sealed class MailTemplateEditorBlocksTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniMailVariable> Variables =
    [
        new("run.name", "Nom du run") { SampleValue = "nightly" },
        new("run.url", "Lien du run") { Kind = OmniMailVariableKind.Link, SampleValue = "https://app.example/runs/42" },
        new("steps", "Étapes en échec") { Kind = OmniMailVariableKind.Block, SampleValue = "<table><tr><td>build</td></tr></table>" },
        new("summary", "Résumé") { Kind = OmniMailVariableKind.Block },
    ];

    private BunitJSModuleInterop SetupSurface(string? afterExec = null)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.HtmlEditor);
        module.Setup<string?>("read", _ => true).SetResult(null);
        module.Setup<string?>("exec", _ => true).SetResult(afterExec);
        return module;
    }

    private IRenderedComponent<OmniMailTemplateEditor> RenderEditor(string? value = "<p>Bonjour</p>", bool showBlocks = true, bool readOnly = false) =>
        Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Id, "mail")
            .Add(component => component.Value, value)
            .Add(component => component.ShowBlocks, showBlocks)
            .Add(component => component.ReadOnly, readOnly)
            .Add(component => component.Variables, Variables));

    [Fact]
    public void TheBlocksGroup_OffersTheBanner_AndOneActionButtonPerLinkVariable()
    {
        SetupSurface();
        var editor = RenderEditor();

        var group = editor.Find(".omni-mail-template-editor__blocks");
        Assert.Equal("group", group.GetAttribute("role"));
        Assert.Equal("Insérer un bloc", group.GetAttribute("aria-label"));
        Assert.Contains("Bandeau", editor.Find(".omni-mail-template-editor__banner").TextContent, StringComparison.Ordinal);
        var button = Assert.Single(editor.FindAll(".omni-mail-template-editor__button"));
        Assert.Equal("Bouton : Lien du run", button.TextContent.Trim());
        Assert.Equal("{{#button run.url}}", button.GetAttribute("title"));
    }

    [Fact]
    public void ADisabledEditor_DisablesTheActionButtons()
    {
        SetupSurface();
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Id, "mail")
            .Add(component => component.Value, "<p>Bonjour</p>")
            .Add(component => component.Disabled, true)
            .Add(component => component.Variables, Variables));

        Assert.True(editor.Find(".omni-mail-template-editor__button").HasAttribute("disabled"));
    }

    [Fact]
    public void WithoutShowBlocks_TheGroupIsNotRendered_AndReadOnlyDisablesIt()
    {
        SetupSurface();
        Assert.Empty(RenderEditor(showBlocks: false).FindAll(".omni-mail-template-editor__blocks"));

        var readOnly = RenderEditor(readOnly: true);
        Assert.True(readOnly.Find(".omni-mail-template-editor__button").HasAttribute("disabled"));
        Assert.All(readOnly.FindAll(".omni-mail-template-editor__banner button"), part => Assert.True(part.HasAttribute("disabled")));
    }

    [Fact]
    public async Task TheBannerButton_WritesAnInformationBanner_InAParagraphOfItsOwn()
    {
        var surface = SetupSurface();
        var editor = RenderEditor();

        await editor.Find(".omni-mail-template-editor__banner button").ClickAsync(new MouseEventArgs());

        var exec = Assert.Single(surface.Invocations["exec"]);
        Assert.Equal("inserthtml", exec.Arguments[1]);
        Assert.Equal("<p>{{#banner info}} </p>", exec.Arguments[2]);
    }

    [Fact]
    public async Task TheBannerMenu_WritesEachTone()
    {
        var surface = SetupSurface();
        var editor = RenderEditor();

        await editor.FindAll(".omni-mail-template-editor__banner button")[^1].ClickAsync(new MouseEventArgs());
        var items = editor.FindAll("[data-omni-banner]");
        Assert.Equal(["Information", "Succès", "Avertissement", "Danger"], items.Select(item => item.TextContent.Trim()));
        await editor.Find("[data-omni-banner=warning]").ClickAsync(new MouseEventArgs());

        Assert.Equal("<p>{{#banner warning}} </p>", Assert.Single(surface.Invocations["exec"]).Arguments[2]);
    }

    [Fact]
    public async Task TheActionButton_WritesItsMarker_PointingToTheLinkVariable()
    {
        var surface = SetupSurface();
        var editor = RenderEditor();

        await editor.Find(".omni-mail-template-editor__button").ClickAsync(new MouseEventArgs());

        Assert.Equal("<p>{{#button run.url}} </p>", Assert.Single(surface.Invocations["exec"]).Arguments[2]);
    }

    [Fact]
    public async Task ABlockVariable_IsWrittenInAParagraphOfItsOwn_ATextOrLinkVariableAsText()
    {
        var surface = SetupSurface();
        var editor = RenderEditor();
        var buttons = editor.FindAll(".omni-mail-template-editor__variable");

        await buttons[2].ClickAsync(new MouseEventArgs());
        await editor.FindAll(".omni-mail-template-editor__variable")[1].ClickAsync(new MouseEventArgs());

        var calls = surface.Invocations["exec"];
        Assert.Equal(("inserthtml", "<p>{{steps}}</p>"), ((string)calls[0].Arguments[1]!, (string)calls[0].Arguments[2]!));
        Assert.Equal(("inserttext", "{{run.url}}"), ((string)calls[1].Arguments[1]!, (string)calls[1].Arguments[2]!));
    }

    [Fact]
    public async Task ThePreview_DrawsTheButtonBannerAndBlocks_WithTheSampleValues()
    {
        SetupSurface();
        var preview = JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        preview.SetupVoid("render", _ => true);
        var editor = RenderEditor("<p>{{#banner danger}} Le run {{run.name}} a échoué.</p><p>{{steps}}</p><p>{{summary}}</p><p>{{#button run.url}}Ouvrir</p>");

        await editor.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());

        var html = (string)Assert.Single(preview.Invocations["render"]).Arguments[1]!;
        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        Assert.Equal("https://app.example/runs/42", document.QuerySelector("a")!.GetAttribute("href"));
        Assert.Contains(document.QuerySelectorAll("td"), cell => cell.TextContent.Trim() == "Le run nightly a échoué.");
        Assert.Contains(document.QuerySelectorAll("td"), cell => cell.TextContent == "build");
        Assert.Contains(document.QuerySelectorAll("p"), paragraph => paragraph.TextContent == "[Résumé]");
        Assert.DoesNotContain(" style=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", html, StringComparison.Ordinal);
    }
}
