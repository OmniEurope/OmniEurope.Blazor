using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The repository and the package are public (PLAN-016): no tracked file names a client application,
/// an internal development or assistant tool, or carries a local path. The terms are not written here:
/// each word of a tracked file is compared by its SHA-256 footprint, lowercase. The licence texts of
/// third-party dependencies are left out, their wording being theirs.
/// </summary>
public sealed class PublicContentGuardTests
{
    private static readonly HashSet<string> Forbidden =
    [
        "7b2a545f668dc000ccdc28f9854bc2c3b5e8dd3db821b0cde305b30545b81bec",
        "129f27fec540f3c55a0252b650e31092c69cbfc9e067834fd7299fc5f5c4ee5e",
        "c4c4726bcedece3ac3669d104975bfd50dbb867c4e4903b0042e9f8e71460983",
        "2725a1e33ec7b7701a41f7ba429a9f06b8f723e2238ec0b04fe572b0748daf18",
        "887c426cb1bedae2bd50f98546bd7bf8ffd03152d0e9db4c29aac6ae1fa418f7",
        "f926d1474f88ba7da2a88d0d2b1ff2bc476d12304ff0cf3bab2af2c883b29715",
        "36b37a3092f9c25c64d00f1d2182605c5b3e0b212f60133bca4f3307df1f8982",
        "152570e47cef2216695a3096056f75687cf75692024ad357a68a0e2718fea3b3",
        "375398159e1de5168ac517b8b52d3480805bb772841938b416048dec1c94579a",
        "180f08d97cffc5f9c8dd9f157e2aa92d2973c6cbb8894130ebbbc380a122e87c",
        "7c82602500857aa6ed0cf38c4c3e4ec645bdcaa82c00b9155eb08be100c778a9",
        "0e6a8e0b849ed9b064c5a25e1ee5592f427e3eb9d250e42069ce46147d00e8d4",
        "3331440021fd765074866ea8ff86fe006dbfc3fbe98e0b3508ae54525b80346d",
        "dcaaa90a966cff005e869c4db95f2fc2ea6bf77fd33110890acd0e1837c9161b",
        "b6fe01ea3299211dd5133e5ac2027f4bca85d97407d41b4a68359c07d7db5d71",
        "39f00cba331f3369a79d7359989d86cf3e04d03dcd4a95d0cef5a933b031ee8e",
        "9ad314ce9d6ecfe0db21b2d6a70609e15f95f5fb1718933ba318e0d8d2c9a203",
        "57de4cf40144bdf7d00010f2f5557a7d642c2b9705309bfade167dd313e2ca93",
        "c857d09db23e6822e3600bc06ad8d58f92ed62bc8efd81c753f77048662cb97d",
        "3ea125d0bff386e6754b3782b300016fc79a9cf8f8669c0a5c3db64467ddb681",
        "c70eca6b0f88f44d81a41311647e50fda1ac454ec04ffd442b0eb4743a993131",
        "7d3194f79e645c42e4396dda38be04766810ec6a00d00aced3ffc2a0a1f1a9ef",
        "f1eb4326d6e2c593485e339d9b9991c6e87f16f36baf4bbc691d897b171289ab",
    ];

    // A word: letters, digits and underscores of any script, so a name glued to a Latvian or Greek
    // ending is the longer word and not the name.
    private static readonly Regex Word = new(@"[\p{L}\p{N}_]+", RegexOptions.CultureInvariant);

    private static readonly Regex LocalPath = new(@"[A-Za-z]:[\\/]+(?:Users|Dev)[\\/]|\bvps\d{4,}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly string[] Excluded = ["docs/third-party-licenses/", "NOTICE.md"];

    [Fact]
    public void NoTrackedFile_NamesAForbiddenTerm_OrALocalPath()
    {
        var findings = new List<string>();
        var known = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var file in TrackedFiles())
        {
            if (Excluded.Any(prefix => file.StartsWith(prefix, StringComparison.Ordinal)))
            {
                continue;
            }

            findings.AddRange(Findings(file, Forbidden, known).Select(finding => $"{file}: {finding} (path)"));
            var full = Path.Combine(Root, file);
            if (!File.Exists(full))
            {
                continue;
            }

            var bytes = File.ReadAllBytes(full);
            if (Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8000)) >= 0)
            {
                continue;
            }

            var lines = Encoding.UTF8.GetString(bytes).Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                findings.AddRange(Findings(lines[index], Forbidden, known).Select(finding => $"{file}:{index + 1}: {finding}"));
            }
        }

        Assert.True(findings.Count == 0, "Terme interdit dans un fichier suivi :\n" + string.Join('\n', findings.Take(50)));
    }

    [Fact]
    public void TheGuard_FindsAHashedWord_AndALocalPath_ButNotTheLongerWord()
    {
        var hashes = new HashSet<string>(StringComparer.Ordinal) { Footprint("zzinterditzz") };
        var known = new Dictionary<string, bool>(StringComparer.Ordinal);

        Assert.Equal(["word #3"], Findings("Demande de ZZinterditZZ (PLAN-099).", hashes, known));
        Assert.Empty(Findings("zzinterditzzā et zzinterditzz2 restent des mots plus longs.", hashes, known));
        // Built at run time, so this file holds no local path of its own.
        Assert.Equal(["local path"], Findings("Voir C:" + @"\Users\exemple\notes.md", hashes, known));
        Assert.Equal(["local path"], Findings("serveur vps" + "123456", hashes, known));
    }

    private static IEnumerable<string> Findings(string text, HashSet<string> hashes, Dictionary<string, bool> known)
    {
        var word = 0;
        foreach (Match match in Word.Matches(text))
        {
            word++;
            var lower = match.Value.ToLowerInvariant();
            if (!known.TryGetValue(lower, out var forbidden))
            {
                forbidden = hashes.Contains(Footprint(lower));
                known[lower] = forbidden;
            }

            if (forbidden)
            {
                yield return $"word #{word}";
            }
        }

        if (LocalPath.IsMatch(text))
        {
            yield return "local path";
        }
    }

    private static string Footprint(string word) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(word.ToLowerInvariant())));

    private static IReadOnlyList<string> TrackedFiles()
    {
        using var git = Process.Start(new ProcessStartInfo("git", ["-C", Root, "ls-files", "-z"])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8
        }) ?? throw new InvalidOperationException("git ls-files ne démarre pas.");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);
        var files = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        // A run outside a checkout would prove nothing: the repository holds well over a thousand files.
        Assert.True(files.Length > 500, $"git ls-files ne rend que {files.Length} fichiers.");
        return files;
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Racine du dépôt introuvable.");
        }
    }
}
