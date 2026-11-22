namespace OmniEurope.Blazor.Tests;

/// <summary>Finds the repository checkout the tests run from, by walking up to the solution file.</summary>
internal static class RepositoryRootLocator
{
    /// <summary>The directory that holds <c>OmniEurope.Blazor.slnx</c>, above the test binaries.</summary>
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
