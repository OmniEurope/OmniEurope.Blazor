using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Declares its grid columns in a <c>@foreach</c>, the way a consumer builds columns from a list:
/// every render creates new delegates that capture the loop variable. <see cref="CellRenders"/>
/// counts the template calls (and, in <see cref="CaptureValue"/> mode, the value reads) and throws past <see cref="RenderBound"/>, so a column that
/// re-registers on every render (an endless render loop) fails the test instead of hanging it.
/// </summary>
public partial class DataGridLoopColumnsTestHost
{
    public const int RenderBound = 400;

    [Parameter]
    public bool CaptureValue { get; set; }
    public int CellRenders { get; private set; }

    public List<ColumnSpec> Columns { get; } =
    [
        new("name", "Nom", nameof(Row.Name), null),
        new("city", "Ville", nameof(Row.City), null)
    ];

    public IReadOnlyList<Row> Rows { get; } =
    [
        new(1, "Alice", "Bruxelles"),
        new(2, "Bob", "Namur")
    ];

    public void ChangeColumn(int index, ColumnSpec spec)
    {
        Columns[index] = spec;
        StateHasChanged();
    }

    public void Rerender() => StateHasChanged();

    private string Cell(ColumnSpec column, Row row)
    {
        CountRender();
        return row.Read(column.Property)?.ToString() ?? string.Empty;
    }

    private object? ReadValue(ColumnSpec column, Row row)
    {
        CountRender();
        return row.Read(column.Property);
    }

    private void CountRender()
    {
        CellRenders++;
        if (CellRenders > RenderBound)
        {
            throw new InvalidOperationException($"Render loop: the column templates ran more than {RenderBound} times.");
        }
    }

    public sealed record ColumnSpec(string Key, string Title, string Property, string? Width);

    public sealed record Row(int Id, string Name, string City)
    {
        public object? Read(string property) => property switch
        {
            nameof(Name) => Name,
            nameof(City) => City,
            _ => null
        };
    }
}
