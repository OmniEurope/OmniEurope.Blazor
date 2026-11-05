using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniDocumentEditor"/> in bUnit. The text a reader types is read back by <c>omni-structured-document.js</c> (its own
/// node tests and the showcase probe cover the events); here the text the script reports reaches .NET through the internal
/// handler its bridge calls. The fields are driven through the real inputs of the popover, and the drawing through the real
/// <see cref="OmniInkCanvas"/> of the dialog with its export answered by the mocked module.
/// </summary>
public sealed class DocumentEditorTests : OmniBunitContext
{
    private static readonly byte[] Png = [137, 80, 78, 71, 1, 2, 3];
    private static readonly CultureInfo French = new("fr-FR");

    private readonly BunitJSModuleInterop _script;
    private readonly BunitJSModuleInterop _ink;
    private readonly List<OmniDocumentChangeSet> _raised = [];

    public DocumentEditorTests()
    {
        _script = JSInterop.SetupModule(OmniModules.StructuredDocument);
        _script.Setup<bool>("attach", _ => true).SetResult(true);
        _ink = JSInterop.SetupModule(OmniModules.InkCanvas);
        _ink.Setup<byte[]>("exportPng", _ => true).SetResult(Png);
    }

    private static OmniInkStroke Stroke(string color = "#112233", double width = 3) => new(color, width, [new OmniInkPoint(0.1, 0.2, null), new OmniInkPoint(0.5, 0.6, 0.4)]);

    private static OmniDocument Sample() => new(
    [
        new OmniDocumentBlock("b1",
        [
            new OmniDocumentRun("r1", "Order for ", "bold"),
            new OmniDocumentField("f-customer", "customer", "Customer", OmniDocumentFieldKind.Text, "Acme"),
            new OmniDocumentRun("r2", ", on "),
            new OmniDocumentField("f-date", "delivery", "Delivery", OmniDocumentFieldKind.Date, "2026-11-04")
        ]),
        new OmniDocumentBlock("b2",
        [
            new OmniDocumentField("f-priority", "priority", "Priority", OmniDocumentFieldKind.List, "Normal", ["Low", "Normal", "High"]),
            new OmniDocumentField("f-terms", "terms", "Terms", OmniDocumentFieldKind.List, "Net 30", ["Net 30", "Prepaid"], AllowFreeText: true),
            new OmniDocumentField("f-amount", "amount", "Amount", OmniDocumentFieldKind.Number, "12.50"),
            new OmniDocumentField("f-note", "note", "Note", OmniDocumentFieldKind.Text)
        ]),
        new OmniDocumentBlock("b3", [new OmniDocumentRun("r3", string.Empty)])
    ]);

    private IRenderedComponent<OmniDocumentEditor> RenderEditor(
        OmniDocument? document = null,
        Action<ComponentParameterCollectionBuilder<OmniDocumentEditor>>? configure = null)
        => Render<OmniDocumentEditor>(parameters =>
        {
            parameters.Add(editor => editor.Document, document ?? Sample());
            parameters.Add(editor => editor.OnChange, EventCallback.Factory.Create<OmniDocumentChangeSet>(this, set => _raised.Add(set)));
            configure?.Invoke(parameters);
        });

    private static Task Type(IRenderedComponent<OmniDocumentEditor> editor, string runId, string text)
        => editor.InvokeAsync(() => editor.Instance.HandleRunTextAsync(runId, text));

    private static IElement Chip(IRenderedComponent<OmniDocumentEditor> editor, string label)
        => editor.FindAll(".omni-popover > button").Single(button => button.QuerySelector(".omni-document-editor__chip-label")!.TextContent == label);

    private static IElement OpenChip(IRenderedComponent<OmniDocumentEditor> editor, string label)
    {
        Chip(editor, label).Click();
        return editor.Find(".omni-popover__panel");
    }

    private static string ChipText(IRenderedComponent<OmniDocumentEditor> editor, string label)
        => Chip(editor, label).QuerySelector(".omni-document-editor__chip-value")!.TextContent;

    // ----- Markup ------------------------------------------------------------------------------------

    [Fact]
    public void Editor_IsANamedRegion_OfNamedParagraphs_WithNoInlineStyle()
    {
        var editor = RenderEditor();

        var root = editor.Find("div.omni-document-editor");
        Assert.Equal("region", root.GetAttribute("role"));
        Assert.Equal("Document", root.GetAttribute("aria-label"));
        Assert.StartsWith("omni-document-editor-", root.Id, StringComparison.Ordinal);
        var blocks = editor.FindAll(".omni-document-editor__block");
        Assert.Equal(["Paragraphe 1 sur 3", "Paragraphe 2 sur 3", "Paragraphe 3 sur 3"], blocks.Select(block => block.GetAttribute("aria-label")));
        Assert.All(blocks, block => Assert.Equal("group", block.GetAttribute("role")));
        Assert.DoesNotContain("style=", editor.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Label_Id_Class_AndAttributes_GoOnTheRoot()
    {
        var editor = RenderEditor(configure: parameters => parameters
            .Add(component => component.Label, "Service order")
            .Add(component => component.Id, "order")
            .Add(component => component.Class, "mine")
            .AddUnmatched("data-probe", "yes"));

        var root = editor.Find("div.omni-document-editor");
        Assert.Equal("Service order", root.GetAttribute("aria-label"));
        Assert.Equal("order", root.Id);
        Assert.Contains("mine", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("yes", root.GetAttribute("data-probe"));
    }

    [Fact]
    public void Runs_AreEditablePlainTextSpans_NamedAndKeyedByTheirId()
    {
        var editor = RenderEditor();

        var runs = editor.FindAll("span[data-omni-run]");
        Assert.Equal(["r1", "r2", "r3"], runs.Select(run => run.GetAttribute("data-omni-run")));
        Assert.Equal(["Order for ", ", on ", ""], runs.Select(run => run.TextContent));
        Assert.All(runs, run =>
        {
            Assert.Equal("plaintext-only", run.GetAttribute("contenteditable"));
            Assert.Equal("textbox", run.GetAttribute("role"));
            Assert.Equal("Texte modifiable", run.GetAttribute("aria-label"));
        });
    }

    [Fact]
    public void ABrowserWithoutPlainTextOnly_GetsPlainEditableSpans()
    {
        _script.Setup<bool>("attach", _ => true).SetResult(false);

        var editor = RenderEditor();

        Assert.All(editor.FindAll("span[data-omni-run]"), run => Assert.Equal("true", run.GetAttribute("contenteditable")));
    }

    [Fact]
    public void FirstRender_AttachesTheScriptOnce_WithTheRootAndABridge()
    {
        var editor = RenderEditor();
        editor.Render();

        var call = Assert.Single(_script.Invocations["attach"]);
        Assert.Equal(2, call.Arguments.Count);
        Assert.IsType<DotNetObjectReference<StructuredDocumentInteropBridge>>(call.Arguments[1]);
    }

    [Fact]
    public async Task TheBridge_ForwardsTheTextTheScriptReads()
    {
        var calls = new List<string>();
        var bridge = new StructuredDocumentInteropBridge((id, text) => { calls.Add($"{id}={text}"); return Task.CompletedTask; });

        await bridge.OnRunText("r1", "hello");

        Assert.Equal(["r1=hello"], calls);
    }

    // ----- Runs --------------------------------------------------------------------------------------

    [Fact]
    public async Task ATypedRun_IsRaisedInTheChangeSet_AndMarkedAsChanged()
    {
        var editor = RenderEditor();

        await Type(editor, "r1", "Order from ");

        var change = Assert.Single(_raised);
        Assert.Equal([new OmniRunTextChange("r1", "Order from ")], change.Runs);
        Assert.Empty(change.Fields);
        Assert.Empty(change.Drawings);
        Assert.Contains("omni-document-editor__run--changed", editor.Find("[data-omni-run=r1]").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("--changed", editor.Find("[data-omni-run=r2]").ClassName, StringComparison.Ordinal);
        Assert.Equal("Order for ", editor.Find("[data-omni-run=r1]").TextContent);
    }

    [Fact]
    public async Task ARunPutBackToItsOriginalText_LeavesTheChangeSet_ButIsStillRaised()
    {
        var editor = RenderEditor();
        await Type(editor, "r1", "Order from ");

        await Type(editor, "r1", "Order for ");

        Assert.Equal(2, _raised.Count);
        Assert.True(_raised[1].IsEmpty);
        Assert.DoesNotContain("--changed", editor.Find("[data-omni-run=r1]").ClassName, StringComparison.Ordinal);
        Assert.True(editor.Instance.GetChangeSet().IsEmpty);
    }

    [Fact]
    public async Task EditingARunAgain_ReplacesItsText_AndTheSameTextIsNotRaisedTwice()
    {
        var editor = RenderEditor();
        await Type(editor, "r1", "A");
        await Type(editor, "r1", "AB");
        await Type(editor, "r1", "AB");
        await Type(editor, "r2", ", on "); // the original text: nothing to say

        Assert.Equal(2, _raised.Count);
        Assert.Equal([new OmniRunTextChange("r1", "AB")], _raised[1].Runs);
    }

    [Fact]
    public async Task ALineBreak_BecomesASpace_SoARunStaysOnOneLine()
    {
        var editor = RenderEditor();

        await Type(editor, "r1", "first\r\nsecond\n  third \r fourth");

        Assert.Equal("first second third fourth", Assert.Single(Assert.Single(_raised).Runs).Text);
    }

    [Fact]
    public async Task APlainTextRun_IsNeverReadAsMarkup()
    {
        var editor = RenderEditor();

        await Type(editor, "r1", "<b onclick=\"x()\">bold</b> & more");

        Assert.Equal("<b onclick=\"x()\">bold</b> & more", Assert.Single(Assert.Single(_raised).Runs).Text);
        Assert.Empty(editor.FindAll("b"));
        Assert.Equal("Order for ", editor.Find("[data-omni-run=r1]").TextContent);
    }

    [Fact]
    public async Task TextForAnUnknownRun_ForAReadOnlyEditor_OrOnceDisposed_IsIgnored()
    {
        var editor = RenderEditor();
        await Type(editor, "nope", "x");
        await Type(editor, "f-customer", "x");

        editor.Render(parameters => parameters.Add(component => component.ReadOnly, true));
        await Type(editor, "r1", "x");
        editor.Render(parameters => parameters.Add(component => component.ReadOnly, false));
        await editor.Instance.DisposeAsync();
        await Type(editor, "r1", "x");

        Assert.Empty(_raised);
        Assert.True(editor.Instance.GetChangeSet().IsEmpty);
    }

    [Fact]
    public async Task StyleClasses_GivesTheClassOfARun_FromItsOpaqueKey_AndOnlyForARunThatHasOne()
    {
        var asked = new List<string>();
        var editor = RenderEditor(configure: parameters => parameters.Add(component => component.StyleClasses, key =>
        {
            asked.Add(key);
            return key == "bold" ? "host-bold" : null;
        }));

        Assert.Contains("host-bold", editor.Find("[data-omni-run=r1]").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("host-", editor.Find("[data-omni-run=r2]").ClassName, StringComparison.Ordinal);
        Assert.Equal(["bold"], asked.Distinct());

        await Type(editor, "r1", "x");
        Assert.Contains("host-bold", editor.Find("[data-omni-run=r1]").ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-document-editor__run--changed", editor.Find("[data-omni-run=r1]").ClassName, StringComparison.Ordinal);
        Assert.Empty(_raised[0].Fields);
    }

    [Fact]
    public void WithoutStyleClasses_AStyleKeyAddsNoClass()
    {
        var editor = RenderEditor();

        Assert.Equal("omni-document-editor__run", editor.Find("[data-omni-run=r1]").ClassName);
    }

    // ----- Fields: chips -----------------------------------------------------------------------------

    [Fact]
    public void Fields_AreChipsWithTheirLabelAndValue_OpeningAPopover()
    {
        var editor = RenderEditor();

        Assert.Equal(["Customer", "Delivery", "Priority", "Terms", "Amount", "Note"],
            editor.FindAll(".omni-popover > button .omni-document-editor__chip-label").Select(label => label.TextContent));
        Assert.Equal("Acme", ChipText(editor, "Customer"));
        Assert.Equal("Normal", ChipText(editor, "Priority"));
        Assert.Empty(editor.FindAll(".omni-popover__panel"));
        Assert.Equal("false", Chip(editor, "Customer").GetAttribute("aria-expanded"));

        OpenChip(editor, "Customer");
        Assert.Equal("true", Chip(editor, "Customer").GetAttribute("aria-expanded"));
        Assert.Equal("Customer", editor.Find(".omni-popover__panel").GetAttribute("aria-label"));
    }

    [Fact]
    public void AFieldWithoutAValue_ShowsALocalizedPlaceholder_InAMutedStyle()
    {
        var editor = RenderEditor();

        var value = Chip(editor, "Note").QuerySelector(".omni-document-editor__chip-value")!;
        Assert.Equal("à renseigner", value.TextContent);
        Assert.Contains("omni-document-editor__chip-value--empty", value.ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("--empty", Chip(editor, "Customer").QuerySelector(".omni-document-editor__chip-value")!.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void TheChipValueOfADateAndANumber_IsWrittenInTheCultureGiven()
    {
        var french = RenderEditor(configure: parameters => parameters.Add(component => component.Culture, French));
        var american = RenderEditor(configure: parameters => parameters.Add(component => component.Culture, new CultureInfo("en-US")));

        Assert.Equal("04/11/2026", ChipText(french, "Delivery"));
        Assert.Equal("12,50", ChipText(french, "Amount"));
        Assert.Equal("11/4/2026", ChipText(american, "Delivery"));
        Assert.Equal("12.50", ChipText(american, "Amount"));
    }

    [Fact]
    public void WithoutACulture_TheChipUsesTheCultureOfThePage()
    {
        var editor = RenderEditor();

        Assert.Equal(DateOnly.Parse("2026-11-04", CultureInfo.InvariantCulture).ToString("d", CultureInfo.CurrentCulture), ChipText(editor, "Delivery"));
    }

    [Fact]
    public async Task TheDatePickerAChipOpens_FollowsTheCultureOfTheEditor_NotThePage()
    {
        var editor = RenderEditor(configure: parameters => parameters.Add(component => component.Culture, new CultureInfo("de-DE")));
        var panel = OpenChip(editor, "Delivery");
        var input = panel.QuerySelector("input")!;

        Assert.Equal("04.11.2026", input.GetAttribute("value"));
        Assert.Equal("04.11.2026", ChipText(editor, "Delivery"));

        await input.ChangeAsync(new ChangeEventArgs { Value = "01.12.2026" });

        Assert.Equal([new OmniFieldValueChange("f-date", "delivery", "2026-12-01")], Assert.Single(_raised).Fields);
        panel.QuerySelector("button.omni-date__toggle")!.Click();
        Assert.Equal("Dezember 2026", editor.Find(".omni-calendar__title").TextContent);
    }

    [Fact]
    public async Task TheNumericInputAChipOpens_ReadsATextInTheCultureOfTheEditor_NotThePage()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var editor = RenderEditor(configure: parameters => parameters.Add(component => component.Culture, new CultureInfo("de-DE")));
            var input = OpenChip(editor, "Amount").QuerySelector("input[type=number]")!;

            await input.ChangeAsync(new ChangeEventArgs { Value = "1,5" });

            // de-DE reads the comma as a decimal separator; the page (en-US) would read 15.
            Assert.Equal([new OmniFieldValueChange("f-amount", "amount", "1.5")], Assert.Single(_raised).Fields);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    // ----- Fields: editing ---------------------------------------------------------------------------

    [Fact]
    public async Task ATextField_IsEditedInATextBox_AsItIsTyped()
    {
        var editor = RenderEditor();
        var panel = OpenChip(editor, "Customer");
        var input = panel.QuerySelector("input[type=text]")!;
        Assert.Equal("Acme", input.GetAttribute("value"));
        Assert.Equal("Customer", input.GetAttribute("aria-label"));

        await input.InputAsync(new ChangeEventArgs { Value = "Globex" });

        var change = Assert.Single(_raised);
        Assert.Equal([new OmniFieldValueChange("f-customer", "customer", "Globex")], change.Fields);
        Assert.Equal("Globex", ChipText(editor, "Customer"));
        Assert.Contains("omni-document-editor__chip--changed", Chip(editor, "Customer").ClassName, StringComparison.Ordinal);
        Assert.Single(editor.FindAll(".omni-popover__panel"));

        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = "Acme" });

        Assert.True(_raised[1].IsEmpty);
        Assert.DoesNotContain("--changed", Chip(editor, "Customer").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnEmptyTextPutInAnEmptyField_IsNoChange_AndRaisesNothing()
    {
        var editor = RenderEditor();
        OpenChip(editor, "Note");

        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = string.Empty });

        Assert.Empty(_raised);
        Assert.DoesNotContain("--changed", Chip(editor, "Note").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnEmptiedTextField_IsRaisedAsANullValue_AndAFilledOneWasEmpty()
    {
        var editor = RenderEditor();
        OpenChip(editor, "Customer");
        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = string.Empty });
        Assert.Equal(new OmniFieldValueChange("f-customer", "customer", null), Assert.Single(_raised[0].Fields));
        Assert.Equal("à renseigner", ChipText(editor, "Customer"));

        editor = RenderEditor();
        OpenChip(editor, "Note");
        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = "later" });
        Assert.Equal(new OmniFieldValueChange("f-note", "note", "later"), Assert.Single(_raised[1].Fields));
    }

    [Fact]
    public async Task ADateField_IsEditedInTheDatePicker_AndWrittenIso()
    {
        var editor = RenderEditor(configure: parameters => parameters.Add(component => component.Culture, French));
        var panel = OpenChip(editor, "Delivery");
        var input = panel.QuerySelector("input")!;
        Assert.Equal("04/11/2026", input.GetAttribute("value"));

        await input.ChangeAsync(new ChangeEventArgs { Value = "2026-12-01" });

        Assert.Equal([new OmniFieldValueChange("f-date", "delivery", "2026-12-01")], Assert.Single(_raised).Fields);
        Assert.Equal("01/12/2026", ChipText(editor, "Delivery"));

        await editor.Find(".omni-popover__panel input").ChangeAsync(new ChangeEventArgs { Value = "2026-11-04" });
        Assert.True(_raised[1].IsEmpty);
    }

    [Fact]
    public async Task ADateField_ClearedInThePicker_IsRaisedAsNull()
    {
        var editor = RenderEditor();
        OpenChip(editor, "Delivery");

        await editor.Find(".omni-popover__panel input").ChangeAsync(new ChangeEventArgs { Value = string.Empty });

        Assert.Equal(new OmniFieldValueChange("f-date", "delivery", null), Assert.Single(Assert.Single(_raised).Fields));
        Assert.Equal("à renseigner", ChipText(editor, "Delivery"));
    }

    [Fact]
    public async Task AListField_IsChosenInADropDownOverItsItems()
    {
        var editor = RenderEditor();
        var panel = OpenChip(editor, "Priority");
        var select = panel.QuerySelector("select")!;
        Assert.Equal("Priority", select.GetAttribute("aria-label"));
        Assert.Equal(["Sélectionner", "Low", "Normal", "High"], select.QuerySelectorAll("option").Select(option => option.TextContent.Trim()));

        await select.ChangeAsync(new ChangeEventArgs { Value = select.QuerySelectorAll("option").Single(option => option.TextContent == "High").GetAttribute("value") });

        Assert.Equal([new OmniFieldValueChange("f-priority", "priority", "High")], Assert.Single(_raised).Fields);
        Assert.Equal("High", ChipText(editor, "Priority"));

        await editor.Find(".omni-popover__panel select").ChangeAsync(new ChangeEventArgs { Value = string.Empty });
        Assert.Equal(new OmniFieldValueChange("f-priority", "priority", null), Assert.Single(_raised[1].Fields));
    }

    [Fact]
    public async Task AListThatAllowsFreeText_IsAnAutocompleteOverItsItems_ThatTakesAnyText()
    {
        var editor = RenderEditor();
        var panel = OpenChip(editor, "Terms");
        var input = panel.QuerySelector("input[role=combobox]")!;
        Assert.Equal("Terms", input.GetAttribute("aria-label"));
        Assert.Equal("Net 30", input.GetAttribute("value"));

        await input.InputAsync(new ChangeEventArgs { Value = "pre" });
        editor.WaitForAssertion(() => Assert.Equal(["Prepaid"], editor.FindAll("[role=option]").Select(option => option.TextContent.Trim())));
        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = "Net 45 days" });
        await editor.Find(".omni-popover__panel input").BlurAsync(new FocusEventArgs());

        Assert.Equal([new OmniFieldValueChange("f-terms", "terms", "Net 45 days")], Assert.Single(_raised).Fields);
        Assert.Equal("Net 45 days", ChipText(editor, "Terms"));
    }

    [Fact]
    public async Task AFreeTextListWithoutItems_OffersNothingToPick_ButTakesText()
    {
        var document = new OmniDocument([new OmniDocumentBlock("b", [new OmniDocumentField("f", "t", "Free", OmniDocumentFieldKind.List, AllowFreeText: true)])]);
        var editor = RenderEditor(document);
        OpenChip(editor, "Free");

        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = "anything" });
        editor.WaitForAssertion(() => Assert.Empty(editor.FindAll("[role=option]")));
        await editor.Find(".omni-popover__panel input").BlurAsync(new FocusEventArgs());

        Assert.Equal([new OmniFieldValueChange("f", "t", "anything")], Assert.Single(_raised).Fields);
    }

    [Fact]
    public async Task ANumberField_IsEditedInANumericInput_InTheInvariantCulture()
    {
        var editor = RenderEditor();
        var panel = OpenChip(editor, "Amount");
        var input = panel.QuerySelector("input[type=number]")!;
        Assert.Equal("12.50", input.GetAttribute("value"));

        await input.ChangeAsync(new ChangeEventArgs { Value = "1234.5" });

        Assert.Equal([new OmniFieldValueChange("f-amount", "amount", "1234.5")], Assert.Single(_raised).Fields);

        await editor.Find(".omni-popover__panel input").ChangeAsync(new ChangeEventArgs { Value = "12.5" });
        Assert.True(_raised[1].IsEmpty, "12.5 is the number 12.50 was: it is not a change");

        await editor.Find(".omni-popover__panel input").ChangeAsync(new ChangeEventArgs { Value = string.Empty });
        Assert.Equal(new OmniFieldValueChange("f-amount", "amount", null), Assert.Single(_raised[2].Fields));
    }

    [Fact]
    public async Task TheChangeSet_ListsRunsFieldsAndDrawings_InTheOrderOfTheDocument_WithTheirTags()
    {
        var editor = RenderEditor();
        OpenChip(editor, "Delivery");
        await editor.Find(".omni-popover__panel input").ChangeAsync(new ChangeEventArgs { Value = "2026-12-01" });
        await Type(editor, "r3", "tail");
        await Type(editor, "r1", "head");
        OpenChip(editor, "Customer");
        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = "Globex" });

        var set = editor.Instance.GetChangeSet();

        Assert.Equal(["r1", "r3"], set.Runs.Select(run => run.RunId));
        Assert.Equal([("f-customer", "customer"), ("f-date", "delivery")], set.Fields.Select(field => (field.FieldId, field.Tag)));
        Assert.NotSame(set, editor.Instance.GetChangeSet());
    }

    // ----- Read only ---------------------------------------------------------------------------------

    [Fact]
    public void ReadOnly_DisablesAllEditing()
    {
        var editor = RenderEditor(configure: parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.All(editor.FindAll("span[data-omni-run]"), run =>
        {
            Assert.False(run.HasAttribute("contenteditable"));
            Assert.False(run.HasAttribute("role"));
            Assert.False(run.HasAttribute("aria-label"));
        });
        Assert.Empty(editor.FindAll(".omni-popover"));
        Assert.Empty(editor.FindAll("button"));
        Assert.Contains("omni-document-editor--readonly", editor.Find("div.omni-document-editor").ClassName, StringComparison.Ordinal);
        var chips = editor.FindAll(".omni-document-editor__chip--readonly");
        Assert.Equal(6, chips.Count);
        Assert.Contains("Customer", chips[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Acme", chips[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("--empty", chips[5].QuerySelector(".omni-document-editor__chip-value")!.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadOnly_StillShowsTheDrawingsOfTheDocument()
    {
        var document = new OmniDocument([new OmniDocumentBlock("b", [], new OmniDocumentDrawing([Stroke()], 100, 50))]);

        var editor = RenderEditor(document, parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.Single(editor.FindAll("svg.omni-document-editor__sheet"));
        Assert.Empty(editor.FindAll("button"));
    }

    // ----- Drawings ----------------------------------------------------------------------------------

    private static IRenderedComponent<OmniInkCanvas> Canvas(IRenderedComponent<OmniDocumentEditor> editor) => editor.FindComponent<OmniInkCanvas>();

    private static Task Draw(IRenderedComponent<OmniDocumentEditor> editor, string color = "#112233")
        => Canvas(editor).InvokeAsync(() => Canvas(editor).Instance.HandleStrokeAsync(color, 4, [0.1, 0.2, -1, 0.5, 0.6, 0.4]));

    private static IElement DrawButton(IRenderedComponent<OmniDocumentEditor> editor, int block = 0)
        => editor.FindAll(".omni-document-editor__block")[block].QuerySelector("button.omni-document-editor__draw")!;

    [Fact]
    public void EveryParagraph_OffersToInsertADrawing_WhichOpensADialogAroundAnInkCanvas()
    {
        var editor = RenderEditor();

        Assert.Equal(3, editor.FindAll("button.omni-document-editor__draw").Count);
        Assert.Equal("Insérer un dessin", DrawButton(editor).TextContent.Trim());
        Assert.Empty(editor.FindAll("[role=dialog] canvas"));

        DrawButton(editor).Click();

        var dialog = editor.Find("[role=dialog].omni-dialog");
        Assert.Equal("Dessin", dialog.QuerySelector(".omni-dialog__title")!.TextContent);
        Assert.NotNull(dialog.QuerySelector("canvas"));
        Assert.Equal("Dessin", dialog.QuerySelector("canvas")!.GetAttribute("aria-label"));
        Assert.Equal(400d / 150d, Canvas(editor).Instance.AspectRatio);
        var footer = dialog.QuerySelectorAll(".omni-dialog__footer button").Select(button => button.TextContent.Trim()).ToArray();
        Assert.Equal(["Annuler", "Valider"], footer);
    }

    [Fact]
    public async Task ValidatingADrawing_StoresItsStrokesAndItsPng_ShowsItUnderTheParagraph_AndRaisesTheChange()
    {
        var editor = RenderEditor();
        DrawButton(editor, 2).Click();
        Assert.True(editor.Find(".omni-dialog__footer button:last-child").HasAttribute("disabled"), "nothing to validate before a stroke is drawn");
        await Draw(editor);
        Assert.False(editor.Find(".omni-dialog__footer button:last-child").HasAttribute("disabled"));

        await editor.Find(".omni-dialog__footer button:last-child").ClickAsync();

        var change = Assert.Single(Assert.Single(_raised).Drawings);
        Assert.Equal("b3", change.BlockId);
        Assert.Equal(Png, change.Png);
        Assert.Equal(400, change.Drawing.WidthPt);
        Assert.Equal(150, change.Drawing.HeightPt);
        Assert.Equal("#112233", Assert.Single(change.Drawing.Strokes).Color);
        var arguments = Assert.Single(_ink.Invocations["exportPng"]).Arguments;
        Assert.Equal(533, arguments[3]);
        Assert.Equal(200, arguments[4]);
        Assert.Empty(editor.FindAll("[role=dialog]"));
        var sheet = editor.FindAll(".omni-document-editor__block")[2].QuerySelector("svg.omni-document-editor__sheet")!;
        Assert.Equal("img", sheet.GetAttribute("role"));
        Assert.Equal("Dessin", sheet.GetAttribute("aria-label"));
        Assert.Equal("0 0 533.33 200", sheet.GetAttribute("viewBox"));
        Assert.Equal("Remplacer le dessin", DrawButton(editor, 2).TextContent.Trim());
        Assert.Equal("Insérer un dessin", DrawButton(editor, 0).TextContent.Trim());
    }

    [Fact]
    public async Task ReplacingADrawing_StartsFromItsStrokes_KeepsItsSheetSize_AndReplacesTheChange()
    {
        var document = new OmniDocument([new OmniDocumentBlock("b", [], new OmniDocumentDrawing([Stroke("#aa0000")], 300, 100))]);
        var editor = RenderEditor(document);
        Assert.Equal("Remplacer le dessin", DrawButton(editor).TextContent.Trim());

        DrawButton(editor).Click();
        Assert.Single(Canvas(editor).Instance.Value);
        Assert.Equal(3d, Canvas(editor).Instance.AspectRatio);
        await Draw(editor, "#00aa00");
        await editor.Find(".omni-dialog__footer button:last-child").ClickAsync();
        DrawButton(editor).Click();
        await Draw(editor, "#0000aa");
        await editor.Find(".omni-dialog__footer button:last-child").ClickAsync();

        var set = editor.Instance.GetChangeSet();
        var drawing = Assert.Single(set.Drawings).Drawing;
        Assert.Equal(["#aa0000", "#00aa00", "#0000aa"], drawing.Strokes.Select(stroke => stroke.Color));
        Assert.Equal((300d, 100d), (drawing.WidthPt, drawing.HeightPt));
        Assert.Equal(2, _raised.Count);
        Assert.Equal([400, 400], _ink.Invocations["exportPng"].Select(call => call.Arguments[3]));
    }

    [Fact]
    public async Task CancellingTheDialog_ChangesNothing_AndEscapeOrTheCloseButtonLeaveToo()
    {
        var editor = RenderEditor();
        DrawButton(editor).Click();
        await Draw(editor);

        await editor.Find(".omni-dialog__footer button:first-child").ClickAsync();
        Assert.Empty(editor.FindAll("[role=dialog]"));

        DrawButton(editor).Click();
        Assert.Empty(Canvas(editor).Instance.Value);
        await editor.Find("button.omni-dialog__close").ClickAsync();
        Assert.Empty(editor.FindAll("[role=dialog]"));

        Assert.Empty(_raised);
        Assert.Empty(_ink.Invocations["exportPng"]);
        Assert.True(editor.Instance.GetChangeSet().IsEmpty);
        Assert.Empty(editor.FindAll("svg.omni-document-editor__sheet"));
    }

    [Fact]
    public void AllowDrawingsOff_HidesTheButtons_ButTheDrawingsOfTheDocumentStayVisible()
    {
        var document = new OmniDocument([new OmniDocumentBlock("b", [], new OmniDocumentDrawing([Stroke()], 100, 50))]);

        var editor = RenderEditor(document, parameters => parameters.Add(component => component.AllowDrawings, false));

        Assert.Empty(editor.FindAll("button.omni-document-editor__draw"));
        Assert.Single(editor.FindAll("svg.omni-document-editor__sheet"));
    }

    [Fact]
    public void ADrawing_IsAnInlineSvg_WithOneLinePerStroke_AndADotForASinglePoint()
    {
        var strokes = new[]
        {
            new OmniInkStroke(OmniInkStroke.ThemeColor, 6, [new OmniInkPoint(0, 0, null), new OmniInkPoint(1, 1, 0.9), new OmniInkPoint(0.5, 0.25, null)]),
            new OmniInkStroke("#d93025", 12, [new OmniInkPoint(0.5, 0.5, null)]),
            new OmniInkStroke("navy", 0.01, [new OmniInkPoint(0.1, 0.1, null)])
        };
        var document = new OmniDocument([new OmniDocumentBlock("b", [], new OmniDocumentDrawing(strokes, 450, 225))]);

        var editor = RenderEditor(document);

        var svg = editor.Find("svg.omni-document-editor__sheet");
        Assert.Equal("0 0 600 300", svg.GetAttribute("viewBox"));
        Assert.Equal("600", svg.GetAttribute("width"));
        Assert.Equal("300", svg.GetAttribute("height"));
        var line = Assert.Single(svg.QuerySelectorAll("polyline"));
        Assert.Equal("0,0 600,300 300,75", line.GetAttribute("points"));
        Assert.Equal("currentColor", line.GetAttribute("stroke"));
        Assert.Equal("6", line.GetAttribute("stroke-width"));
        Assert.Equal("none", line.GetAttribute("fill"));
        var dots = svg.QuerySelectorAll("circle");
        Assert.Equal(["300", "150", "6", "#d93025"], [dots[0].GetAttribute("cx")!, dots[0].GetAttribute("cy")!, dots[0].GetAttribute("r")!, dots[0].GetAttribute("fill")!]);
        Assert.Equal("0.25", dots[1].GetAttribute("r"));
        Assert.DoesNotContain("style=", editor.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ADrawingWithoutStrokes_IsAnEmptySheet()
    {
        var document = new OmniDocument([new OmniDocumentBlock("b", [], new OmniDocumentDrawing([], 100, 100))]);

        var editor = RenderEditor(document);

        Assert.Empty(editor.Find("svg.omni-document-editor__sheet").Children);
    }

    // ----- Document and changes ----------------------------------------------------------------------

    [Fact]
    public async Task ANewDocument_StartsTheChangesOver_AndDrawsTheTextAgain_ButTheSameOneLeavesTheTypedTextAlone()
    {
        var editor = RenderEditor(Sample());
        await Type(editor, "r1", "typed");

        // The document did not change: the run is drawn with the text of the document, which Blazor never patches over what the
        // browser holds (the showcase probe types in a real browser and checks it), and the change is kept.
        editor.Render(parameters => parameters.Add(component => component.Class, "same document"));
        Assert.Equal("Order for ", editor.Find("[data-omni-run=r1]").TextContent);
        Assert.False(editor.Instance.GetChangeSet().IsEmpty);
        Assert.Contains("--changed", editor.Find("[data-omni-run=r1]").ClassName, StringComparison.Ordinal);

        editor.Render(parameters => parameters.Add(component => component.Document, Sample()));

        Assert.True(editor.Instance.GetChangeSet().IsEmpty);
        Assert.Equal("Order for ", editor.Find("[data-omni-run=r1]").TextContent);
        Assert.DoesNotContain("--changed", editor.Find("[data-omni-run=r1]").ClassName, StringComparison.Ordinal);
        Assert.Single(_raised);
    }

    [Fact]
    public async Task ANewDocument_ClosesAnOpenDrawingDialog_AndForgetsDrawings()
    {
        var editor = RenderEditor();
        DrawButton(editor).Click();
        await Draw(editor);
        await editor.Find(".omni-dialog__footer button:last-child").ClickAsync();
        DrawButton(editor, 1).Click();
        Assert.NotEmpty(editor.FindAll("[role=dialog]"));

        editor.Render(parameters => parameters.Add(component => component.Document, Sample()));

        Assert.Empty(editor.FindAll("[role=dialog]"));
        Assert.Empty(editor.FindAll("svg.omni-document-editor__sheet"));
        Assert.True(editor.Instance.GetChangeSet().IsEmpty);
    }

    [Fact]
    public async Task ResetChanges_DropsEveryChange_RedrawsTheDocument_AndRaisesNothing()
    {
        var editor = RenderEditor();
        await Type(editor, "r1", "typed");
        OpenChip(editor, "Customer");
        await editor.Find(".omni-popover__panel input").InputAsync(new ChangeEventArgs { Value = "Globex" });
        DrawButton(editor).Click();
        await Draw(editor);
        await editor.Find(".omni-dialog__footer button:last-child").ClickAsync();
        var raised = _raised.Count;

        await editor.InvokeAsync(editor.Instance.ResetChanges);

        Assert.True(editor.Instance.GetChangeSet().IsEmpty);
        Assert.Equal(raised, _raised.Count);
        Assert.Equal("Order for ", editor.Find("[data-omni-run=r1]").TextContent);
        Assert.Equal("Acme", ChipText(editor, "Customer"));
        Assert.Empty(editor.FindAll("svg.omni-document-editor__sheet"));
        Assert.Empty(editor.FindAll(".omni-popover__panel"));
    }

    [Fact]
    public void ADocumentThatBreaksTheModel_IsRefusedByTheComponent()
    {
        var broken = new OmniDocument([new OmniDocumentBlock("b", [new OmniDocumentRun("x", "a"), new OmniDocumentRun("x", "b")])]);

        var failure = Assert.Throws<ArgumentException>(() => RenderEditor(broken));

        Assert.Equal("Document", failure.ParamName);
        Assert.Contains("'x'", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADocumentThatIsMissing_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => Render<OmniDocumentEditor>(parameters => parameters.Add(editor => editor.Document, null!)));
    }

    [Fact]
    public void AnEmptyDocument_IsAnEmptyEditor()
    {
        var editor = RenderEditor(new OmniDocument([]));

        Assert.Empty(editor.FindAll(".omni-document-editor__block"));
        Assert.True(editor.Instance.GetChangeSet().IsEmpty);
    }

    // ----- Localization ------------------------------------------------------------------------------

    [Fact]
    public void Texts_AreLocalized()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        try
        {
            var editor = RenderEditor();

            Assert.Equal("Document", editor.Find("div.omni-document-editor").GetAttribute("aria-label"));
            Assert.Equal("Paragraph 2 of 3", editor.FindAll(".omni-document-editor__block")[1].GetAttribute("aria-label"));
            Assert.Equal("Editable text", editor.Find("[data-omni-run=r1]").GetAttribute("aria-label"));
            Assert.Equal("to fill in", ChipText(editor, "Note"));
            Assert.Equal("Insert a drawing", DrawButton(editor).TextContent.Trim());
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    // ----- Script lifecycle --------------------------------------------------------------------------

    [Fact]
    public async Task Dispose_DetachesAndReleasesTheScript()
    {
        var editor = RenderEditor();

        await editor.Instance.DisposeAsync();

        Assert.Single(_script.Invocations["detach"]);
    }

    [Fact]
    public async Task DisposingBeforeTheRenderHappened_DoesNothing()
    {
        await new OmniDocumentEditor().DisposeAsync();
    }

    [Fact]
    public async Task LostCircuit_WhileTheScriptIsCalled_IsIgnored()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);

        var editor = RenderEditor();
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        await editor.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
    }

    [Fact]
    public async Task EditorGoneWhileItsScriptLoads_ReleasesTheScriptOnArrival_AndAttachesNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var editor = RenderEditor();

        var disposing = editor.Instance.DisposeAsync().AsTask();
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        await disposing;

        Assert.Empty(runtime.Module.Calls);
    }
}
