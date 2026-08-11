using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class SelectionComponentTests : BunitContext
{
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
        form.FindAll("#select-bar button")[2].Click();

        Assert.Equal("beta", form.Instance.Model.Single);
        Assert.Equal(["alpha", "gamma"], form.Instance.Model.Multiple);
        Assert.Equal("gamma", form.Instance.Model.ListValue);
        Assert.Equal(["alpha"], form.Instance.Model.Checked);
        Assert.Equal("beta", form.Instance.Model.Radio);
        Assert.Equal("gamma", form.Instance.Model.Bar);
        Assert.DoesNotContain("style=", form.Markup, StringComparison.OrdinalIgnoreCase);
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
        var form = Render<SelectionTestHost>(parameters => parameters.Add(component => component.DebounceMilliseconds, 100));

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
            .Add(component => component.DebounceMilliseconds, 0)
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
            .Add(component => component.DebounceMilliseconds, 0)
            .Add(component => component.Search, (_, _) => throw new InvalidOperationException("C:\\secret\\query.txt"))
            .Add(component => component.SearchFailed, exception => observed = exception));

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
    public void Upload_AppliesTheInputIdToTheNativeFileControl()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Id, "upload-wrapper")
            .Add(component => component.InputId, "upload-input"));

        Assert.Equal("upload-wrapper", upload.Find(".omni-upload").Id);
        Assert.Equal("upload-input", upload.Find("input[type=file]").Id);
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
    public void Upload_ValidatesTheOpenedStreamBeforeCallingTheTransport()
    {
        var uploadCalled = false;
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Validate, async request =>
            {
                await using var stream = request.OpenReadStream(request.Files[0]);
                var firstByte = stream.ReadByte();
                return firstByte == 'P' ? null : "La signature du fichier est invalide.";
            })
            .Add(component => component.Upload, _ =>
            {
                uploadCalled = true;
                return Task.CompletedTask;
            }));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.False(uploadCalled);
        Assert.Contains("La signature du fichier est invalide.", upload.Markup, StringComparison.Ordinal);
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
