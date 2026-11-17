namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>One node of the archive shown as a large virtualized tree grid: a folder or a file.</summary>
/// <param name="Id">The stable key of the node.</param>
/// <param name="Name">The displayed name.</param>
/// <param name="Kind">Folder or File.</param>
/// <param name="Size">The size in kilobytes.</param>
/// <param name="Children">The nodes under it, none for a file.</param>
public sealed record ArchiveNode(int Id, string Name, string Kind, int Size, IReadOnlyList<ArchiveNode>? Children = null)
{
    /// <summary>The number of folders at the first level of <see cref="Sample"/>.</summary>
    public const int Folders = 50;

    /// <summary>The number of files in each folder.</summary>
    public const int FilesPerFolder = 99;

    /// <summary>Every node of <see cref="Sample"/>: the folders and their files, 5 000 in all.</summary>
    public const int Total = Folders * (1 + FilesPerFolder);

    /// <summary>The sample archive: fifty folders of ninety-nine files each.</summary>
    public static IReadOnlyList<ArchiveNode> Sample { get; } =
    [
        .. Enumerable.Range(0, Folders).Select(folder => new ArchiveNode(
            folder * 100,
            $"Folder {folder + 1}",
            "Folder",
            FilesPerFolder * 12,
            [.. Enumerable.Range(1, FilesPerFolder).Select(file => new ArchiveNode((folder * 100) + file, $"File {folder + 1}.{file}", "File", 12))]))
    ];
}
