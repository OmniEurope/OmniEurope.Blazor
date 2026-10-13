using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniMailTemplateEditor"/>: the subject, one button per variable that writes its token where the
/// caret last was (subject or body), and the preview drawn by its script from <see cref="OmniMailRenderer"/>.
/// </summary>
public sealed class MailTemplateEditorTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniMailVariable> Variables =
    [
        new("client.name", "Nom du client") { SampleValue = "Anne Martin", Description = "Fiche du client" },
        new("file", "Dossier")
    ];

    private BunitJSModuleInterop SetupSurface()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.HtmlEditor);
        module.Setup<string?>("read", _ => true).SetResult(null);
        return module;
    }

    private IRenderedComponent<OmniMailTemplateEditor> RenderEditor(
        List<string?>? bodies = null,
        string? value = "<p>Bonjour {{client.name}}</p>",
        string? subject = "Dossier {{file}}",
        bool showSubject = true,
        bool disabled = false) =>
        Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Id, "mail")
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, (string? next) => bodies?.Add(next))
            .Add(component => component.Subject, subject)
            .Add(component => component.ShowSubject, showSubject)
            .Add(component => component.Disabled, disabled)
            .Add(component => component.Variables, Variables));

    [Fact]
    public void Renders_TheSubject_OneButtonPerVariable_AndTheTwoTabs()
    {
        SetupSurface();
        var editor = RenderEditor();

        Assert.Equal("Dossier {{file}}", editor.Find("#mail-subject").GetAttribute("value"));
        var buttons = editor.FindAll(".omni-mail-template-editor__variable");
        Assert.Equal(["Nom du client", "Dossier"], buttons.Select(button => button.TextContent.Trim()));
        Assert.Equal("{{client.name}} : Fiche du client", buttons[0].GetAttribute("title"));
        Assert.Equal("{{file}}", buttons[1].GetAttribute("title"));
        Assert.Equal("Insérer une variable", editor.Find(".omni-mail-template-editor__variables").GetAttribute("aria-label"));
        Assert.Equal(["Modèle", "Aperçu"], editor.FindAll("[role=tab]").Select(tab => tab.TextContent.Trim()));
        Assert.Single(editor.FindAll(".omni-html-editor__surface"));
    }

    [Fact]
    public void WithoutShowSubject_TheSubjectFieldIsNotRendered()
    {
        SetupSurface();
        var editor = RenderEditor(showSubject: false);

        Assert.Empty(editor.FindAll("#mail-subject"));
    }

    [Fact]
    public async Task AVariable_IsWrittenAtTheCaretOfTheBody_ByDefault()
    {
        var surface = SetupSurface();
        surface.Setup<string?>("exec", _ => true).SetResult("<p>Bonjour {{client.name}}{{file}}</p>");
        var bodies = new List<string?>();
        var editor = RenderEditor(bodies);

        await editor.FindAll(".omni-mail-template-editor__variable")[1].ClickAsync(new MouseEventArgs());

        var exec = Assert.Single(surface.Invocations["exec"]);
        Assert.Equal("inserttext", exec.Arguments[1]);
        Assert.Equal("{{file}}", exec.Arguments[2]);
        Assert.Equal(["<p>Bonjour {{client.name}}{{file}}</p>"], bodies);
    }

    [Fact]
    public async Task AVariable_IsWrittenInTheSubject_WhenTheSubjectWasFocusedLast()
    {
        var surface = SetupSurface();
        var preview = JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        preview.Setup<bool>("insertAtCaret", _ => true).SetResult(true);
        var editor = RenderEditor();

        await editor.Find(".omni-mail-template-editor__subject").TriggerEventAsync("onfocusin", new FocusEventArgs());
        await editor.FindAll(".omni-mail-template-editor__variable")[0].ClickAsync(new MouseEventArgs());

        var insertion = Assert.Single(preview.Invocations["insertAtCaret"]);
        Assert.Equal(["mail-subject", "{{client.name}}"], insertion.Arguments);
        Assert.Empty(surface.Invocations["exec"]);
    }

    [Fact]
    public async Task TheBody_TakesTheVariablesAgain_OnceFocusedAfterTheSubject()
    {
        var surface = SetupSurface();
        surface.Setup<string?>("exec", _ => true).SetResult("<p>x</p>");
        JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        var editor = RenderEditor();

        await editor.Find(".omni-mail-template-editor__subject").TriggerEventAsync("onfocusin", new FocusEventArgs());
        await editor.Find(".omni-mail-template-editor__body").TriggerEventAsync("onfocusin", new FocusEventArgs());
        await editor.FindAll(".omni-mail-template-editor__variable")[0].ClickAsync(new MouseEventArgs());

        Assert.Equal("{{client.name}}", Assert.Single(surface.Invocations["exec"]).Arguments[2]);
    }

    [Fact]
    public async Task ThePreviewTab_DrawsTheRenderedMail_WithTheSampleValues()
    {
        SetupSurface();
        var preview = JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        preview.SetupVoid("render", _ => true);
        var editor = RenderEditor();
        Assert.Empty(preview.Invocations["render"]);

        await editor.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());

        var html = (string)Assert.Single(preview.Invocations["render"]).Arguments[1]!;
        Assert.Contains("Bonjour Anne Martin", html, StringComparison.Ordinal);
        // The page's strict CSP refuses a style attribute even in a document parsed apart: the preview carries its looks elsewhere.
        Assert.DoesNotContain(" style=", html, StringComparison.Ordinal);
        Assert.Contains("data-omni-mail-look=", html, StringComparison.Ordinal);
        Assert.Contains("<title>Dossier [Dossier]</title>", html, StringComparison.Ordinal);
        Assert.Equal("Objet : Dossier [Dossier]", editor.Find(".omni-mail-template-editor__preview-subject").TextContent);

        // Drawn again only when the mail changes.
        editor.Render();
        Assert.Single(preview.Invocations["render"]);
        editor.Render(parameters => parameters.Add(component => component.Value, "<p>Autre</p>"));
        Assert.Equal(2, preview.Invocations["render"].Count);
    }

    [Fact]
    public void Disabled_DisablesTheVariableButtons()
    {
        SetupSurface();
        var editor = RenderEditor(disabled: true);

        Assert.All(editor.FindAll(".omni-mail-template-editor__variable"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.True(editor.Find("#mail-subject").HasAttribute("disabled"));
    }

    [Fact]
    public async Task WithoutVariablesOrId_NoButtonRow_AndThePreviewRendersWithoutSubject()
    {
        SetupSurface();
        var preview = JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        preview.SetupVoid("render", _ => true);
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Value, "<p>Seul</p>")
            .Add(component => component.ShowSubject, false));

        Assert.Empty(editor.FindAll(".omni-mail-template-editor__variables"));
        await editor.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());

        var html = (string)Assert.Single(preview.Invocations["render"]).Arguments[1]!;
        Assert.Contains("<title></title>", html, StringComparison.Ordinal);
        Assert.Empty(editor.FindAll(".omni-mail-template-editor__preview-subject"));
    }

    [Fact]
    public void AnEmptyVariableList_DrawsNoButtonRow_AndAnEditorWithoutId_NamesItsSubjectField()
    {
        SetupSurface();
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Variables, []));

        Assert.Empty(editor.FindAll(".omni-mail-template-editor__variables"));
        var subject = editor.Find(".omni-mail-template-editor__subject input");
        Assert.StartsWith("omni-mail-template-editor-", subject.Id, StringComparison.Ordinal);
        Assert.EndsWith("-subject", subject.Id, StringComparison.Ordinal);
        Assert.Equal(subject.Id, editor.Find(".omni-mail-template-editor__subject label").GetAttribute("for"));
    }

    [Fact]
    public async Task TypingTheSubject_RaisesSubjectChanged()
    {
        SetupSurface();
        var subjects = new List<string?>();
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Id, "mail")
            .Add(component => component.Subject, "Avant")
            .Add(component => component.SubjectChanged, (string? next) => subjects.Add(next)));

        await editor.Find("#mail-subject").InputAsync(new ChangeEventArgs { Value = "Après {{file}}" });

        editor.WaitForAssertion(() => Assert.Equal(["Après {{file}}"], subjects));
    }

    [Fact]
    public async Task ThePreview_IsDrawnAgain_WhenTheSubjectVariablesOrThemeChange()
    {
        SetupSurface();
        var preview = JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        preview.SetupVoid("render", _ => true);
        var editor = RenderEditor();
        await editor.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());
        Assert.Single(preview.Invocations["render"]);

        editor.Render(parameters => parameters.Add(component => component.Subject, "Autre objet"));
        Assert.Equal(2, preview.Invocations["render"].Count);
        editor.Render(parameters => parameters.Add(component => component.Variables, [new OmniMailVariable("file", "Dossier") { SampleValue = "X-9" }]));
        Assert.Equal(3, preview.Invocations["render"].Count);
        editor.Render(parameters => parameters.Add(component => component.Theme, OmniMailTheme.Default with { Surface = "#fafafa" }));
        Assert.Equal(4, preview.Invocations["render"].Count);
        Assert.Contains("background-color:#fafafa", (string)preview.Invocations["render"][3].Arguments[1]!, StringComparison.Ordinal);

        // Back to the template: nothing is drawn while the preview is hidden.
        await editor.FindAll("[role=tab]")[0].ClickAsync(new MouseEventArgs());
        editor.Render(parameters => parameters.Add(component => component.Value, "<p>Caché</p>"));
        Assert.Equal(4, preview.Invocations["render"].Count);
    }

    [Fact]
    public async Task ALostCircuit_DuringThePreview_IsQuiet_AndTheNextRenderDrawsAgain()
    {
        SetupSurface();
        var preview = JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        preview.SetupVoid("render", _ => true).SetException(new JSDisconnectedException("circuit perdu"));
        var editor = RenderEditor();

        await editor.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());
        Assert.Single(preview.Invocations["render"]);

        editor.Render();
        Assert.Equal(2, preview.Invocations["render"].Count);
    }

    [Fact]
    public async Task Disposing_OnALostCircuit_IsQuiet()
    {
        var lost = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(lost);
        var editor = RenderEditor();
        await editor.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());
        Assert.Contains("render", lost.Module.Calls);

        await editor.Instance.DisposeAsync();
        await editor.Instance.DisposeAsync();
    }

    [Fact]
    public async Task Disposing_WithoutThePreviewEverShown_ReleasesNothing()
    {
        SetupSurface();
        var editor = RenderEditor();

        await editor.Instance.DisposeAsync();
    }

    [Fact]
    public void WithoutOnSendTest_ThereIsNoTestButton()
    {
        SetupSurface();
        var editor = RenderEditor();

        Assert.Empty(editor.FindAll(".omni-mail-template-editor__send-test"));
    }

    [Fact]
    public async Task SendATest_HandsTheMailAsSent_WithTheSampleValues_BusyMeanwhile()
    {
        SetupSurface();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        OmniMailRenderResult? received = null;
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Value, "<p>Bonjour {{client.name}}</p>")
            .Add(component => component.Subject, "Dossier {{file}}")
            .Add(component => component.Variables, Variables)
            .Add(component => component.OnSendTest, (OmniMailRenderResult mail) => { received = mail; return pending.Task; }));

        var button = editor.Find(".omni-mail-template-editor__send-test");
        Assert.Equal("Envoyer un essai", button.TextContent.Trim());
        var click = button.ClickAsync(new MouseEventArgs());
        editor.WaitForAssertion(() => Assert.Equal("true", editor.Find(".omni-mail-template-editor__send-test").GetAttribute("aria-busy")));

        Assert.NotNull(received);
        Assert.Contains("Bonjour Anne Martin", received.Html, StringComparison.Ordinal);
        // The mail as a mail client receives it: its looks in style attributes, not in the preview's attribute.
        Assert.Contains(" style=", received.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-omni-mail-look", received.Html, StringComparison.Ordinal);
        Assert.Equal("Dossier [Dossier]", received.Subject);

        pending.SetResult();
        await click;
        editor.WaitForAssertion(() => Assert.Null(editor.Find(".omni-mail-template-editor__send-test").GetAttribute("aria-busy")));
    }

    [Fact]
    public async Task AFailedTest_GoesToOnSendTestError_AndTheButtonIsFreed()
    {
        SetupSurface();
        var errors = new List<Exception>();
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Value, "<p>x</p>")
            .Add(component => component.OnSendTest, (OmniMailRenderResult _) => throw new InvalidOperationException("serveur de mail absent"))
            .Add(component => component.OnSendTestError, (Exception exception) => errors.Add(exception)));

        await editor.Find(".omni-mail-template-editor__send-test").ClickAsync(new MouseEventArgs());

        Assert.Equal("serveur de mail absent", Assert.Single(errors).Message);
        Assert.Null(editor.Find(".omni-mail-template-editor__send-test").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task AFailedTest_WithoutAnErrorHandler_Propagates()
    {
        SetupSurface();
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Value, "<p>x</p>")
            .Add(component => component.OnSendTest, (OmniMailRenderResult _) => throw new InvalidOperationException("serveur de mail absent")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => editor.Find(".omni-mail-template-editor__send-test").ClickAsync(new MouseEventArgs()));
    }

    [Fact]
    public async Task TheTokensOfTheBody_AreMarked_OnEachVisualSurface_AndReleasedOnDisposal()
    {
        var surface = SetupSurface();
        var preview = JSInterop.SetupModule(Internal.OmniModules.MailPreview);
        preview.Setup<int>("attachTokens", _ => true).SetResult(1);
        preview.SetupVoid("detachTokens", _ => true).SetVoidResult();
        var editor = RenderEditor();

        var first = Assert.Single(preview.Invocations["attachTokens"]);
        var firstSurface = (ElementReference)first.Arguments[0]!;

        // The source face has no surface; the visual face coming back is a new one, marked again.
        await editor.Find("[data-command=toggle-source]").ClickAsync(new MouseEventArgs());
        Assert.Single(preview.Invocations["attachTokens"]);
        await editor.Find("[data-command=toggle-source]").ClickAsync(new MouseEventArgs());
        Assert.Equal(2, preview.Invocations["attachTokens"].Count);
        Assert.NotEqual(firstSurface.Id, ((ElementReference)preview.Invocations["attachTokens"][1].Arguments[0]!).Id);

        await editor.Instance.DisposeAsync();
        Assert.Single(preview.Invocations["detachTokens"]);
    }

    [Fact]
    public void AVariableNameATemplateCannotWrite_IsRefused()
    {
        SetupSurface();
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Variables, [new OmniMailVariable("1 nom", "Mauvais")])));

        Assert.Contains("1 nom", exception.Message, StringComparison.Ordinal);
    }
}
