using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Stat tiles in the columns of a row, one clickable and one whose long title is cut.</summary>
public partial class StatTilesColumnsExample
{
    private static string Tokens => $"{12.4.ToString("N1", CultureInfo.CurrentCulture)}k";

    private static string Reset => $"Resets at {new TimeOnly(18, 0).ToString("t", CultureInfo.CurrentCulture)}";

    private static string WeeklyShare => 0.83.ToString("P0", CultureInfo.CurrentCulture);

    private static string Gain => 1284.ToString("C0", CultureInfo.CurrentCulture);
}
