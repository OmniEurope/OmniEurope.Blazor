using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Behaviour defects found while documenting the public API: each test failed before its fix. The
/// scripts are replaced by bUnit's module doubles, so the tests cover what .NET asks of them.
/// </summary>
public sealed class ReportedDefectRegressionTests : OmniBunitContext
{
    private static JSDisconnectedException Lost() => new("The circuit is gone.");

    // ---- lost circuit during the first render ----

    [Fact]
    public void TitleTooltips_ALostCircuitWhileInstalling_IsIgnored()
    {
        JSInterop.SetupModule(OmniModules.Tooltip).SetupVoid("installTitleTooltips", _ => true).SetException(Lost());

        var tooltips = Render<OmniTitleTooltips>();

        Assert.Empty(tooltips.Markup.Trim());
    }

    [Fact]
    public void Tabs_ALostCircuitWhileConfiguring_IsIgnored()
    {
        JSInterop.SetupModule(OmniModules.Focus).SetupVoid("configureTabs", _ => true).SetException(Lost());

        var tabs = Render<OmniTabs>(parameters => parameters.Add(component => component.ChildContent, builder =>
        {
            builder.OpenComponent<OmniTabsItem>(0);
            builder.AddAttribute(1, nameof(OmniTabsItem.Title), "Général");
            builder.CloseComponent();
        }));

        Assert.Single(tabs.FindAll("[role=tab]"));
    }

    [Fact]
    public void LogViewer_ALostCircuitWhileAttaching_IsIgnored()
    {
        JSInterop.SetupModule(OmniModules.LogViewer).SetupVoid("attach", _ => true).SetException(Lost());

        var viewer = Render<OmniLogViewer>(parameters => parameters.Add(component => component.Lines, Array.Empty<OmniLogLine>()));

        Assert.NotNull(viewer.Instance);
    }

    [Fact]
    public void Spreadsheet_ALostCircuitWhileAttaching_IsIgnored()
    {
        JSInterop.SetupModule(OmniModules.Spreadsheet).SetupVoid("attach", _ => true).SetException(Lost());

        var sheet = Render<OmniSpreadsheet>(parameters => parameters.Add(component => component.Value, OmniSpreadsheetData.Create(2, 2)));

        Assert.Equal(2, sheet.FindAll("tbody tr").Count);
    }

    [Fact]
    public void Stack_ALostCircuitWhileConfiguringTheScroll_IsIgnored()
    {
        JSInterop.SetupModule(OmniModules.Focus).SetupVoid("configureScrollOverflow", _ => true).SetException(Lost());

        var stack = Render<OmniStack>(parameters => parameters
            .Add(component => component.Overflow, OmniStackOverflow.Scroll)
            .AddChildContent("Contenu"));

        Assert.Contains("Contenu", stack.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectBar_ALostCircuitWhileConfiguring_IsIgnored()
    {
        JSInterop.SetupModule(OmniModules.Focus).SetupVoid("configureSelectBarOverflow", _ => true).SetException(Lost());
        var value = "a";

        var bar = Render<OmniSelectBar<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "Alpha"), new OmniOption<string>("b", "Beta")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal(2, bar.FindAll(".omni-select-bar__item").Count);
    }

    // ---- OmniTitleTooltips ----

    [Fact]
    public async Task TitleTooltips_Dispose_UninstallsTheDocumentListeners()
    {
        var module = JSInterop.SetupModule(OmniModules.Tooltip);
        Render<OmniTitleTooltips>();
        Assert.Single(module.Invocations["installTitleTooltips"]);

        await DisposeComponentsAsync();

        Assert.Single(module.Invocations["uninstallTitleTooltips"]);
    }

    // ---- OmniStepsItem.Index ----

    [Fact]
    public void Steps_WithoutIndex_AreNumberedInTheirOrder()
    {
        var steps = Render<OmniSteps>(parameters => parameters
            .Add(component => component.Value, 1)
            .Add(component => component.ChildContent, builder =>
            {
                foreach (var title in new[] { "Dépôt", "Examen", "Décision" })
                {
                    builder.OpenComponent<OmniStepsItem>(0);
                    builder.AddAttribute(1, nameof(OmniStepsItem.Title), title);
                    builder.CloseComponent();
                }
            }));

        Assert.Equal(["1", "2", "3"], steps.FindAll(".omni-steps__number").Select(number => number.TextContent));
        var selected = Assert.Single(steps.FindAll(".omni-steps__item--selected"));
        Assert.Contains("Examen", selected.TextContent, StringComparison.Ordinal);
    }

    // ---- OmniOverlayService.Dispose ----

    [Fact]
    public async Task OverlayService_Dispose_ClosesEveryDialogAndSaysSo()
    {
        var service = new OmniOverlayService();
        var changes = 0;
        service.OpenDialog(new OmniDialogRequest("Premier", _ => { }));
        var awaited = service.OpenDialogAsync(new OmniDialogRequest("Second", _ => { }));
        service.Changed += () => changes++;

        service.Dispose();

        Assert.Null(await awaited);
        Assert.Null(service.Dialog);
        Assert.Empty(service.Dialogs);
        Assert.Equal(1, changes);
    }

    // ---- OmniTreeItem: a load replaced by a newer one ----

    [Fact]
    public async Task TreeItem_AReplacedLoadThatStops_IsNeitherAFailureNorTheEndOfTheNewLoad()
    {
        var firstGate = new TaskCompletionSource();
        var calls = 0;
        Exception? reported = null;
        Func<CancellationToken, Task> loader = async token =>
        {
            if (++calls == 1)
            {
                await firstGate.Task;
                token.ThrowIfCancellationRequested();
                return;
            }

            await new TaskCompletionSource().Task;
        };
        var item = Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "root")
            .Add(component => component.Text, "Racine")
            .Add(component => component.LoadChildren, loader)
            .Add(component => component.OnLoadError, (Exception exception) => reported = exception));

        item.Find(".omni-tree__toggle").Click();
        item.Find(".omni-tree__toggle").Click();
        item.Find(".omni-tree__toggle").Click();
        Assert.Equal(2, calls);
        await item.InvokeAsync(firstGate.SetResult);

        Assert.Null(reported);
        Assert.Equal("status", item.Find(".omni-tree__state").GetAttribute("role"));
    }

    // ---- OmniSpreadsheetValue / OmniSpreadsheetData ----

    [Fact]
    public void SpreadsheetValue_AnErrorWithoutACode_IsRefused() =>
        Assert.Throws<ArgumentNullException>(() => OmniSpreadsheetValue.FromError(null!));

    [Fact]
    public void SpreadsheetAddress_ANegativeRow_IsRefused()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => OmniSpreadsheetData.Address(-1, 0));

        Assert.Equal("row", exception.ParamName);
    }

    // ---- OmniValidatorBase: a failure nobody handles ----

    [Fact]
    public void Validator_AFieldValidationFailureWithoutHandler_ReachesTheErrorBoundary()
    {
        var host = Render<ValidatorUnhandledFailureTestHost>();

        host.InvokeAsync(host.Instance.Trigger);

        host.WaitForAssertion(() => Assert.Equal("validation failure", host.Find(".boundary-error").TextContent));
    }

    // ---- OmniTextBox.Debounce: the pending value is not lost ----

    [Fact]
    public void TextBoxDebounce_SubmittingDuringTheDelay_SubmitsTheTypedValue()
    {
        var model = new TextModel();
        string? submitted = null;
        var form = Render<EditForm>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.OnValidSubmit, (EditContext _) => submitted = model.Text)
            .Add(component => component.ChildContent, (EditContext _) => DebouncedTextBox(model)));

        _ = form.Find("input").InputAsync(new ChangeEventArgs { Value = "abc" });
        form.Find("form").Submit();

        Assert.Equal("abc", submitted);
    }

    [Fact]
    public void TextBoxDebounce_LeavingTheFieldDuringTheDelay_UpdatesTheValue()
    {
        var model = new TextModel();
        var form = Render<EditForm>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.ChildContent, (EditContext _) => DebouncedTextBox(model)));

        _ = form.Find("input").InputAsync(new ChangeEventArgs { Value = "abc" });
        Assert.Equal(string.Empty, model.Text);
        form.Find("input").Blur();

        Assert.Equal("abc", model.Text);
    }

    private RenderFragment DebouncedTextBox(TextModel model) => builder =>
    {
        builder.OpenComponent<OmniTextBox>(0);
        builder.AddAttribute(1, nameof(OmniTextBox.Value), model.Text);
        builder.AddAttribute(2, nameof(OmniTextBox.ValueChanged), EventCallback.Factory.Create<string>(this, text => model.Text = text));
        builder.AddAttribute(3, nameof(OmniTextBox.ValueExpression), (Expression<Func<string>>)(() => model.Text));
        builder.AddAttribute(4, nameof(OmniTextBox.Debounce), TimeSpan.FromHours(1));
        builder.CloseComponent();
    };

    private sealed class TextModel
    {
        public string Text { get; set; } = string.Empty;
    }

    // ---- OmniImage ----

    [Fact]
    public void Image_Fit_LeavesTheHeightToTheAttributes_SoTheBoxIsTheOneGiven()
    {
        var image = Render<OmniImage>(parameters => parameters
            .Add(component => component.Source, "/photo.png")
            .Add(component => component.Alt, "Photo")
            .Add(component => component.Width, 180)
            .Add(component => component.Height, 120)
            .Add(component => component.Fit, OmniImageFit.Cover));

        var img = image.Find("img");
        Assert.Equal(["omni-image", "omni-image--cover"], img.ClassList);
        Assert.Equal("120", img.GetAttribute("height"));
        // object-fit only shows inside a box of its own: the base rule no longer forces height:auto,
        // which replaced the height attribute by the picture's own ratio; only a natural image takes it.
        var css = ShippedLookTests.Css;
        Assert.Matches(@"\.omni-image \{ display: block; max-width: 100%; \}", css);
        Assert.Matches(@"\.omni-image--natural \{ height: auto; \}", css);
        Assert.DoesNotMatch(@"\.omni-image--(contain|cover) \{[^}]*height", css);
    }

    [Fact]
    public void Image_AnInvalidHeight_NamesTheHeight()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniImage>(parameters => parameters
            .Add(component => component.Source, "/photo.png")
            .Add(component => component.Alt, "Photo")
            .Add(component => component.Height, 0)));

        Assert.Equal(nameof(OmniImage.Height), exception.ParamName);
    }

    [Fact]
    public void TextArea_AnInvalidMaxLength_NamesTheMaxLength()
    {
        var value = string.Empty;
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniTextArea>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.MaxLength, 0)));

        Assert.Equal(nameof(OmniTextArea.MaxLength), exception.ParamName);
    }

    // ---- PhosphorIconGlyphs.For ----

    [Fact]
    public void IconGlyphs_EveryNameHasItsGlyph_AndAnUnknownNameIsRefused()
    {
        foreach (var name in Enum.GetValues<OmniIconName>())
        {
            Assert.False(string.IsNullOrEmpty(PhosphorIconGlyphs.For(name).PathData), name.ToString());
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => PhosphorIconGlyphs.For((OmniIconName)9999));
    }

    // ---- OmniLoadingState ----

    [Fact]
    public void LoadingState_LoadsFromSeveralThreads_AreCountedExactly()
    {
        var state = new OmniLoadingState();

        Parallel.For(0, 200_000, index =>
        {
            if (index % 2 == 0)
            {
                state.Begin();
                state.End();
            }
            else
            {
                using var _ = state.Track();
            }
        });

        Assert.False(state.Loading);
        state.Begin();
        Assert.True(state.Loading);
        state.End();
        Assert.False(state.Loading);
    }

    // ---- OmniMain.AutoHideScrollbar ----

    [Fact]
    public void Main_TurningAutoHideOff_StopsWatchingTheScroll_AndOnAgainWatchesIt()
    {
        var module = JSInterop.SetupModule(OmniModules.Interop);
        var main = Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, true)
            .Add(component => component.AutoHideScrollbar, true)
            .AddChildContent("Main"));
        Assert.Single(module.Invocations["watchScrolling"]);

        main.Render(parameters => parameters.Add(component => component.AutoHideScrollbar, false));
        Assert.Single(module.Invocations["unwatchScrolling"]);

        main.Render(parameters => parameters.Add(component => component.AutoHideScrollbar, true));
        Assert.Equal(2, module.Invocations["watchScrolling"].Count);
    }

    // ---- OmniSlider ----

    [Fact]
    public void Slider_WritesItsValueTextInTheCurrentCulture_AndItsNumbersInvariant()
    {
        var value = 0.5;
        var slider = Render<OmniSlider>(parameters => parameters
            .Add(component => component.Minimum, 0d)
            .Add(component => component.Maximum, 1d)
            .Add(component => component.Step, 0.1d)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var input = slider.Find("input");
        Assert.Equal("0,5", input.GetAttribute("aria-valuetext"));
        Assert.Equal("0.5", input.GetAttribute("aria-valuenow"));
        Assert.Equal("0.5", input.GetAttribute("value"));
        Assert.Equal("0,5", slider.Find("output").TextContent);
    }

    // ---- OmniValueAxis.TickCount ----

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void ValueAxis_ATickCountBelowOne_IsRefused(int tickCount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniValueAxis>(parameters => parameters
            .Add(component => component.Minimum, 0d)
            .Add(component => component.Maximum, 10d)
            .Add(component => component.TickCount, tickCount)));

        Assert.Equal(nameof(OmniValueAxis.TickCount), exception.ParamName);
    }

    // ---- OmniDropDown / OmniListBox: a disabled option cannot be parsed in ----

    private static readonly OmniOption<string>[] OptionsWithADisabledOne =
        [new("a", "Alpha"), new("b", "Beta", Disabled: true)];

    [Fact]
    public void DropDown_TheIndexOfADisabledOption_DoesNotParse()
    {
        var value = "a";
        var dropDown = Render<ParsingDropDown>(parameters => parameters
            .Add(component => component.Options, OptionsWithADisabledOne)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.True(dropDown.Instance.Parse("0", out var enabled));
        Assert.Equal("a", enabled);
        Assert.False(dropDown.Instance.Parse("1", out _));
    }

    [Fact]
    public void ListBox_TheIndexOfADisabledOption_DoesNotParse()
    {
        var value = "a";
        var listBox = Render<ParsingListBox>(parameters => parameters
            .Add(component => component.Options, OptionsWithADisabledOne)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.True(listBox.Instance.Parse("0", out var enabled));
        Assert.Equal("a", enabled);
        Assert.False(listBox.Instance.Parse("1", out _));
    }

    private sealed class ParsingDropDown : OmniDropDown<string>
    {
        public bool Parse(string text, out string value) => TryParseValueFromString(text, out value, out _);
    }

    private sealed class ParsingListBox : OmniListBox<string, string>
    {
        public bool Parse(string text, out string value) => TryParseValueFromString(text, out value, out _);
    }
}
