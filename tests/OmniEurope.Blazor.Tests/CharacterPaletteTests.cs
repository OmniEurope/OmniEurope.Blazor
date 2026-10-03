using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniCharacterPalette"/> on its own, then under <see cref="OmniHtmlEditor"/> through its
/// <c>Characters</c> parameter. The surface script is bUnit's module double.
/// </summary>
public sealed class CharacterPaletteTests : OmniBunitContext
{
    private const string ModulePath = Internal.OmniModules.HtmlEditor;

    /// <summary>The combining acute accent (U+0301): after a letter, one grapheme of two characters.</summary>
    private const char Acute = (char)0x0301;

    private static readonly IReadOnlyList<string> French = ["é", "è", "ç", "œ", "«", "»"];

    [Fact]
    public void Palette_DrawsEachCharacter_AsALabelledButtonOfAToolbar()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            var palette = Render<OmniCharacterPalette>(parameters => parameters
                .Add(component => component.Characters, ["é", "«", "ß", "œ"]));

            var bar = palette.Find(".omni-character-palette");
            Assert.Equal("toolbar", bar.GetAttribute("role"));
            Assert.Equal("Caractères spéciaux", bar.GetAttribute("aria-label"));
            var buttons = palette.FindAll(".omni-character-palette__character");
            Assert.Equal(["é", "«", "ß", "œ"], buttons.Select(button => button.TextContent));
            Assert.Equal(["Insérer é", "Insérer «", "Insérer ß", "Insérer œ"], buttons.Select(button => button.GetAttribute("aria-label")));
            Assert.All(buttons, button => Assert.Equal("button", button.GetAttribute("type")));
            Assert.Equal("Majuscules", palette.Find(".omni-character-palette__case").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Palette_TakesTheHostsLabels_AndLeavesEmptyItemsOut()
    {
        var palette = Render<OmniCharacterPalette>(parameters => parameters
            .Add(component => component.Characters, ["ä", "", "ö"])
            .Add(component => component.Label, "Deutsch")
            .Add(component => component.UppercaseLabel, "Groß")
            .Add(component => component.Class, "host"));

        Assert.Equal("Deutsch", palette.Find(".omni-character-palette").GetAttribute("aria-label"));
        Assert.Contains("host", palette.Find(".omni-character-palette").ClassList);
        Assert.Equal("Groß", palette.Find(".omni-character-palette__case").GetAttribute("aria-label"));
        Assert.Equal(["ä", "ö"], palette.FindAll(".omni-character-palette__character").Select(button => button.TextContent));
    }

    [Fact]
    public async Task OnSelect_GetsTheFormShown_LowerThenUpperOnceTheToggleIsPressed()
    {
        var chosen = new List<string>();
        var palette = Render<OmniCharacterPalette>(parameters => parameters
            .Add(component => component.Characters, French)
            .Add(component => component.OnSelect, (string character) => chosen.Add(character)));
        var toggle = palette.Find(".omni-character-palette__case");
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));

        await Character(palette, 0).ClickAsync(new MouseEventArgs());
        await toggle.ClickAsync(new MouseEventArgs());

        Assert.Equal("true", palette.Find(".omni-character-palette__case").GetAttribute("aria-pressed"));
        Assert.True(palette.Instance.IsUppercase);
        Assert.Equal(["É", "È", "Ç", "Œ", "«", "»"], palette.FindAll(".omni-character-palette__character").Select(button => button.TextContent));

        await Character(palette, 3).ClickAsync(new MouseEventArgs());
        // A character without an uppercase form is handed out as it is.
        await Character(palette, 4).ClickAsync(new MouseEventArgs());

        Assert.Equal(["é", "Œ", "«"], chosen);
    }

    [Fact]
    public async Task ShiftClick_GetsTheUppercaseForm_WithoutPressingTheToggle()
    {
        var chosen = new List<string>();
        var palette = Render<OmniCharacterPalette>(parameters => parameters
            .Add(component => component.Characters, ["ç", "ß", "e" + Acute])
            .Add(component => component.OnSelect, (string character) => chosen.Add(character)));

        await Character(palette, 0).ClickAsync(new MouseEventArgs { ShiftKey = true });
        await Character(palette, 1).ClickAsync(new MouseEventArgs { ShiftKey = true });
        await Character(palette, 2).ClickAsync(new MouseEventArgs { ShiftKey = true });
        await Character(palette, 0).ClickAsync(new MouseEventArgs());

        // ß has no single uppercase letter by the invariant culture; a grapheme keeps its combining mark.
        Assert.Equal(["Ç", "ß", "E" + Acute, "ç"], chosen);
        Assert.Equal("false", palette.Find(".omni-character-palette__case").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Toggle_IsLeftOut_WhenNoCharacterHasAnUppercaseForm_OrWhenTheHostHidesIt()
    {
        var punctuation = Render<OmniCharacterPalette>(parameters => parameters
            .Add(component => component.Characters, ["«", "»", "–", "ß"]));
        var hidden = Render<OmniCharacterPalette>(parameters => parameters
            .Add(component => component.Characters, French)
            .Add(component => component.ShowUppercaseToggle, false));

        Assert.Empty(punctuation.FindAll(".omni-character-palette__case"));
        Assert.Empty(punctuation.FindAll("[role=separator]"));
        Assert.Equal(4, punctuation.FindAll(".omni-character-palette__character").Count);
        Assert.Empty(hidden.FindAll(".omni-character-palette__case"));
    }

    [Fact]
    public void EveryButton_PreventsTheDefaultOfMouseDown_SoAnEditorKeepsItsCaret()
    {
        var palette = Render<OmniCharacterPalette>(parameters => parameters
            .Add(component => component.Characters, French));

        var buttons = palette.FindAll("button");
        Assert.Equal(French.Count + 1, buttons.Count);
        Assert.All(buttons, button => Assert.True(button.HasAttribute("blazor:onmousedown:preventDefault")));
    }

    [Fact]
    public void Editor_DrawsNoPalette_UnlessCharactersAreGivenAndTheEditorIsEditable()
    {
        SetupSurface();

        Assert.Empty(RenderEditor(null).FindAll(".omni-character-palette"));
        Assert.Empty(RenderEditor([]).FindAll(".omni-character-palette"));
        Assert.Empty(RenderEditor(French, readOnly: true).FindAll(".omni-character-palette"));
        Assert.Empty(RenderEditor(French, disabled: true).FindAll(".omni-character-palette"));
        Assert.Empty(RenderEditor(French, mode: OmniHtmlEditorMode.Source).FindAll(".omni-character-palette"));

        var editor = RenderEditor(French);
        var palette = editor.Find(".omni-html-editor .omni-character-palette");
        Assert.Contains("omni-html-editor__palette", palette.ClassList);
        Assert.Equal(editor.Find(".omni-html-editor__surface").GetAttribute("id"), palette.GetAttribute("aria-controls"));
        // Under the surface: the palette follows it in the document.
        Assert.Contains("omni-html-editor__surface", palette.PreviousElementSibling!.ClassList);
        Assert.Equal(French.Count, editor.FindAll(".omni-character-palette__character").Count);
    }

    [Fact]
    public async Task Editor_TypesTheChosenCharacterAtTheCaret_ThroughTheSurfaceTextInsertion_AsOneUndoStep()
    {
        var module = SetupSurface();
        module.Setup<string?>("exec", _ => true).SetResult("<p>AÉ</p>");
        var changes = new List<string>();
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string changed) => changes.Add(changed))
            .Add(component => component.Commands, [OmniHtmlEditorCommands.Undo])
            .Add(component => component.Characters, French));
        Assert.True(editor.Find("[data-command=undo]").HasAttribute("disabled"));

        await editor.Find(".omni-character-palette__case").ClickAsync(new MouseEventArgs());
        await editor.FindAll(".omni-character-palette__character")[0].ClickAsync(new MouseEventArgs());

        var exec = Assert.Single(module.Invocations["exec"]);
        Assert.Equal("inserttext", exec.Arguments[1]);
        Assert.Equal("É", exec.Arguments[2]);
        Assert.Equal(["<p>AÉ</p>"], changes);
        Assert.False(editor.Find("[data-command=undo]").HasAttribute("disabled"));
    }

    /// <summary>A character button found again: a click renders the palette anew.</summary>
    private static AngleSharp.Dom.IElement Character(IRenderedComponent<OmniCharacterPalette> palette, int index) =>
        palette.FindAll(".omni-character-palette__character")[index];

    private BunitJSModuleInterop SetupSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        return module;
    }

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(
        IReadOnlyList<string>? characters,
        bool readOnly = false,
        bool disabled = false,
        OmniHtmlEditorMode mode = OmniHtmlEditorMode.Visual)
    {
        var value = "<p>A</p>";
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ReadOnly, readOnly)
            .Add(component => component.Disabled, disabled)
            .Add(component => component.Mode, mode)
            .Add(component => component.Characters, characters));
    }
}
