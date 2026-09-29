using System.Xml.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-007 lot 9: the Selection and Editor families name their controls through the form label unless
/// a <c>Label</c> is given, share one surface between the two choice lists, report the form's
/// validation state, and give every editor the same <c>ReadOnly</c> and <c>Disabled</c>.
/// </summary>
public sealed class SelectionEditorCoherenceTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Choices =
    [
        new("a", "Alpha"),
        new("b", "Beta", Disabled: true)
    ];

    private sealed class Model
    {
        public string Choice { get; set; } = string.Empty;

        public IReadOnlyList<string> Many { get; set; } = [];

        public double Amount { get; set; }

        public int? Stars { get; set; }
    }

    // ---- the form label names the control ----

    [Fact]
    public void DropDown_WithoutLabel_WritesNoAriaLabel_SoTheFormLabelNamesIt()
    {
        var value = string.Empty;
        var page = Render(builder =>
        {
            builder.OpenComponent<OmniLabel>(0);
            builder.AddComponentParameter(1, nameof(OmniLabel.For), "country");
            builder.AddComponentParameter(2, nameof(OmniLabel.ChildContent), (RenderFragment)(label => label.AddContent(0, "Pays")));
            builder.CloseComponent();
            builder.OpenComponent<OmniDropDown<string>>(3);
            builder.AddComponentParameter(4, nameof(OmniDropDown<string>.Id), "country");
            builder.AddComponentParameter(5, nameof(OmniDropDown<string>.Options), Choices);
            builder.AddComponentParameter(6, nameof(OmniDropDown<string>.Value), value);
            builder.AddComponentParameter(7, nameof(OmniDropDown<string>.ValueExpression), (System.Linq.Expressions.Expression<Func<string>>)(() => value));
            builder.CloseComponent();
        });

        var select = page.Find("select");
        Assert.Equal("country", select.Id);
        Assert.Equal("country", page.Find("label").GetAttribute("for"));
        // The generic placeholder "Sélectionner" used to be written here and overrode the label.
        Assert.Null(select.GetAttribute("aria-label"));
    }

    [Fact]
    public void DropDown_Label_IsWrittenAsTheAriaLabel()
    {
        var value = string.Empty;
        var dropDown = Render<OmniDropDown<string>>(parameters => parameters
            .Add(component => component.Options, Choices)
            .Add(component => component.Label, "Pays")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal("Pays", dropDown.Find("select").GetAttribute("aria-label"));
    }

    [Fact]
    public void CodeEditor_Fallback_WritesAnAriaLabel_OnlyWhenLabelIsSet()
    {
        var value = string.Empty;
        var unnamed = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Null(unnamed.Find("textarea").GetAttribute("aria-label"));

        var named = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.Label, "Manifeste")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Equal("Manifeste", named.Find("textarea").GetAttribute("aria-label"));
    }

    [Fact]
    public void CodeEditor_Wrap_WrapsTheFallback()
    {
        var value = string.Empty;
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.Wrap, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal("soft", editor.Find("textarea").GetAttribute("wrap"));
    }

    [Fact]
    public void HtmlEditor_SourceTextArea_WritesAnAriaLabel_OnlyWhenLabelIsSet()
    {
        var value = "<p>a</p>";
        var unnamed = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Null(unnamed.Find("textarea").GetAttribute("aria-label"));

        var named = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Label, "Contenu")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Equal("Contenu", named.Find("textarea").GetAttribute("aria-label"));
    }

    // ---- ReadOnly and Disabled on every editor ----

    [Fact]
    public void HtmlEditor_ReadOnly_LocksTheValue_WithoutDimming()
    {
        var value = "<p>a</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.Bold])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var surface = editor.Find("[role=textbox]");
        Assert.Equal("false", surface.GetAttribute("contenteditable"));
        Assert.Equal("true", surface.GetAttribute("aria-readonly"));
        Assert.Equal("0", surface.GetAttribute("tabindex"));
        Assert.Null(surface.GetAttribute("aria-disabled"));
        Assert.True(editor.Find("[data-command=bold]").HasAttribute("disabled"));
        Assert.Contains("omni-html-editor--readonly", editor.Find(".omni-html-editor").ClassList);
        Assert.DoesNotContain("omni-html-editor--disabled", editor.Find(".omni-html-editor").ClassList);
    }

    [Fact]
    public void HtmlEditor_ReadOnly_SourceTextAreaIsReadOnly_AndIgnoresInput()
    {
        var value = "<p>a</p>";
        var changed = false;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string _) => changed = true));

        var textarea = editor.Find("textarea");
        Assert.True(textarea.HasAttribute("readonly"));
        Assert.False(textarea.HasAttribute("disabled"));
        textarea.Input("<p>b</p>");
        Assert.False(changed);
    }

    [Fact]
    public void HtmlEditor_Disabled_IsDimmed()
    {
        var value = "<p>a</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Disabled, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Contains("omni-html-editor--disabled", editor.Find(".omni-html-editor").ClassList);
        Assert.Equal("true", editor.Find("[role=textbox]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void DiffViewer_Disabled_LocksAnEditableModifiedText_AndDims()
    {
        var changed = false;
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.ReadOnly, false)
            .Add(component => component.Disabled, true)
            .Add(component => component.Original, "a")
            .Add(component => component.Modified, "b")
            .Add(component => component.ModifiedChanged, (string _) => changed = true));

        var textarea = viewer.Find("textarea");
        Assert.True(textarea.HasAttribute("disabled"));
        textarea.Input("c");
        Assert.False(changed);
        Assert.Contains("omni-diff-viewer--disabled", viewer.Find(".omni-diff-viewer").ClassList);
    }

    // ---- code block and unified diff names ----

    [Fact]
    public void CodeBlock_Title_IsDrawnByTheSharedHeader()
    {
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, "dotnet test")
            .Add(component => component.Title, "Commande de test")
            .Add(component => component.Language, "shell"));

        Assert.Equal("Commande de test", block.Find("figcaption.omni-code-block__header .omni-code-block__title").TextContent);
        Assert.Equal("shell", block.Find(".omni-code-block__language").TextContent);
        Assert.Single(block.FindAll(".omni-code-block__copy"));
    }

    [Fact]
    public void UnifiedDiff_ExpandedByDefaultOff_FoldsTheFiles_AndFileActionsTemplateIsDrawnPerFile()
    {
        const string diff = "--- a/one.txt\n+++ b/one.txt\n@@ -1 +1 @@\n-a\n+b\n--- a/two.txt\n+++ b/two.txt\n@@ -1 +1 @@\n-c\n+d\n";
        var folded = Render<OmniUnifiedDiff>(parameters => parameters
            .Add(component => component.Diff, diff)
            .Add(component => component.ExpandedByDefault, false)
            .Add(component => component.FileActionsTemplate, file => builder => builder.AddMarkupContent(0, "<i class=\"probe-action\"></i>")));

        Assert.All(folded.FindAll(".omni-unified-diff__toggle"), toggle => Assert.Equal("false", toggle.GetAttribute("aria-expanded")));
        Assert.Equal(2, folded.FindAll(".omni-unified-diff__file-actions .probe-action").Count);

        var open = Render<OmniUnifiedDiff>(parameters => parameters.Add(component => component.Diff, diff));
        Assert.All(open.FindAll(".omni-unified-diff__toggle"), toggle => Assert.Equal("true", toggle.GetAttribute("aria-expanded")));
    }

    // ---- the two choice lists share one surface ----

    [Fact]
    public void CheckBoxList_Error_MarksTheGroupInvalid_DescribesIt_AndDimsDisabledItems()
    {
        IReadOnlyList<string> value = [];
        var list = Render<OmniCheckBoxList<string>>(parameters => parameters
            .Add(component => component.Id, "tags")
            .Add(component => component.Options, Choices)
            .Add(component => component.AriaDescribedBy, "tags-help")
            .Add(component => component.Error, "Choisissez au moins une option.")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var fieldset = list.Find("fieldset");
        Assert.Equal("true", fieldset.GetAttribute("aria-invalid"));
        Assert.Equal("tags-help tags-error", fieldset.GetAttribute("aria-describedby"));
        Assert.Contains("omni-choice-list--invalid", fieldset.ClassList);
        Assert.Equal("Choisissez au moins une option.", list.Find("#tags-error").TextContent.Trim());
        Assert.Contains("omni-choice-list__item--disabled", list.FindAll("label")[1].ClassList);
        Assert.DoesNotContain("omni-choice-list__item--disabled", list.FindAll("label")[0].ClassList);
    }

    [Fact]
    public void RadioButtonList_AriaDescribedBy_IsAParameter_LikeTheCheckBoxList()
    {
        var value = string.Empty;
        var list = Render<OmniRadioButtonList<string>>(parameters => parameters
            .Add(component => component.Id, "plan")
            .Add(component => component.Options, Choices)
            .Add(component => component.AriaDescribedBy, "plan-help")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal("plan-help", list.Find("fieldset").GetAttribute("aria-describedby"));
        Assert.Null(list.Find("fieldset").GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task RadioButtonList_ReadsTheInvalidStateFromTheEditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        var list = Render<OmniRadioButtonList<string>>(parameters => parameters
            .AddCascadingValue(context)
            .Add(component => component.Options, Choices)
            .Add(component => component.Value, model.Choice)
            .Add(component => component.ValueExpression, () => model.Choice));
        Assert.Null(list.Find("fieldset").GetAttribute("aria-invalid"));

        await Invalidate(list, context, new FieldIdentifier(model, nameof(Model.Choice)));

        list.WaitForAssertion(() =>
        {
            Assert.Equal("true", list.Find("fieldset").GetAttribute("aria-invalid"));
            Assert.Contains("omni-choice-list--invalid", list.Find("fieldset").ClassList);
        });
    }

    [Fact]
    public async Task CheckBoxList_ReadsTheInvalidStateFromTheEditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        var list = Render<OmniCheckBoxList<string>>(parameters => parameters
            .AddCascadingValue(context)
            .Add(component => component.Options, Choices)
            .Add(component => component.Value, model.Many)
            .Add(component => component.ValueExpression, () => model.Many));

        await Invalidate(list, context, new FieldIdentifier(model, nameof(Model.Many)));

        list.WaitForAssertion(() => Assert.Equal("true", list.Find("fieldset").GetAttribute("aria-invalid")));
    }

    [Fact]
    public async Task Slider_ReadsTheInvalidStateFromTheEditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        var slider = Render<OmniSlider>(parameters => parameters
            .AddCascadingValue(context)
            .Add(component => component.Value, model.Amount)
            .Add(component => component.ValueExpression, () => model.Amount));
        Assert.Null(slider.Find("input").GetAttribute("aria-invalid"));

        await Invalidate(slider, context, new FieldIdentifier(model, nameof(Model.Amount)));

        slider.WaitForAssertion(() => Assert.Equal("true", slider.Find("input").GetAttribute("aria-invalid")));
    }

    [Fact]
    public async Task Rating_ReadsTheInvalidStateFromTheEditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        var rating = Render<OmniRating>(parameters => parameters
            .AddCascadingValue(context)
            .Add(component => component.Value, model.Stars)
            .Add(component => component.ValueExpression, () => model.Stars));
        Assert.Null(rating.Find("[role=radiogroup]").GetAttribute("aria-invalid"));

        await Invalidate(rating, context, new FieldIdentifier(model, nameof(Model.Stars)));

        rating.WaitForAssertion(() => Assert.Equal("true", rating.Find("[role=radiogroup]").GetAttribute("aria-invalid")));
    }

    // ---- resources ----

    [Fact]
    public void EveryCharacterCategoryOfTheHtmlEditor_HasItsNameInBothResourceFiles()
    {
        var resources = Path.Combine(Root, "src", "OmniEurope.Blazor", "Resources");
        foreach (var file in new[] { "AppStrings.resx", "AppStrings.en.resx" })
        {
            var keys = XDocument.Load(Path.Combine(resources, file)).Root!.Elements("data")
                .Select(element => (string)element.Attribute("name")!)
                .ToHashSet(StringComparer.Ordinal);
            Assert.All(HtmlEditorCharacters.Categories, category =>
                Assert.True(keys.Contains("HtmlEditorCharCategory" + category), $"{file}: HtmlEditorCharCategory{category} is missing."));
        }
    }

    private static Task Invalidate<TComponent>(IRenderedComponent<TComponent> component, EditContext context, FieldIdentifier field)
        where TComponent : IComponent
    {
        var messages = new ValidationMessageStore(context);
        return component.InvokeAsync(() =>
        {
            messages.Add(field, "Invalide");
            context.NotifyValidationStateChanged();
        });
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("OmniEurope.Blazor.slnx not found above the test output.");
        }
    }
}
