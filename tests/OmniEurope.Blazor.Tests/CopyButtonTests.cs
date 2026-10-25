using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniCopyButton"/>: the exact text reaches the clipboard interop, a factory runs on the
/// click only, the feedback shows then goes back, and a refusal is announced without an exception.
/// </summary>
public sealed class CopyButtonTests : OmniBunitContext
{
    private const string InteropModulePath = Internal.OmniModules.Interop;
    private static readonly TimeSpan ShortFeedback = TimeSpan.FromMilliseconds(60);

    private BunitJSModuleInterop Clipboard(bool accepted)
    {
        var module = JSInterop.SetupModule(InteropModulePath);
        module.Setup<bool>("copyText", _ => true).SetResult(accepted);
        return module;
    }

    [Fact]
    public void Idle_ShowsTheLocalizedLabel_AndSaysNothing()
    {
        Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters.Add(component => component.Text, "x"));

        Assert.Equal("Copier", button.Find("button").TextContent.Trim());
        Assert.Contains("omni-button--secondary", button.Find("button").ClassList);
        Assert.Equal(string.Empty, button.Find("[role=status]").TextContent);
        Assert.Equal("polite", button.Find("[role=status]").GetAttribute("aria-live"));
        Assert.NotNull(button.Find("button svg"));
    }

    [Fact]
    public void Label_ReplacesTheDefaultText()
    {
        Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.Label, "Copier en Markdown"));

        Assert.Equal("Copier en Markdown", button.Find("button").TextContent.Trim());
    }

    [Fact]
    public void Click_HandsTheExactTextToTheClipboard()
    {
        var module = Clipboard(true);
        const string text = "# Titre\n\n```json\n{ \"a\": \"é <b>\" }\n```  \n";
        var button = Render<OmniCopyButton>(parameters => parameters.Add(component => component.Text, text));

        button.Find("button").Click();

        Assert.Equal(text, Assert.Single(module.Invocations["copyText"]).Arguments[0]);
    }

    [Fact]
    public async Task TextFactory_IsCalledOnTheClickOnly_AndItsResultIsCopied()
    {
        var module = Clipboard(true);
        var calls = 0;
        var button = Render<OmniCopyButton>(parameters => parameters.Add(component => component.TextFactory, () =>
        {
            calls++;
            return Task.FromResult($"report {calls}");
        }));

        Assert.Equal(0, calls);
        await button.Find("button").ClickAsync();
        Assert.Equal(1, calls);
        Assert.Equal("report 1", Assert.Single(module.Invocations["copyText"]).Arguments[0]);
    }

    [Fact]
    public void TextFactory_WinsOverText()
    {
        var module = Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "from text")
            .Add(component => component.TextFactory, () => Task.FromResult("from factory")));

        button.Find("button").Click();

        Assert.Equal("from factory", Assert.Single(module.Invocations["copyText"]).Arguments[0]);
    }

    [Fact]
    public void NeitherTextNorFactory_DisablesTheButton()
    {
        var module = Clipboard(true);
        var none = Render<OmniCopyButton>();
        var empty = Render<OmniCopyButton>(parameters => parameters.Add(component => component.Text, string.Empty));

        Assert.True(none.Find("button").HasAttribute("disabled"));
        Assert.True(empty.Find("button").HasAttribute("disabled"));
        Assert.Empty(module.Invocations["copyText"]);
    }

    [Fact]
    public void Copied_ShowsCopiedThenGoesBackToTheLabel()
    {
        Clipboard(true);
        bool? reported = null;
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.CopiedFeedbackDuration, ShortFeedback)
            .Add(component => component.OnCopy, copied => reported = copied));

        button.Find("button").Click();

        Assert.True(reported);
        Assert.Equal("Copié", button.Find("button").TextContent.Trim());
        Assert.Equal("Copié", button.Find("[role=status]").TextContent);

        button.WaitForAssertion(() =>
        {
            Assert.Equal("Copier", button.Find("button").TextContent.Trim());
            Assert.Equal(string.Empty, button.Find("[role=status]").TextContent);
        });
    }

    [Fact]
    public void IconOnly_IsTheSquareIconButton_NamedAndTitledByItsWords_ThroughTheFeedback()
    {
        Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.IconOnly, true)
            .Add(component => component.CopiedFeedbackDuration, ShortFeedback));

        var element = button.Find("button");
        Assert.Contains("omni-button--icon", element.ClassList);
        Assert.Equal(string.Empty, element.TextContent.Trim());
        Assert.NotNull(element.QuerySelector("svg"));
        Assert.Equal("Copier", element.GetAttribute("aria-label"));
        Assert.Equal("Copier", element.GetAttribute("title"));

        element.Click();

        Assert.Equal("Copié", button.Find("button").GetAttribute("aria-label"));
        Assert.Equal("Copié", button.Find("button").GetAttribute("title"));
        Assert.Equal("Copié", button.Find("[role=status]").TextContent);
        button.WaitForAssertion(() => Assert.Equal("Copier", button.Find("button").GetAttribute("aria-label")));
    }

    [Fact]
    public void IconOnly_KeepsAGivenLabelAsItsName()
    {
        Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.Label, "Copier l'identifiant")
            .Add(component => component.IconOnly, true));

        Assert.Equal("Copier l'identifiant", button.Find("button").GetAttribute("aria-label"));
        Assert.Equal("Copier l'identifiant", button.Find("button").GetAttribute("title"));
    }

    [Fact]
    public void ByDefault_ShowsItsWords_WithoutATitle()
    {
        Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters.Add(component => component.Text, "x"));

        Assert.DoesNotContain("omni-button--icon", button.Find("button").ClassList);
        Assert.Null(button.Find("button").GetAttribute("title"));
        Assert.Null(button.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void Refused_IsAnnouncedPolitely_ReportsFalse_AndNeverThrows()
    {
        Clipboard(false);
        bool? reported = null;
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.CopiedFeedbackDuration, ShortFeedback)
            .Add(component => component.OnCopy, copied => reported = copied));

        button.Find("button").Click();

        Assert.False(reported);
        Assert.Equal("La copie a échoué", button.Find("[role=status]").TextContent);
        Assert.Equal("La copie a échoué", button.Find("button").TextContent.Trim());
        button.WaitForAssertion(() => Assert.Equal("Copier", button.Find("button").TextContent.Trim()));
    }

    [Fact]
    public void ScriptFailure_IsTreatedAsARefusal()
    {
        var module = JSInterop.SetupModule(InteropModulePath);
        module.Setup<bool>("copyText", _ => true).SetException(new JSException("denied"));
        bool? reported = null;
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.OnCopy, copied => reported = copied));

        button.Find("button").Click();

        Assert.False(reported);
        Assert.Equal("La copie a échoué", button.Find("[role=status]").TextContent);
    }

    [Fact]
    public void Disabled_DoesNotCopy()
    {
        var module = Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.Disabled, true));

        Assert.True(button.Find("button").HasAttribute("disabled"));
        Assert.Empty(module.Invocations["copyText"]);
    }

    [Fact]
    public void Parameters_ReachTheButton()
    {
        Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.Variant, OmniButtonVariant.Ghost)
            .Add(component => component.Size, OmniControlSize.Small)
            .Add(component => component.Id, "copy-it")
            .Add(component => component.Class, "mine")
            .AddUnmatched("data-probe", "1"));

        var element = button.Find("button");
        Assert.Equal("copy-it", element.Id);
        Assert.Contains("omni-button--ghost", element.ClassList);
        Assert.Contains("omni-button--small", element.ClassList);
        Assert.Contains("mine", element.ClassList);
        Assert.Equal("1", element.GetAttribute("data-probe"));
    }

    [Fact]
    public void CustomIcon_IsKeptAfterACopy()
    {
        Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters
            .Add(component => component.Text, "x")
            .Add(component => component.Icon, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", "mine-icon");
                builder.CloseElement();
            })));

        button.Find("button").Click();

        Assert.NotNull(button.Find("button .mine-icon"));
    }

    [Fact]
    public async Task TextFactory_ReturningNull_CopiesAnEmptyText()
    {
        var module = Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters.Add(component => component.TextFactory, () => Task.FromResult<string>(null!)));

        await button.Find("button").ClickAsync();

        Assert.Equal(string.Empty, Assert.Single(module.Invocations["copyText"]).Arguments[0]);
    }

    [Fact]
    public void SecondClick_ReusesTheClipboardHelper_AndCopiesAgain()
    {
        var module = Clipboard(true);
        var button = Render<OmniCopyButton>(parameters => parameters.Add(component => component.Text, "x"));

        button.Find("button").Click();
        button.Find("button").Click();

        Assert.Equal(2, module.Invocations["copyText"].Count);
    }
}
