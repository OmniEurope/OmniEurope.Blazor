namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The commands, diffs and configurations the code reading examples show.</summary>
public static class CodeReadingSamples
{
    /// <summary>The command before the change.</summary>
    public const string OriginalCommand = "dotnet build App.slnx";

    /// <summary>The command after the change.</summary>
    public const string UpdatedCommand = "dotnet test App.slnx";

    /// <summary>The line the excerpt viewer numbers its first line from, as in the file the excerpt comes from.</summary>
    public const int ExcerptFirstLine = 120;

    /// <summary>The unified diff of the command change, as <c>git diff</c> writes it.</summary>
    public const string CommandDiff = "--- a/command.txt\n+++ b/command.txt\n@@ -1 +1 @@\n-dotnet build App.slnx\n+dotnet test App.slnx\n";

    /// <summary>A configuration before the change.</summary>
    public const string OriginalConfiguration = "{\n  \"mode\": \"old\"\n}";

    /// <summary>A configuration after the change.</summary>
    public const string ChangedConfiguration = "{\n  \"mode\": \"new\"\n}";

    /// <summary>The command to show: the modified one or the original.</summary>
    /// <param name="changed">Whether the modified command is wanted.</param>
    /// <returns>The command text.</returns>
    public static string Command(bool changed) => changed ? UpdatedCommand : OriginalCommand;
}
