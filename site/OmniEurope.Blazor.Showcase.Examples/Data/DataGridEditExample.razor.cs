using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Two editable grids: edited by a button or a double click, and edited by a click on a cell.</summary>
public partial class DataGridEditExample
{
    private OmniDataGrid<EditableFile>? _editGrid;

    // The clicks the Details button of a Country cell received: a click on it must not open the row.
    private int _cellEditDetails;

    // The values a row had when it entered edit mode: the editors write into the file itself, so a
    // cancelled edit is undone by the example from this copy (the grid only reports the cancellation).
    private readonly Dictionary<string, (string Applicant, string Country)> _originals = [];

    // Kept while the example is shown: an edit changes the file itself, as a host's model would.
    private readonly List<EditableFile> _editableFiles =
    [
        new() { Reference = "D-1001", Applicant = "Marie Lambert", Country = "Belgium" },
        new() { Reference = "D-1002", Applicant = "Jonas Weber", Country = "Germany" },
        new() { Reference = "D-1003", Applicant = "Inês Costa", Country = "Portugal" },
    ];

    // The Country of the second file is empty on purpose: a cell with nothing to read is still there to be clicked.
    private readonly List<EditableFile> _cellEditFiles =
    [
        new() { Reference = "D-2001", Applicant = "Camille Durand", Country = "Belgium" },
        new() { Reference = "D-2002", Applicant = "Sofia Rossi", Country = string.Empty },
        new() { Reference = "D-2003", Applicant = "Lars Jansen", Country = "Netherlands" },
    ];

    private void CountCellEditDetails() => _cellEditDetails++;

    private void KeepOriginal(EditableFile file) => _originals[file.Reference] = (file.Applicant, file.Country);

    private void RestoreOriginal(EditableFile file)
    {
        if (_originals.Remove(file.Reference, out var original))
        {
            (file.Applicant, file.Country) = original;
        }
    }

    /// <summary>A file of the editable grids: its applicant and country change in place.</summary>
    public sealed class EditableFile
    {
        /// <summary>The file reference.</summary>
        public required string Reference { get; init; }

        /// <summary>The applicant name.</summary>
        public string Applicant { get; set; } = string.Empty;

        /// <summary>The country the file belongs to.</summary>
        public string Country { get; set; } = string.Empty;
    }
}
