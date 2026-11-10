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
        var reader = new DiffReader();
        if (string.IsNullOrEmpty(diff))
        {
            return reader.Files;
        }

        foreach (var raw in diff.Split('\n'))
        {
            reader.Read(raw.EndsWith('\r') ? raw[..^1] : raw);
        }

        reader.Flush();
        return reader.Files;
    }

    /// <summary>The reading state of one diff: the file and the hunk being filled, and the lines each side of the hunk still owes.</summary>
    private sealed class DiffReader
    {
        private FileBuilder? _file;
        private HunkBuilder? _hunk;
        private int _oldLeft;
        private int _newLeft;

        internal List<OmniDiffFile> Files { get; } = [];

        internal void Read(string line)
        {
            if (_hunk is not null && (_oldLeft > 0 || _newLeft > 0) && ReadBodyLine(_hunk, line))
            {
                return;
            }

            if (_hunk is not null && line.StartsWith('\\'))
            {
                _hunk.Add(OmniDiffLineKind.Note, line[1..].Trim(), false, false);
                return;
            }

            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                Flush();
                _file = FileBuilder.FromGitHeader(line["diff --git ".Length..]);
                return;
            }

            var header = HunkHeader().Match(line);
            if (header.Success)
            {
                StartHunk(line, header);
                return;
            }

            ReadFileHeader(line);
        }

        internal void Flush()
        {
            if (_file is not null)
            {
                Files.Add(_file.Build());
            }

            _file = null;
            _hunk = null;
            EndHunkBody();
        }

        private void EndHunkBody()
        {
            _oldLeft = 0;
            _newLeft = 0;
        }

        /// <summary>
        /// Inside a hunk, the header counts say what each line is. An empty line stands for an unchanged empty
        /// line whose leading space a tool or an editor trimmed. False when the line is shorter than its header
        /// declared: the hunk ends there and the line is read as a header.
        /// </summary>
        private bool ReadBodyLine(HunkBuilder hunk, string line)
        {
            var marker = line.Length == 0 ? ' ' : line[0];
            var text = line.Length == 0 ? string.Empty : line[1..];
            switch (marker)
            {
                case ' ':
                    hunk.Add(OmniDiffLineKind.Context, text, true, true);
                    _oldLeft--;
                    _newLeft--;
                    return true;
                case '+':
                    hunk.Add(OmniDiffLineKind.Added, text, false, true);
                    _newLeft--;
                    return true;
                case '-':
                    hunk.Add(OmniDiffLineKind.Removed, text, true, false);
                    _oldLeft--;
                    return true;
                case '\\':
                    hunk.Add(OmniDiffLineKind.Note, line[1..].Trim(), false, false);
                    return true;
                default:
                    EndHunkBody();
                    return false;
            }
        }

        private void StartHunk(string line, Match header)
        {
            // The numbers come from external text: a range that does not fit an int, line numbers
            // included, makes the header malformed, and the hunk is skipped rather than thrown on.
            if (!TryRange(header.Groups[1], header.Groups[2], out var oldStart, out var oldCount)
                || !TryRange(header.Groups[3], header.Groups[4], out var newStart, out var newCount))
            {
                _hunk = null;
                EndHunkBody();
                return;
            }

            _file ??= new FileBuilder();
            _hunk = _file.StartHunk(line, oldStart, oldCount, newStart, newCount);
            _oldLeft = _hunk.OldCount;
            _newLeft = _hunk.NewCount;
        }

        /// <summary>The <c>---</c> and <c>+++</c> lines and the extended header lines (mode, rename, binary) that precede the hunks of a file.</summary>
        private void ReadFileHeader(string line)
        {
            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                if (_file is null || _file.HasHunks)
                {
                    Flush();
                    _file = new FileBuilder();
                }

                _file.SetOldPath(PathOf(line[4..]));
                return;
            }

            if (_file is null || _file.HasHunks)
            {
                return;
            }

            if (line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                _file.SetNewPath(PathOf(line[4..]));
                return;
            }

            _file.ReadExtendedHeader(line);
        }
    }

    /// <summary>
    /// The start and count of one side of a hunk header, a missing count meaning 1; false when either
    /// does not fit an int or the last line of the range would not.
    /// </summary>
    private static bool TryRange(Group startGroup, Group countGroup, out int start, out int count)
    {
        count = 1;
        return int.TryParse(startGroup.ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out start)
            && (!countGroup.Success || int.TryParse(countGroup.ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out count))
            && (long)start + count <= int.MaxValue;
    }

    /// <summary>The path of a <c>---</c> or <c>+++</c> header: quotes, the timestamp of <c>diff -u</c> and the <c>a/</c> or <c>b/</c> prefix removed.</summary>
    private static string? PathOf(string value)
    {
        var path = value;
        var tab = path.IndexOf('\t', StringComparison.Ordinal);
        if (tab >= 0)
        {
            path = path[..tab];
        }

        path = GitQuotedPath.Unquote(path.Trim());
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
            // A side Git quoted (a name past ASCII, core.quotePath) is read as a C string.
            var builder = new FileBuilder();
            if (GitQuotedPath.TrySplit(paths, out var oldPath, out var newPath))
            {
                builder._oldPath = StripSide(oldPath);
                builder._newPath = StripSide(newPath);
                return builder;
            }

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
                _oldPath = GitQuotedPath.Unquote(line["rename from ".Length..]);
            }
            else if (line.StartsWith("rename to ", StringComparison.Ordinal))
            {
                _renamed = true;
                _newPath = GitQuotedPath.Unquote(line["rename to ".Length..]);
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
