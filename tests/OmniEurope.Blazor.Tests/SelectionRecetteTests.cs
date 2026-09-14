using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the OE demo recette asked of the menus and selectors: closing on a press outside, disabled
/// options that read as such, a select bar that scrolls under chevrons, the searched letters marked
/// in the suggestions, and a file field that lists, adds and removes files.
/// </summary>
public sealed class SelectionRecetteTests : OmniBunitContext
{
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";

    [Fact]
    public void ProfileMenu_AsksTheScriptToCloseOnAnOutsidePressAndOnAChosenItem()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Summary, (RenderFragment)(builder => builder.AddContent(0, "Camille")))
            .AddChildContent<OmniProfileMenuItem>(item => item.AddChildContent("Profil")));

        menu.WaitForAssertion(() => Assert.Single(module.Invocations["configureDisclosure"]));
        var arguments = module.Invocations["configureDisclosure"][0].Arguments;
        Assert.False(string.IsNullOrEmpty(((ElementReference)arguments[0]!).Id));
        Assert.Equal(true, arguments[1]);
        Assert.Equal(true, arguments[2]);

        menu.Render(parameters => parameters.Add(component => component.CloseOnOutsideClick, false));

        menu.WaitForAssertion(() => Assert.Equal(2, module.Invocations["configureDisclosure"].Count));
        Assert.Equal(false, module.Invocations["configureDisclosure"][1].Arguments[1]);
    }

    [Fact]
    public void MultiSelectCompact_ClosesOnAnOutsidePressButNotOnEachTick_AndTheListNeverAsks()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        IReadOnlyList<string> value = [];
        var options = new OmniOption<string>[] { new("a", "Alpha"), new("b", "Beta", Disabled: true) };
        var compact = Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, options)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.Compact));

        compact.WaitForAssertion(() => Assert.Single(module.Invocations["configureDisclosure"]));
        Assert.Equal(true, module.Invocations["configureDisclosure"][0].Arguments[1]);
        Assert.Equal(false, module.Invocations["configureDisclosure"][0].Arguments[2]);

        Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, options)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Single(module.Invocations["configureDisclosure"]);
    }

    [Fact]
    public void MultiSelectCompact_Disabled_KeepsItsSummaryShutAndOutOfTheTabOrder()
    {
        IReadOnlyList<string> value = ["a"];
        var compact = Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "Alpha")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.Compact)
            .Add(component => component.Disabled, true));

        var summary = compact.Find("summary");
        Assert.Equal("true", summary.GetAttribute("aria-disabled"));
        Assert.Equal("-1", summary.GetAttribute("tabindex"));

        var enabled = Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "Alpha")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.Compact));
        Assert.Null(enabled.Find("summary").GetAttribute("aria-disabled"));
        Assert.Null(enabled.Find("summary").GetAttribute("tabindex"));
    }

    [Fact]
    public void SelectBar_ScrollsUnderChevronsAndSaysWhenItIsDisabled()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var value = "b";
        var options = new OmniOption<string>[] { new("a", "Alpha"), new("b", "Beta"), new("c", "Carte", Disabled: true) };
        var bar = Render<OmniSelectBar<string>>(parameters => parameters
            .Add(component => component.Id, "bar")
            .Add(component => component.Options, options)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, next => value = next));

        bar.WaitForAssertion(() => Assert.Single(module.Invocations["configureSelectBarOverflow"]));
        Assert.Equal(3, bar.FindAll(".omni-select-bar__viewport > .omni-select-bar__item").Count);
        Assert.All(bar.FindAll(".omni-select-bar__scroll"), chevron =>
        {
            Assert.True(chevron.HasAttribute("hidden"));
            Assert.Equal("true", chevron.GetAttribute("aria-hidden"));
            Assert.Equal("-1", chevron.GetAttribute("tabindex"));
        });
        var items = bar.FindAll(".omni-select-bar__item");
        Assert.True(items[2].HasAttribute("disabled"));
        items[2].Click();
        Assert.Equal("b", value);
        Assert.DoesNotContain("omni-select-bar--disabled", bar.Find("#bar").ClassName, StringComparison.Ordinal);

        bar.Render(parameters => parameters.Add(component => component.Disabled, true));
        Assert.Contains("omni-select-bar--disabled", bar.Find("#bar").ClassName, StringComparison.Ordinal);
        Assert.Equal("true", bar.Find("#bar").GetAttribute("aria-disabled"));
        Assert.All(bar.FindAll(".omni-select-bar__item"), item => Assert.True(item.HasAttribute("disabled")));
    }

    [Fact]
    public void RadioButtonList_MarksADisabledOptionOnItsLabel()
    {
        var value = "a";
        var list = Render<OmniRadioButtonList<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "Standard"), new OmniOption<string>("b", "Retrait en agence", Disabled: true)])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, next => value = next));

        var labels = list.FindAll("label.omni-choice-list__item");
        Assert.DoesNotContain("omni-choice-list__item--disabled", labels[0].ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-choice-list__item--disabled", labels[1].ClassName, StringComparison.Ordinal);
        Assert.True(list.FindAll("input[type=radio]")[1].HasAttribute("disabled"));
    }

    [Fact]
    public async Task Autocomplete_MarksTheSearchedLettersIgnoringCaseAndAccents()
    {
        var value = string.Empty;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.DebounceMilliseconds, 0)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
                [new("liege", "Liège (Belgique)"), new("lille", "Lille, Lille-Flandres")])));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "LIEGE" });
        var marks = autocomplete.FindAll("mark.omni-autocomplete__match");
        Assert.Single(marks);
        Assert.Equal("Liège", marks[0].TextContent);
        Assert.Equal("Liège (Belgique)", autocomplete.FindAll(".omni-autocomplete__option")[0].TextContent);

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "lille" });
        Assert.Equal(["Lille", "Lille"], autocomplete.FindAll("mark.omni-autocomplete__match").Select(mark => mark.TextContent));

        autocomplete.Render(parameters => parameters.Add(component => component.HighlightMatches, false));
        Assert.Empty(autocomplete.FindAll("mark"));
        Assert.Equal("Lille, Lille-Flandres", autocomplete.FindAll(".omni-autocomplete__option")[1].TextContent);
    }

    [Theory]
    [InlineData("Bruxelles", "", new[] { "Bruxelles:False" })]
    [InlineData("Bruxelles", "xel", new[] { "Bru:False", "xel:True", "les:False" })]
    [InlineData("Naïve naïve", "NAIVE", new[] { "Naïve:True", " :False", "naïve:True" })]
    [InlineData("Gand", "zzz", new[] { "Gand:False" })]
    public void TextMatch_SplitsAroundEveryOccurrence(string text, string query, string[] expected)
    {
        var segments = OmniTextMatch.Split(text, query).Select(segment => $"{segment.Text}:{segment.Matched}");

        Assert.Equal(expected, segments);
    }

    [Fact]
    public void Upload_ZoneStatesItsLimitsAndPaintsADrag()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.InputId, "files")
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFiles, 3)
            .Add(component => component.MaximumFileSize, 1024 * 1024)
            .Add(component => component.AllowedContentTypes, ["image/png", "application/pdf"]));

        Assert.Equal("files-hint", upload.Find("input[type=file]").GetAttribute("aria-describedby"));
        var hint = upload.Find("#files-hint").TextContent;
        Assert.Contains("PNG, PDF", hint, StringComparison.Ordinal);
        Assert.Contains("1 Mo au plus par fichier", hint, StringComparison.Ordinal);
        Assert.Contains("3 fichiers au plus", hint, StringComparison.Ordinal);
        Assert.Contains("Déposez des fichiers ici ou", upload.Find(".omni-upload__prompt").TextContent, StringComparison.Ordinal);

        upload.Find(".omni-upload__zone").TriggerEvent("ondragenter", new DragEventArgs());
        Assert.Contains("omni-upload--dragging", upload.Find(".omni-upload").ClassName, StringComparison.Ordinal);
        upload.Find(".omni-upload__zone").TriggerEvent("ondragleave", new DragEventArgs());
        Assert.DoesNotContain("omni-upload--dragging", upload.Find(".omni-upload").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", upload.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Upload_BoundFiles_AreListedAndRemovedOneByOne()
    {
        IReadOnlyList<OmniUploadFile> files =
        [
            new("rapport-2026.pdf", 1_258_291, "application/pdf"),
            new("photo.jpg", 48_000, "image/jpeg")
        ];
        OmniUploadFile? removed = null;
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next)
            .Add(component => component.FileRemoved, file => removed = file));

        var rows = upload.FindAll(".omni-upload__file");
        Assert.Equal(2, rows.Count);
        Assert.Equal("rapport-2026.pdf", rows[0].QuerySelector(".omni-upload__file-name")!.TextContent);
        Assert.Equal("1,2 Mo", rows[0].QuerySelector(".omni-upload__file-size")!.TextContent);
        Assert.Equal("Retirer photo.jpg", rows[1].QuerySelector(".omni-upload__remove")!.GetAttribute("aria-label"));

        rows[0].QuerySelector(".omni-upload__remove")!.Click();

        Assert.Equal("rapport-2026.pdf", removed?.Name);
        Assert.Equal(["photo.jpg"], files.Select(file => file.Name));
        Assert.Contains("rapport-2026.pdf retiré de la liste.", upload.Find(".omni-upload__message").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_BoundMultiple_AppendsTheSelection_AndCountsTheWholeListAgainstTheMaximum()
    {
        IReadOnlyList<OmniUploadFile> files = [new("existant.pdf", 2048)];
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFiles, 3)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next));

        upload.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("un", "a.txt", contentType: "text/plain"),
            InputFileContent.CreateFromText("deux", "b.txt", contentType: "text/plain"));
        Assert.Equal(["existant.pdf", "a.txt", "b.txt"], files.Select(file => file.Name));

        upload.Render(parameters => parameters.Add(component => component.Files, files));
        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("trois", "c.txt", contentType: "text/plain"));

        Assert.Equal(3, files.Count);
        Assert.Contains("au maximum 3 fichiers", upload.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_BoundSingle_ReplacesTheFile_AndAFailedUploadAttachesNothing()
    {
        IReadOnlyList<OmniUploadFile> files = [new("ancien.pdf", 2048)];
        var single = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next));
        single.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("neuf", "neuf.txt", contentType: "text/plain"));
        Assert.Equal(["neuf.txt"], files.Select(file => file.Name));

        IReadOnlyList<OmniUploadFile> kept = [new("garde.pdf", 10)];
        var failing = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.Files, kept)
            .Add(component => component.FilesChanged, next => kept = next)
            .Add(component => component.Upload, _ => throw new InvalidOperationException("réseau")));
        failing.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "x.txt", contentType: "text/plain"));

        Assert.Equal(["garde.pdf"], kept.Select(file => file.Name));
        Assert.Contains("Le téléversement a échoué.", failing.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_Unbound_ListsTheLastSelectionWithoutRemoveButtons()
    {
        var upload = Render<OmniUpload>();
        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.Equal("note.txt", upload.Find(".omni-upload__files .omni-upload__file-name").TextContent);
        Assert.Empty(upload.FindAll(".omni-upload__remove"));
    }
}
