using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniUpload.Accept"/>: the picker filter, apart from the content-type check.</summary>
public sealed class UploadAcceptTests : OmniBunitContext
{
    [Fact]
    public void Accept_IsPassedToTheInput_AndWinsOverTheDerivedList()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Accept, ".csv,text/csv")
            .Add(component => component.AllowedContentTypes, ["text/csv"]));

        Assert.Equal(".csv,text/csv", upload.Find("input[type=file]").GetAttribute("accept"));
    }

    [Fact]
    public void WithoutAccept_TheAttributeStillComesFromTheAllowedTypes_OrIsAbsent()
    {
        var typed = Render<OmniUpload>(parameters => parameters.Add(component => component.AllowedContentTypes, ["image/png", "application/pdf"]));
        var open = Render<OmniUpload>();

        Assert.Equal("image/png,application/pdf", typed.Find("input[type=file]").GetAttribute("accept"));
        Assert.Null(open.Find("input[type=file]").GetAttribute("accept"));
    }

    [Fact]
    public void Accept_DoesNotReplaceTheContentTypeCheck()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Accept, ".csv")
            .Add(component => component.AllowedContentTypes, ["text/csv"]));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "note.txt", contentType: "text/plain"));

        Assert.Contains("omni-upload__message--error", upload.Markup, StringComparison.Ordinal);
    }
}
