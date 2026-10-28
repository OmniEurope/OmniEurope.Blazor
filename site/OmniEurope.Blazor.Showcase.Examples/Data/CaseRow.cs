namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>One file of the data grid examples.</summary>
/// <param name="Reference">The file reference.</param>
/// <param name="Applicant">The applicant name.</param>
/// <param name="Country">The country the file belongs to.</param>
/// <param name="Amount">The amount claimed.</param>
/// <param name="Filed">When the file was filed.</param>
public sealed record CaseRow(string Reference, string Applicant, string Country, decimal Amount, DateTime Filed = default);

/// <summary>The sample files of the data grid examples.</summary>
public static class CaseData
{
    // An organisation's name, kept as written: a long value that shows the ellipsis.
    private const string LongApplicant = "Établissement public de coopération intercommunale du Grand Est";

    /// <summary>The countries the sample files belong to.</summary>
    public static IReadOnlyList<string> Countries { get; } = ["Belgium", "France", "Luxembourg", "Netherlands"];

    /// <summary>Seven files, each with the time it was filed.</summary>
    public static IReadOnlyList<CaseRow> Rows { get; } =
    [
        new("D-2401", "Camille Durand", "Belgium", 12400, new DateTime(2026, 3, 2, 9, 15, 0)),
        new("D-2402", "Jonas Meyer", "Luxembourg", 8600, new DateTime(2026, 3, 2, 17, 40, 0)),
        new("D-2403", "Sofia Rossi", "France", 21500, new DateTime(2026, 3, 9, 11, 5, 0)),
        new("D-2404", "Lars Jansen", "Netherlands", 4300, new DateTime(2026, 3, 12, 8, 30, 0)),
        new("D-2405", "Ana Silva", "France", 17800, new DateTime(2026, 3, 16, 14, 0, 0)),
        new("D-2406", "Piet de Vries", "Netherlands", 9100, new DateTime(2026, 3, 20, 10, 45, 0)),
        new("D-2407", "Marie Lambert", "Belgium", 15200, new DateTime(2026, 3, 23, 16, 20, 0))
    ];

    /// <summary>Three files: short enough to stay under any height cap.</summary>
    public static IReadOnlyList<CaseRow> Few { get; } = [.. Rows.Take(3)];

    /// <summary>Three files, the first with a very long applicant.</summary>
    public static IReadOnlyList<CaseRow> Narrow { get; } = [Rows[0] with { Applicant = LongApplicant }, .. Rows.Skip(1).Take(2)];

    /// <summary>Ten thousand files; the applicant of one of them, far outside the first window, is very long.</summary>
    public static IReadOnlyList<CaseRow> Many { get; } =
    [
        .. Enumerable.Range(1, 10_000)
            .Select(index => Rows[index % Rows.Count] with
            {
                Reference = $"D-{index:00000}",
                Applicant = index == 7_777 ? LongApplicant : Rows[index % Rows.Count].Applicant
            })
    ];

    /// <summary>
    /// Two thousand files; one amount, far from the first window, is much wider than the others, so the fit
    /// to content of the amount has to rank every text, rendered or not.
    /// </summary>
    public static IReadOnlyList<CaseRow> Fill { get; } =
    [
        .. Enumerable.Range(1, 2_000)
            .Select(index => Rows[index % Rows.Count] with
            {
                Reference = $"F-{index:0000}",
                Amount = index == 1_999 ? 123_456_789_012_345_678m : 1_000m + (index * 37m)
            })
    ];
}
