namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A form whose rules are the DataAnnotations of its model.</summary>
public partial class AnnotationsValidationExample
{
    private ContactModel Contact { get; } = new();

    private bool Sent { get; set; }

    private void Submit() => Sent = true;
}
