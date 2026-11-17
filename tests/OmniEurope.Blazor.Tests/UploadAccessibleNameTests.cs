using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The request of a client application of 2026-11-17 (axe label, label-title-only): the file control of an
/// OmniUpload had no name outside a form field, only descriptions, and an aria-label given to the component
/// landed on its root, a div without a role. The control is now always named.
/// </summary>
public sealed class UploadAccessibleNameTests : OmniBunitContext
{
    [Fact]
    public void AFieldOutsideAFormField_IsNamedByItsBrowseText()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Display, OmniUploadDisplay.Field)
            .Add(component => component.Accept, ".csv,.txt"));

        Assert.Equal("Parcourir", upload.Find("input.omni-upload__input").GetAttribute("aria-label"));
    }

    [Fact]
    public void AZoneOutsideAFormField_IsNamedByItsPrompt()
    {
        var upload = Render<OmniUpload>();

        var name = upload.Find("input.omni-upload__input").GetAttribute("aria-label");
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Equal(System.Text.RegularExpressions.Regex.Replace(upload.Find(".omni-upload__prompt").TextContent.Trim(), @"\s+", " "), name);
    }

    [Fact]
    public void TheLabel_AndElseTheHostAriaLabel_NameTheControl_NeverTheRoot()
    {
        var labelled = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Display, OmniUploadDisplay.Field)
            .Add(component => component.Label, "Fichier de contacts"));
        Assert.Equal("Fichier de contacts", labelled.Find("input.omni-upload__input").GetAttribute("aria-label"));

        var host = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Display, OmniUploadDisplay.Field)
            .AddUnmatched("aria-label", "Import CSV")
            .AddUnmatched("data-test", "upload"));
        Assert.Equal("Import CSV", host.Find("input.omni-upload__input").GetAttribute("aria-label"));
        var root = host.Find(".omni-upload");
        Assert.False(root.HasAttribute("aria-label"));
        Assert.Equal("upload", root.GetAttribute("data-test"));
    }

    [Fact]
    public void InsideAFormFieldThatLabelsIt_TheLabelForNamesTheControl()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "contacts")
            .Add(component => component.Label, "Contacts")
            .AddChildContent<OmniUpload>(upload => upload
                .Add(component => component.Id, "contacts")
                .Add(component => component.Display, OmniUploadDisplay.Field)));

        var input = field.Find("input.omni-upload__input");
        Assert.Equal("contacts", input.Id);
        Assert.False(input.HasAttribute("aria-label"));
        Assert.Equal("contacts", field.Find("label.omni-label").GetAttribute("for"));
    }

    [Fact]
    public void WithAnId_APageLabelNamesTheControl_NoPromptOverridesIt()
    {
        var upload = Render<OmniUpload>(parameters => parameters.Add(component => component.Id, "supporting"));

        var input = upload.Find("input.omni-upload__input");
        Assert.Equal("supporting", input.Id);
        Assert.False(input.HasAttribute("aria-label"));
    }
}
