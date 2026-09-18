using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Reads a unified diff, as <c>git diff</c>, <c>git show</c> or <c>diff -u</c> write it, into files, hunks
/// and numbered lines. A bare hunk without file headers, the patch of a single file some APIs return,
/// becomes one file without a path.
/// </summary>
/// <remarks>
/// Hunk lines are counted against their <c>@@</c> header, so a removed line that itself starts with
/// <c>--</c> is not mistaken for the header of the next file. Lines outside any file or hunk (a commit
/// message, a mail header) are skipped. Binary files are reported with no hunk.
/// </remarks>
public static partial class OmniUnifiedDiffParser
{
    private const string DevNull = "/dev/null";

    /// <summary>The files of <paramref name="diff"/>, in order; none for an empty text.</summary>
    public static IReadOnlyList<OmniDiffFile> Parse(string? diff)
    {
        var files = new List<OmniDiffFile>();
        if (string.IsNullOrEmpty(diff))
        {
            return files;
        }

        FileBuilder? file = null;
        HunkBuilder? hunk = null;
        var oldLeft = 0;
        var newLeft = 0;

        void Flush()
        {
            if (file is not null)
            {
                files.Add(file.Build());
            }

            file = null;
            hunk = null;
            oldLeft = 0;
            newLeft = 0;
        }

        foreach (var raw in diff.Split('\n'))
        {
            var line = raw.EndsWith('\r') ? raw[..^1] : raw;

            if (hunk is not null && (oldLeft > 0 || newLeft > 0))
            {
                // Inside a hunk, the header counts say what each line is. An empty line stands for an
                // unchanged empty line whose leading space a tool or an editor trimmed.
                var marker = line.Length == 0 ? ' ' : line[0];
                var text = line.Length == 0 ? string.Empty : line[1..];
                if (marker == ' ')
                {
                    hunk.Add(OmniDiffLineKind.Context, text, true, true);
                    oldLeft--;
                    newLeft--;
                    continue;
                }

                if (marker == '+')
                {
                    hunk.Add(OmniDiffLineKind.Added, text, false, true);
                    newLeft--;
                    continue;
                }

                if (marker == '-')
                {
                    hunk.Add(OmniDiffLineKind.Removed, text, true, false);
                    oldLeft--;
                    continue;
                }

                if (marker == '\\')
                {
                    hunk.Add(OmniDiffLineKind.Note, line[1..].Trim(), false, false);
                    continue;
                }

                // Shorter than its header declared: the hunk ends here and the line is read as a header.
                oldLeft = 0;
                newLeft = 0;
            }

            if (hunk is not null && line.StartsWith('\\'))
            {
                hunk.Add(OmniDiffLineKind.Note, line[1..].Trim(), false, false);
                continue;
            }

            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                Flush();
                file = FileBuilder.FromGitHeader(line["diff --git ".Length..]);
                continue;
            }

            if (HunkHeader().Match(line) is { Success: true } header)
            {
                file ??= new FileBuilder();
                hunk = file.StartHunk(
                    line,
                    Number(header.Groups[1]),
                    header.Groups[2].Success ? Number(header.Groups[2]) : 1,
                    Number(header.Groups[3]),
                    header.Groups[4].Success ? Number(header.Groups[4]) : 1);
                oldLeft = hunk.OldCount;
                newLeft = hunk.NewCount;
                continue;
            }

            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                if (file is null || file.HasHunks)
                {
                    Flush();
                    file = new FileBuilder();
                }

                file.SetOldPath(PathOf(line[4..]));
                continue;
            }

            if (line.StartsWith("+++ ", StringComparison.Ordinal) && file is not null && !file.HasHunks)
            {
                file.SetNewPath(PathOf(line[4..]));
                continue;
            }

            if (file is not null && !file.HasHunks)
            {
                file.ReadExtendedHeader(line);
            }
        }

        Flush();
        return files;
    }

    private static int Number(Group group) => int.Parse(group.ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture);

    /// <summary>The path of a <c>---</c> or <c>+++</c> header: quotes, the timestamp of <c>diff -u</c> and the <c>a/</c> or <c>b/</c> prefix removed.</summary>
    private static string? PathOf(string value)
    {
        var path = value;
        var tab = path.IndexOf('\t', StringComparison.Ordinal);
        if (tab >= 0)
        {
            path = path[..tab];
        }

        path = path.Trim().Trim('"');
        if (path == DevNull)
        {
            return null;
        }

        return StripSide(path);
    }

    private static string StripSide(string path) =>
        path.Length > 2 && (path.StartsWith("a/", StringComparison.Ordinal) || path.StartsWith("b/", StringComparison.Ordinal))
            ? path[2..]
            : path;

    [GeneratedRegex("^@@ -(\\d+)(?:,(\\d+))? \\+(\\d+)(?:,(\\d+))? @@", RegexOptions.CultureInvariant)]
    private static partial Regex HunkHeader();

    private sealed class FileBuilder
    {
        private readonly List<HunkBuilder> _hunks = [];
        private bool _added;
        private bool _deleted;
        private bool _renamed;
        private bool _binary;
        private string? _oldPath;
        private string? _newPath;

        internal bool HasHunks => _hunks.Count > 0;

        internal static FileBuilder FromGitHeader(string paths)
        {
            // "a/old b/new": the last " b/" separates the sides, which holds for paths with spaces. The
            // ---, +++ and rename lines that follow, when present, are more precise and replace them.
            var builder = new FileBuilder();
            var separator = paths.LastIndexOf(" b/", StringComparison.Ordinal);
            if (separator > 0)
            {
                builder._oldPath = StripSide(paths[..separator].Trim('"'));
                builder._newPath = StripSide(paths[(separator + 1)..].Trim('"'));
            }

            return builder;
        }

        internal void SetOldPath(string? path)
        {
            _oldPath = path;
            _added |= path is null;
        }

        internal void SetNewPath(string? path)
        {
            _newPath = path;
            _deleted |= path is null;
        }

        internal void ReadExtendedHeader(string line)
        {
            if (line.StartsWith("new file mode", StringComparison.Ordinal))
            {
                _added = true;
            }
            else if (line.StartsWith("deleted file mode", StringComparison.Ordinal))
            {
                _deleted = true;
            }
            else if (line.StartsWith("rename from ", StringComparison.Ordinal))
            {
                _renamed = true;
                _oldPath = line["rename from ".Length..].Trim('"');
            }
            else if (line.StartsWith("rename to ", StringComparison.Ordinal))
            {
                _renamed = true;
                _newPath = line["rename to ".Length..].Trim('"');
            }
            else if (line.StartsWith("Binary files ", StringComparison.Ordinal) || line.StartsWith("GIT binary patch", StringComparison.Ordinal))
            {
                _binary = true;
            }
        }

        internal HunkBuilder StartHunk(string header, int oldStart, int oldCount, int newStart, int newCount)
        {
            var hunk = new HunkBuilder(header, oldStart, oldCount, newStart, newCount);
            _hunks.Add(hunk);
            return hunk;
        }

        internal OmniDiffFile Build()
        {
            var status = _added ? OmniDiffFileStatus.Added
                : _deleted ? OmniDiffFileStatus.Deleted
                : _renamed ? OmniDiffFileStatus.Renamed
                : OmniDiffFileStatus.Modified;
            return new OmniDiffFile(
                status == OmniDiffFileStatus.Added ? null : _oldPath,
                status == OmniDiffFileStatus.Deleted ? null : _newPath,
                status,
                _binary,
                [.. _hunks.Select(hunk => hunk.Build())]);
        }
    }

    private sealed class HunkBuilder(string header, int oldStart, int oldCount, int newStart, int newCount)
    {
        private readonly List<OmniDiffLine> _lines = [];
        private int _oldOffset;
        private int _newOffset;

        internal int OldCount => oldCount;

        internal int NewCount => newCount;

        internal void Add(OmniDiffLineKind kind, string text, bool countsOld, bool countsNew)
        {
            _lines.Add(new OmniDiffLine(kind, text, countsOld ? oldStart + _oldOffset++ : null, countsNew ? newStart + _newOffset++ : null));
        }

        internal OmniDiffHunk Build() => new(header, oldStart, oldCount, newStart, newCount, _lines);
    }
}
