using System.Reflection;
using System.Text.RegularExpressions;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Holds <see cref="OmniModules"/> to the files it names: every constant is a module of the library's
/// <c>wwwroot</c>, and every module address still written out in the library has its constant.
/// </summary>
public sealed partial class OmniModulesTests
{
    private const string Prefix = "./_content/OmniEurope.Blazor/";

    private static IReadOnlyList<string> Constants =>
    [
        .. typeof(OmniModules).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(constant => constant is { IsLiteral: true } && constant.Name != "Root")
            .Select(constant => (string)constant.GetRawConstantValue()!)
    ];

    [Fact]
    public void EveryConstant_NamesAFileOfTheLibraryWebRoot()
    {
        var webRoot = Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot");

        Assert.NotEmpty(Constants);
        Assert.Equal(Constants.Count, Constants.Distinct(StringComparer.Ordinal).Count());
        foreach (var module in Constants)
        {
            Assert.StartsWith(Prefix, module, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(webRoot, module[Prefix.Length..])), $"{module} names no file of wwwroot.");
        }
    }

    [Fact]
    public void EveryModuleAddressWrittenInTheLibrary_HasItsConstant()
    {
        var source = Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor");
        var written = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".razor")
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => ModuleAddress().Matches(File.ReadAllText(path)).Select(match => match.Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var missing = written.Except(Constants, StringComparer.Ordinal).ToArray();
        Assert.True(missing.Length == 0, $"Module addresses with no OmniModules constant: {string.Join(", ", missing)}");
    }

    [GeneratedRegex(@"\./_content/OmniEurope\.Blazor/[A-Za-z0-9-]+\.js")]
    private static partial Regex ModuleAddress();
}
