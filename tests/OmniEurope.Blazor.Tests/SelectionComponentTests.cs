using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class SelectionComponentTests : OmniBunitContext
{
    [Fact]
    public void DropDown_Label_IsTheAccessibleName_AndWinsOverARawAriaLabel()
    {
        // @attributes come first and the component's own attributes after (PLAN-007): the accessible
        // name is the Label parameter, a raw aria-label attribute does not override it.
        var value = string.Empty;
        var dropDown = Render<OmniDropDown<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("alpha", "Alpha")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Label, "Project")
            .AddUnmatched("aria-label", "Raw"));

        Assert.Equal("Project", dropDown.Find("select").GetAttribute("aria-label"));
    }

    [Fact]
    public void NativeSelectors_UpdateTheirBoundValues()
    {
        var form = Render<SelectionTestHost>();

        form.Find("#drop-down").Change("1");
        form.Find("#multi-select").TriggerEvent("onchange", new Microsoft.AspNetCore.Components.ChangeEventArgs
        {
            Value = new[] { "0", "2" }
        });
        form.Find("#list-box").Change("2");
        form.Find("#checkbox-list input[type=checkbox]").Change(true);
        form.Find("#radio-list input[value=beta]").Change(true);
        form.FindAll("#select-bar .omni-select-bar__item")[2].Click();

        Assert.Equal("beta", form.Instance.Model.Single);
        Assert.Equal(["alpha", "gamma"], form.Instance.Model.Multiple);
        Assert.Equal("gamma", form.Instance.Model.ListValue);
        Assert.Equal(["alpha"], form.Instance.Model.Checked);
        Assert.Equal("beta", form.Instance.Model.Radio);
        Assert.Equal("gamma", form.Instance.Model.Bar);
        Assert.DoesNotContain("style=", form.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ListBox_Multiple_IsANativeMultipleListThatSkipsDisabledOptions()
    {
        IReadOnlyList<string> bound = ["beta"];
        var options = new OmniOption<string>[] { new("alpha", "Alpha"), new("beta", "Beta"), new("gamma", "Gamma", Disabled: true) };
        var list = Render<OmniListBox<string, IReadOnlyList<string>>>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.VisibleRows, 4)
            .Add(component => component.Options, options)
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, value => bound = value));

        var select = list.Find("select.omni-list-box");
        Assert.True(select.HasAttribute("multiple"));
        Assert.Equal("4", select.GetAttribute("size"));
        Assert.True(list.FindAll("option")[1].HasAttribute("selected"));

        select.TriggerEvent("onchange", new ChangeEventArgs { Value = new[] { "0", "2" } });

        Assert.Equal(["alpha"], bound);
    }

    [Fact]
    public void ListBox_Single_RendersNoMultipleAttribute()
    {
        var bound = "beta";
        var list = Render<OmniListBox<string, string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("alpha", "Alpha"), new OmniOption<string>("beta", "Beta")])
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound));

        Assert.False(list.Find("select").HasAttribute("multiple"));
        Assert.True(list.FindAll("option")[1].HasAttribute("selected"));
    }

    [Fact]
    public void ListBox_RefusesABindingThatDoesNotMatchItsMode()
    {
        var single = "alpha";
        IReadOnlyList<string> many = [];
        var options = new[] { new OmniOption<string>("alpha", "Alpha") };

        var multipleOnOne = Assert.Throws<InvalidOperationException>(() => Render<OmniListBox<string, string>>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.Options, options)
            .Add(component => component.Value, single)
            .Add(component => component.ValueExpression, () => single)));
        var singleOnMany = Assert.Throws<InvalidOperationException>(() => Render<OmniListBox<string, IReadOnlyList<string>>>(parameters => parameters
            .Add(component => component.Options, options)
            .Add(component => component.Value, many)
            .Add(component => component.ValueExpression, () => many)));

        Assert.Contains("Multiple", multipleOnOne.Message, StringComparison.Ordinal);
        Assert.Contains("Multiple", singleOnMany.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DateSliderAndColor_UpdateTheirBoundValues()
    {
        var form = Render<SelectionTestHost>();

        form.Find("#date").Change("2026-08-10");
        form.Find("#slider").Input("7.5");
        form.Find("#color").Input("#12abef");

        Assert.Equal(new DateOnly(2026, 8, 10), form.Instance.Model.Date);
        Assert.Equal(7.5, form.Instance.Model.Amount);
        Assert.Equal("#12ABEF", form.Instance.Model.Color);
        var holder = new SliderHolder { Value = 2 };
        Assert.Equal("vertical", Render<OmniSlider>(parameters => parameters
            .Add(component => component.Value, holder.Value)
            .Add(component => component.ValueExpression, () => holder.Value)
            .Add(component => component.Vertical, true)).Find("input").GetAttribute("aria-orientation"));
    }

    [Fact]
    public void Slider_ValueCommitted_FiresOnceOnReleaseWhileValueChangedFollowsTheDrag()
    {
        var holder = new SliderHolder { Value = 0 };
        var steps = new List<double>();
        double? committed = null;
        var slider = Render<OmniSlider>(parameters => parameters
            .Add(component => component.Value, holder.Value)
            .Add(component => component.ValueExpression, () => holder.Value)
            .Add(component => component.ValueChanged, value => steps.Add(value))
            .Add(component => component.ValueCommitted, value => committed = value));

        slider.Find("input").Input("10");
        slider.Find("input").Input("20");
        Assert.Null(committed);

        slider.Find("input").Change("20");

        Assert.Equal([10, 20], steps);
        Assert.Equal(20, committed);
    }

    [Fact]
    public void Slider_WithoutValueCommitted_AttachesNoChangeHandler()
    {
        var holder = new SliderHolder { Value = 3 };
        var slider = Render<OmniSlider>(parameters => parameters
            .Add(component => component.Value, holder.Value)
            .Add(component => component.ValueExpression, () => holder.Value));

        Assert.Throws<Bunit.MissingEventHandlerException>(() => slider.Find("input").Change("4"));
    }

    [Theory]
    [InlineData(10, 0, 1, 5)]
    [InlineData(0, 10, 0, 5)]
    [InlineData(0, 10, 1, 11)]
    public void Slider_RejectsInvalidBoundsStepAndInitialValue(double minimum, double maximum, double step, double value)
    {
        var holder = new SliderHolder { Value = value };

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Render<OmniSlider>(parameters => parameters
            .Add(component => component.Minimum, minimum)
            .Add(component => component.Maximum, maximum)
            .Add(component => component.Step, step)
            .Add(component => component.Value, holder.Value)
            .Add(component => component.ValueExpression, () => holder.Value)));
    }

    [Fact]
    public void DatePicker_RejectsContradictoryBounds()
    {
        DateOnly? value = null;

        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniDatePicker>(parameters => parameters
            .Add(component => component.Minimum, new DateOnly(2026, 8, 12))
            .Add(component => component.Maximum, new DateOnly(2026, 8, 11))
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)));

        Assert.Equal("Minimum cannot be greater than Maximum.", exception.Message);
    }

    [Fact]
    public void DateTimePicker_BindsALocalMomentAndRejectsContradictoryBounds()
    {
        var form = Render<SelectionTestHost>();

        form.Find("#date-time").Change("2026-03-02T14:30");
        Assert.Equal(new DateTime(2026, 3, 2, 14, 30, 0, DateTimeKind.Unspecified), form.Instance.Model.Moment);
        // A text field with the house panel now, written in the culture (fr-FR here), and still
        // reading the ISO shape the native control used to send.
        Assert.Equal("text", form.Find("#date-time").GetAttribute("type"));
        Assert.Equal("02/03/2026 14:30", form.Find("#date-time").GetAttribute("value"));

        // Out of the declared bounds: refused. The bound value is left as it was and the field is
        // marked invalid, rather than silently clamped to the bound.
        form.Find("#date-time").Change("2031-01-01T00:00");
        Assert.Equal(new DateTime(2026, 3, 2, 14, 30, 0, DateTimeKind.Unspecified), form.Instance.Model.Moment);
        Assert.Equal("true", form.Find("#date-time").GetAttribute("aria-invalid"));

        DateTime? value = null;
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniDateTimePicker>(parameters => parameters
            .Add(component => component.Minimum, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Unspecified))
            .Add(component => component.Maximum, new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Unspecified))
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)));
        Assert.Equal("Minimum cannot be greater than Maximum.", exception.Message);
    }

    [Fact]
    public async Task Autocomplete_DebouncesAnnouncesAndSelectsAResult()
    {
        var form = Render<SelectionTestHost>();

        await form.Find("#autocomplete").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "alp" });
        form.WaitForAssertion(() => Assert.Equal(1, form.Instance.SearchCount));
        Assert.Contains("1 résultat disponible.", form.Markup, StringComparison.Ordinal);

        form.Find(".omni-autocomplete__option").Click();

        Assert.Equal("alpha", form.Instance.Model.Autocomplete);
        Assert.Contains("Alpha sélectionné.", form.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Autocomplete_DebouncesRapidInputsAndSearchesOnlyTheLatestTerm()
    {
        var form = Render<SelectionTestHost>(parameters => parameters.Add(component => component.Debounce, TimeSpan.FromMilliseconds(100)));

        var input = form.Find("#autocomplete");
        var first = input.InputAsync(new ChangeEventArgs { Value = "a" });
        var second = input.InputAsync(new ChangeEventArgs { Value = "al" });
        var third = input.InputAsync(new ChangeEventArgs { Value = "alp" });
        await Task.WhenAll(first, second, third);

        form.WaitForAssertion(() =>
        {
            Assert.Equal(1, form.Instance.SearchCount);
            Assert.Equal("alp", form.Instance.LastSearch);
        }, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Autocomplete_IgnoresAnOlderSearchThatCompletesLast()
    {
        var value = string.Empty;
        var stale = new TaskCompletionSource<IReadOnlyList<OmniOption<string>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = new TaskCompletionSource<IReadOnlyList<OmniOption<string>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (_, _) => ++callCount == 1 ? stale.Task : latest.Task));

        var input = autocomplete.Find("input");
        var staleSearch = input.InputAsync(new ChangeEventArgs { Value = "first" });
        Assert.Equal(1, callCount);
        var latestSearch = input.InputAsync(new ChangeEventArgs { Value = "second" });
        Assert.Equal(2, callCount);

        latest.SetResult([new("latest", "Récent")]);
        await latestSearch;
        stale.SetResult([new("stale", "Obsolète")]);
        await staleSearch;

        Assert.Contains("Récent", autocomplete.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Obsolète", autocomplete.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Autocomplete_ExposesARecoverableErrorWithoutLeakingExceptionDetails()
    {
        var value = string.Empty;
        Exception? observed = null;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (_, _) => throw new InvalidOperationException("C:\\secret\\query.txt"))
            .Add(component => component.OnSearchError, exception => observed = exception));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "query" });

        Assert.IsType<InvalidOperationException>(observed);
        Assert.Equal("alert", autocomplete.Find(".omni-autocomplete__error").GetAttribute("role"));
        Assert.Contains("La recherche a échoué", autocomplete.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", autocomplete.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-error", autocomplete.Find("input").GetAttribute("aria-describedby"), StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_ValidatesTypeAndReportsSuccessfulProgress()
    {
        var uploadCalled = false;
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.AllowedContentTypes, ["text/plain"])
            .Add(component => component.Upload, request =>
            {
                uploadCalled = true;
                request.ReportProgress(40);
                return Task.CompletedTask;
            }));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.True(uploadCalled);
        Assert.Contains("Téléversement terminé.", upload.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-valuenow=\"100\"", upload.Markup, StringComparison.Ordinal);

        var invalid = Render<OmniUpload>(parameters => parameters
            .Add(component => component.AllowedContentTypes, ["image/png"]));
        invalid.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.Contains("n'est pas autorisé", invalid.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", invalid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Upload_PutsItsIdOnTheNativeFileControl_SoAFormFieldLabelNamesIt()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Id, "upload-input"));

        Assert.Equal("upload-input", upload.Find("input[type=file]").Id);
        Assert.True(string.IsNullOrEmpty(upload.Find(".omni-upload").Id));
        Assert.Single(upload.FindAll("#upload-input"));
        Assert.Contains("upload-input-hint", upload.Find("input[type=file]").GetAttribute("aria-describedby"), StringComparison.Ordinal);
    }

    [Fact]
    public void Upload_DoesNotExposeCallbackExceptionDetails()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Upload, _ => throw new InvalidOperationException("C:\\secret\\token.txt")));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.Contains("Le téléversement a échoué.", upload.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", upload.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token.txt", upload.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Upload_RejectsTheFilesAfterCheckingTheOpenedStream_WithoutStoringOrListingThem()
    {
        var stored = new List<string>();
        IReadOnlyList<OmniUploadFile> files = [];
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next)
            .Add(component => component.Upload, async request =>
            {
                await using var stream = request.OpenReadStream(request.Files[0]);
                if (stream.ReadByte() != 'P')
                {
                    request.Reject("La signature du fichier est invalide.");
                    return;
                }

                stored.Add(request.Files[0].Name);
            }));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.Empty(stored);
        Assert.Empty(files);
        Assert.Contains("La signature du fichier est invalide.", upload.Markup, StringComparison.Ordinal);
        Assert.Empty(upload.FindAll(".omni-upload__retry"));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("PNG", "plan.png", contentType: "text/plain"));

        Assert.Equal(["plan.png"], stored);
        Assert.Equal("plan.png", Assert.Single(files).Name);
    }

    [Fact]
    public void LargeSelector_ReloadsOptionsAndSelectsTheNewLastValue()
    {
        var selector = Render<LargeSelectionTestHost>();
        Assert.Equal(10_000, selector.FindAll("option").Count);

        selector.Render(parameters => parameters.Add(component => component.OptionCount, 10_001));
        selector.Find("#large-selector").Change("10000");

        Assert.Equal(10_001, selector.FindAll("option").Count);
        Assert.Equal(10_000, selector.Instance.Model.Value);
    }

    private sealed class SliderHolder
    {
        public double Value { get; set; }
    }
}
