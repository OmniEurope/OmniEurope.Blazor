using Microsoft.Extensions.DependencyInjection;
using Bunit;
using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniValueFormatter"/>: base-1024 sizes with the unit and decimal separator of the culture, and the
/// duration rule (seconds, minutes, hours and minutes, days and hours).
/// </summary>
public sealed class FormatTests : OmniBunitContext
{
    private OmniValueFormatter Formatter => Services.GetRequiredService<OmniValueFormatter>();

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Theory]
    [InlineData(0L, "0 o")]
    [InlineData(512L, "512 o")]
    [InlineData(1023L, "1023 o")]
    [InlineData(1024L, "1 Ko")]
    [InlineData(1536L, "1,5 Ko")]
    [InlineData(2000L, "2 Ko")]
    [InlineData(1_258_291L, "1,2 Mo")]
    [InlineData(1_048_575L, "1 Mo")]
    [InlineData(1_048_576L, "1 Mo")]
    [InlineData(3_931_000_000L, "3,7 Go")]
    [InlineData(1_099_511_627_776L, "1 To")]
    [InlineData(5_000_000_000_000_000L, "4547,5 To")]
    public void FileSize_InFrench_UsesBase1024AndFrenchUnits(long bytes, string expected) =>
        Assert.Equal(expected, Formatter.FileSize(bytes, French));

    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(3_931_000_000L, "3.7 GB")]
    [InlineData(1_099_511_627_776L, "1 TB")]
    public void FileSize_InEnglish_UsesEnglishUnitsAndDecimalPoint(long bytes, string expected) =>
        Assert.Equal(expected, Formatter.FileSize(bytes, English));

    [Fact]
    public void FileSize_Negative_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Formatter.FileSize(-1, French));

    [Theory]
    [InlineData("de", "1,5 KB")]
    [InlineData("fi", "1,5 kt")]
    [InlineData("pl", "1,5 KB")]
    public void FileSize_ReadsTheUnitAndSeparatorOfAnotherLanguage(string name, string expected) =>
        Assert.Equal(expected, Formatter.FileSize(1536, CultureInfo.GetCultureInfo(name)));

    [Fact]
    public void FileSize_WithRegionalCulture_FallsBackToTheNeutralLanguage() =>
        Assert.Equal("1,5 KB", Formatter.FileSize(1536, CultureInfo.GetCultureInfo("de-AT")));

    [Fact]
    public void FileSize_WithUnshippedCulture_FallsBackToTheFrenchSymbolsAndKeepsItsSeparator() =>
        Assert.Equal("1.5 Ko", Formatter.FileSize(1536, CultureInfo.GetCultureInfo("ja-JP")));

    [Fact]
    public void FileSize_WithNoCulture_UsesTheCurrentUiCulture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = English;
            Assert.Equal("1.5 KB", Formatter.FileSize(1536));
            CultureInfo.CurrentUICulture = French;
            Assert.Equal("1,5 Ko", Formatter.FileSize(1536));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData(0, "0s")]
    [InlineData(45, "45s")]
    [InlineData(59, "59s")]
    [InlineData(60, "1min")]
    [InlineData(61, "1min1s")]
    [InlineData(599, "9min59s")]
    [InlineData(600, "10min")]
    [InlineData(2532, "42min")]
    [InlineData(3599, "59min")]
    [InlineData(3600, "1h")]
    [InlineData(3659, "1h")]
    [InlineData(3660, "1h1min")]
    [InlineData(6260, "1h44min")]
    [InlineData(86399, "23h59min")]
    [InlineData(86400, "1j")]
    [InlineData(93600, "1j2h")]
    [InlineData(259200, "3j")]
    public void Duration_InFrench_FollowsTheRule(int seconds, string expected) =>
        Assert.Equal(expected, Formatter.Duration(TimeSpan.FromSeconds(seconds), French));

    [Theory]
    [InlineData(45, "45s")]
    [InlineData(2532, "42min")]
    [InlineData(6260, "1h44min")]
    [InlineData(93600, "1d2h")]
    public void Duration_InEnglish_UsesEnglishSymbols(int seconds, string expected) =>
        Assert.Equal(expected, Formatter.Duration(TimeSpan.FromSeconds(seconds), English));

    [Fact]
    public void Duration_TruncatesTheFractionOfASecond()
    {
        Assert.Equal("59s", Formatter.Duration(TimeSpan.FromMilliseconds(59_999), French));
        Assert.Equal("1min", Formatter.Duration(TimeSpan.FromMilliseconds(60_000), French));
    }

    [Fact]
    public void Duration_ReadsTheSymbolsOfAnotherLanguage()
    {
        Assert.Equal("1h44min", Formatter.Duration(TimeSpan.FromSeconds(6260), CultureInfo.GetCultureInfo("de")));
        Assert.Equal("1ч44мин", Formatter.Duration(TimeSpan.FromSeconds(6260), CultureInfo.GetCultureInfo("bg")));
        Assert.Equal("1j", Formatter.Duration(TimeSpan.FromDays(1), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Duration_WithoutACulture_ReadsTheCurrentUICulture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = French;
            Assert.Equal("1j2h", Formatter.Duration(TimeSpan.FromHours(26)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Duration_Negative_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Formatter.Duration(TimeSpan.FromSeconds(-1), French));
}

/// <summary><see cref="OmniUpload"/> writes its sizes through <see cref="OmniValueFormatter.FileSize(long, CultureInfo?)"/>, so it reaches gigabytes.</summary>
public sealed class UploadSizeFormatTests : OmniBunitContext
{
    [Fact]
    public void Upload_ShowsAGigabyteFileInGigabytes()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            IReadOnlyList<OmniUploadFile> files = [new("image.iso", 3_931_000_000, "application/octet-stream")];
            var upload = Render<OmniUpload>(parameters => parameters
                .Add(component => component.Multiple, true)
                .Add(component => component.Files, files)
                .Add(component => component.FilesChanged, next => files = next));

            Assert.Equal("3,7 Go", upload.Find(".omni-upload__file-size").TextContent);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
