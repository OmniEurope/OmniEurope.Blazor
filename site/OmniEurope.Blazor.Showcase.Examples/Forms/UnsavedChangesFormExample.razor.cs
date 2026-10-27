namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A form that guards its own unsaved changes.</summary>
public partial class UnsavedChangesFormExample
{
    private NoteModel Note { get; } = new();

    private string? SavedNote { get; set; }

    private void SaveNote() => SavedNote = Note.Text;
}
