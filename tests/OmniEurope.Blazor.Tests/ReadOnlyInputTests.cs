using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// ReadOnly (PLAN-002): every input component takes it, and an edit attempt neither changes the bound
/// value nor raises its change callback. The component keeps its look, its focus and its value in view.
/// </summary>
public sealed class ReadOnlyInputTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Choices =
        [new OmniOption<string>("alpha", "Alpha"), new OmniOption<string>("beta", "Beta"), new OmniOption<string>("gamma", "Gamma")];

    /// <summary>The bare check box and the one with a text (its input then inside a label) refuse the change alike.</summary>
    [Theory]
    [InlineData(null, "input")]
    [InlineData("Accepter", "label input")]
    public void CheckBox_ReadOnly_WithOrWithoutText_DoesNotChangeTheValue_NorRaiseTheCallback(string? text, string inputSelector)
    {
        var changes = new List<bool>();
        var value = false;
        var box = this.RenderBoundRecording<OmniCheckBox<bool>, bool>(value, changes, parameters =>
        {
            parameters.Add(component => component.ReadOnly, true);
            if (text is not null)
            {
                parameters.AddChildContent(text);
            }
        });

        var input = box.Find(inputSelector);
        Assert.Equal("true", input.GetAttribute("aria-readonly"));
        input.Change(true);

        Assert.Empty(changes);
    }

    [Fact]
    public void CheckBox_Nullable_ReadOnly_DoesNotCycle()
    {
        var changes = new List<bool?>();
        bool? value = null;
        var box = this.RenderBoundRecording<OmniCheckBox<bool?>, bool?>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true));

        box.Find("button").Click();

        Assert.Equal("true", box.Find("button").GetAttribute("aria-readonly"));
        Assert.Empty(changes);
    }

    [Fact]
    public void Switch_ReadOnly_DoesNotToggle()
    {
        var changes = new List<bool>();
        var value = false;
        var toggle = this.RenderBoundRecording<OmniSwitch<bool>, bool>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true));

        toggle.Find("button").Click();

        Assert.Equal("true", toggle.Find("button").GetAttribute("aria-readonly"));
        Assert.Equal("false", toggle.Find("button").GetAttribute("aria-checked"));
        Assert.Empty(changes);
    }

    [Fact]
    public void DropDown_ReadOnly_ShowsTheChosenTextInAReadOnlyField_AndTakesNoChange()
    {
        var changes = new List<string>();
        var value = "beta";
        var dropDown = this.RenderBoundRecording<OmniDropDown<string>, string>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        Assert.Empty(dropDown.FindAll("select"));
        var field = dropDown.Find("input");
        Assert.True(field.HasAttribute("readonly"));
        Assert.Equal("Beta", field.GetAttribute("value"));
        Assert.Throws<MissingEventHandlerException>(() => field.Change("Gamma"));

        Assert.Empty(changes);
    }

    [Fact]
    public void DropDown_Filterable_ReadOnly_DoesNotSearchOrSelect()
    {
        var changes = new List<string>();
        var value = "alpha";
        var dropDown = this.RenderBoundRecording<OmniDropDown<string>, string>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, Choices));

        var input = dropDown.Find("input");
        Assert.True(input.HasAttribute("readonly"));
        input.Input("gam");
        input.KeyDown("ArrowDown");

        Assert.Empty(dropDown.FindAll("[role=option]"));
        Assert.Empty(changes);
    }

    [Fact]
    public void Autocomplete_ReadOnly_DoesNotSearch_NorOpenTheList()
    {
        var searches = 0;
        var value = string.Empty;
        var autocomplete = this.RenderBound<OmniAutocomplete<string>, string>(value, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Search, (_, _) =>
            {
                searches++;
                return Task.FromResult(Choices);
            }));

        var input = autocomplete.Find("input");
        Assert.True(input.HasAttribute("readonly"));
        Assert.Equal("true", input.GetAttribute("aria-readonly"));
        input.Input("al");
        input.KeyDown("ArrowDown");

        Assert.Equal(0, searches);
        Assert.Empty(autocomplete.FindAll("[role=option]"));
    }

    [Fact]
    public void ListBox_ReadOnly_DrawsTheRowsAsAReadOnlyListbox_WithNoSelectToChange()
    {
        var changes = new List<string>();
        var value = "beta";
        var list = this.RenderBoundRecording<OmniListBox<string, string>, string>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        Assert.Empty(list.FindAll("select"));
        var box = list.Find("[role=listbox]");
        Assert.Equal("true", box.GetAttribute("aria-readonly"));
        Assert.Equal("0", box.GetAttribute("tabindex"));
        Assert.Equal(["false", "true", "false"], list.FindAll("[role=option]").Select(row => row.GetAttribute("aria-selected")));
        Assert.Throws<MissingEventHandlerException>(() => list.FindAll("[role=option]")[2].Click());

        Assert.Empty(changes);
        Assert.Equal(["false", "true", "false"], list.FindAll("[role=option]").Select(row => row.GetAttribute("aria-selected")));
    }

    [Fact]
    public void MultiSelect_ReadOnly_KeepsTheChoices_AndTakesNoToggleNorClear()
    {
        var changes = new List<IReadOnlyList<string>>();
        IReadOnlyList<string> value = ["alpha"];
        var select = this.RenderBoundRecording<OmniMultiSelect<string>, IReadOnlyList<string>>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        Assert.Empty(select.FindAll(".omni-multi-select-compact__clear"));
        var boxes = select.FindAll(".omni-multi-select-compact__option input[type=checkbox]");
        Assert.All(boxes, box => Assert.Equal("true", box.GetAttribute("aria-readonly")));
        boxes[1].Change(true);

        Assert.Empty(changes);
    }

    [Fact]
    public void CheckBoxList_ReadOnly_DoesNotToggle()
    {
        var changes = new List<IReadOnlyList<string>>();
        IReadOnlyList<string> value = ["alpha"];
        var list = this.RenderBoundRecording<OmniCheckBoxList<string>, IReadOnlyList<string>>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        var boxes = list.FindAll("input[type=checkbox]");
        Assert.All(boxes, box => Assert.Equal("true", box.GetAttribute("aria-readonly")));
        boxes[1].Change(true);

        Assert.Empty(changes);
    }

    [Fact]
    public void RadioButtonList_ReadOnly_IsAReadOnlyRadioGroup_AndTakesNoChoice()
    {
        var changes = new List<string>();
        var value = "alpha";
        var list = this.RenderBoundRecording<OmniRadioButtonList<string>, string>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        var group = list.Find("fieldset");
        Assert.Equal("radiogroup", group.GetAttribute("role"));
        Assert.Equal("true", group.GetAttribute("aria-readonly"));
        list.FindAll("input[type=radio]")[2].Change("gamma");

        Assert.Empty(changes);
    }

    [Fact]
    public void SelectBar_ReadOnly_DoesNotPick()
    {
        var changes = new List<string>();
        var value = "alpha";
        var bar = this.RenderBoundRecording<OmniSelectBar<string>, string>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        Assert.Equal("true", bar.Find("[role=radiogroup]").GetAttribute("aria-readonly"));
        bar.FindAll("button[role=radio]")[2].Click();

        Assert.Empty(changes);
    }

    [Fact]
    public void SelectBar_IconTemplate_DrawsAnIconBeforeEachOptionText()
    {
        var value = "alpha";
        var bar = this.RenderBound<OmniSelectBar<string>, string>(value, parameters => parameters
            .Add(component => component.Options, Choices)
            .Add(component => component.IconTemplate, option => builder => builder.AddMarkupContent(0, $"<i class=\"probe-icon\">{option}</i>")));

        Assert.Equal(["alpha", "beta", "gamma"], bar.FindAll("button[role=radio] .probe-icon").Select(icon => icon.TextContent));
    }

    [Fact]
    public void SelectableCard_ReadOnly_DoesNotToggle()
    {
        var changes = new List<bool>();
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Carte")
            .Add(component => component.Multiple, true)
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Value, false)
            .Add(component => component.ValueChanged, next => changes.Add(next)));

        var button = card.Find("button");
        Assert.Equal("true", button.GetAttribute("aria-readonly"));
        button.Click();
        button.KeyDown("Enter");

        Assert.Empty(changes);
        Assert.Equal("false", card.Find("button").GetAttribute("aria-checked"));
    }

    [Fact]
    public void SelectableCardGroup_ReadOnly_DoesNotPick()
    {
        var changes = new List<string>();
        var value = "alpha";
        var group = this.RenderBoundRecording<OmniSelectableCardGroup<string, string>, string>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        Assert.Equal("true", group.Find(".omni-selectable-card-group").GetAttribute("aria-readonly"));
        var cards = group.FindAll("button.omni-selectable-card");
        cards[2].Click();
        cards[0].KeyDown("ArrowDown");

        Assert.Empty(changes);
        Assert.Equal(["true", "false", "false"], group.FindAll("button.omni-selectable-card").Select(card => card.GetAttribute("aria-checked")));
    }

    [Fact]
    public void Slider_ReadOnly_KeepsItsValue()
    {
        var changes = new List<double>();
        var committed = new List<double>();
        double value = 40;
        var slider = this.RenderBoundRecording<OmniSlider, double>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.OnValueCommit, next => committed.Add(next)));

        var input = slider.Find("input[type=range]");
        Assert.Equal("true", input.GetAttribute("aria-readonly"));
        input.Input("80");
        input.Change("80");

        Assert.Empty(changes);
        Assert.Empty(committed);
    }

    [Fact]
    public void ColorPicker_ReadOnly_KeepsItsValue()
    {
        var changes = new List<string>();
        var value = "#112233";
        var picker = this.RenderBoundRecording<OmniColorPicker, string>(value, changes, parameters => parameters
            .Add(component => component.ReadOnly, true));

        Assert.Contains("omni-color-picker--readonly", picker.Find(".omni-color-picker").ClassList);
        picker.Find("input[type=color]").Input("#ff0000");

        Assert.Empty(changes);
    }

    [Fact]
    public void Upload_ReadOnly_ListsTheFiles_WithNoPickerNorRemoveButton()
    {
        var selected = 0;
        var removed = 0;
        IReadOnlyList<OmniUploadFile> files = [new("a.txt", 2_000, "text/plain"), new("b.txt", 3_000, "text/plain")];
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Multiple, true)
            .Add(component => component.Files, files)
            .Add(component => component.OnFilesSelect, _ => selected++)
            .Add(component => component.OnFileRemove, _ => removed++));

        Assert.Empty(upload.FindComponents<InputFile>());
        Assert.Empty(upload.FindAll(".omni-upload__remove"));
        Assert.Empty(upload.FindAll(".omni-upload__clear"));
        Assert.Equal(["a.txt", "b.txt"], upload.FindAll(".omni-upload__file-name").Select(name => name.TextContent));
        Assert.Equal("0", upload.Find(".omni-upload__readonly").GetAttribute("tabindex"));
        Assert.Equal(0, selected);
        Assert.Equal(0, removed);
    }

    [Fact]
    public void Upload_ReadOnly_NamesItsGroupAsTheFileControlWouldBeNamed()
    {
        IReadOnlyList<OmniUploadFile> files = [new("a.txt", 2_000, "text/plain")];
        IRenderedComponent<OmniUpload> Upload(Action<ComponentParameterCollectionBuilder<OmniUpload>> more) =>
            Render<OmniUpload>(parameters =>
            {
                parameters.Add(component => component.ReadOnly, true).Add(component => component.Files, files);
                more(parameters);
            });

        // Two read-only fields of one page ("Contrat", "Pièce d'identité") must not both be "Fichiers joints".
        var named = Upload(parameters => parameters.Add(component => component.Label, "Contrat signé"));
        Assert.Equal("Contrat signé", named.Find(".omni-upload__readonly").GetAttribute("aria-label"));

        var hostNamed = Upload(parameters => parameters.AddUnmatched("aria-label", "Pièce d'identité"));
        Assert.Equal("Pièce d'identité", hostNamed.Find(".omni-upload__readonly").GetAttribute("aria-label"));
        Assert.False(hostNamed.Find(".omni-upload").HasAttribute("aria-label"));

        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            var unnamed = Upload(_ => { });
            Assert.Equal("Fichiers joints", unnamed.Find(".omni-upload__readonly").GetAttribute("aria-label"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void DropDown_ReadOnly_WithAValueNoOptionHolds_ShowsAnEmptyField()
    {
        var value = "inconnu";
        var dropDown = this.RenderBound<OmniDropDown<string>, string>(value, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Options, Choices));

        Assert.Equal(string.Empty, dropDown.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void ListBox_ReadOnly_Multiple_MarksTheListMultiSelectable_AndTheDisabledRows()
    {
        IReadOnlyList<string> value = ["alpha", "gamma"];
        var list = this.RenderBound<OmniListBox<string, IReadOnlyList<string>>, IReadOnlyList<string>>(value, parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Multiple, true)
            .Add(component => component.Disabled, true)
            .Add(component => component.Options, [new OmniOption<string>("alpha", "Alpha"), new OmniOption<string>("beta", "Beta", Disabled: true), new OmniOption<string>("gamma", "Gamma")]));

        var box = list.Find("[role=listbox]");
        Assert.Equal("true", box.GetAttribute("aria-multiselectable"));
        Assert.Equal("true", box.GetAttribute("aria-disabled"));
        Assert.False(box.HasAttribute("tabindex"));
        Assert.Equal(["true", "false", "true"], list.FindAll("[role=option]").Select(row => row.GetAttribute("aria-selected")));
        Assert.Equal([null, "true", null], list.FindAll("[role=option]").Select(row => row.GetAttribute("aria-disabled")));
    }

    [Fact]
    public void Upload_ReadOnly_ListsTheFilesChosenBeforeItLocked()
    {
        var upload = Render<OmniUpload>(parameters => parameters.Add(component => component.Multiple, true));
        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "avant.txt", contentType: "text/plain"));

        upload.Render(parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.Empty(upload.FindComponents<InputFile>());
        Assert.Equal(["avant.txt"], upload.FindAll(".omni-upload__file-name").Select(name => name.TextContent));
    }

    [Fact]
    public void Upload_ReadOnly_Bound_ListsTheBoundFiles()
    {
        IReadOnlyList<OmniUploadFile> files = [new("lie.txt", 10, "text/plain")];
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next));

        Assert.Equal(["lie.txt"], upload.FindAll(".omni-upload__file-name").Select(name => name.TextContent));
        Assert.Empty(upload.FindAll(".omni-upload__remove"));
    }

    [Fact]
    public void Upload_ReadOnly_WithNoFile_SaysSo()
    {
        var upload = Render<OmniUpload>(parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.Empty(upload.FindComponents<InputFile>());
        Assert.Single(upload.FindAll(".omni-upload__field-value--empty"));
    }

    [Theory]
    [InlineData(OmniTextTone.Info, "omni-text--info")]
    [InlineData(OmniTextTone.Warning, "omni-text--warning")]
    public void Text_InfoAndWarningTones_WriteTheirToneClass(OmniTextTone tone, string expected)
    {
        var text = Render<OmniText>(parameters => parameters
            .Add(component => component.Tone, tone)
            .AddChildContent("Note"));

        Assert.Contains(expected, text.Find(".omni-text").ClassList);
    }
}
