namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// One row of the data grid demonstration.
/// </summary>
/// <param name="Reference">The file reference.</param>
/// <param name="Applicant">The applicant name.</param>
/// <param name="Country">The country the file belongs to.</param>
/// <param name="Amount">The amount claimed.</param>
/// <param name="Filed">When the file was filed.</param>
public sealed record GridRow(string Reference, string Applicant, string Country, decimal Amount, DateTime Filed = default);
