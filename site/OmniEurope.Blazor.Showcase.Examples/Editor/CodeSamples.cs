namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>Code samples for the code editor, one per language: only identifiers, no sentence to translate.</summary>
public static class CodeSamples
{
    private const string PlainText = "plaintext";

    private static readonly IReadOnlyDictionary<string, string> Samples = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["yaml"] = "name: release\nstages:\n  - pipeline: build-front\n  - pipeline: build-back\n  - pipeline: \"deploy-prod\"\nvariables:\n  region: eu-west\n  replicas: 2\n",
        ["json"] = "{\n  \"destination\": \"Lisbon\",\n  \"nights\": 4,\n  \"flights\": [\"TP 437\", \"TP 438\"],\n  \"budget\": { \"currency\": \"EUR\", \"amount\": 850 }\n}\n",
        ["csharp"] = "public sealed record Flight(string Number, DateTimeOffset Departure)\n{\n    public bool IsDirect { get; init; } = true;\n\n    public TimeSpan Until(DateTimeOffset now) => Departure - now;\n}\n",
        ["javascript"] = "export function nights(start, end) {\n    const day = 24 * 60 * 60 * 1000;\n    return Math.round((end - start) / day);\n}\n",
        ["html"] = "<section class=\"trip\">\n  <h2>Lisbon</h2>\n  <p><strong>4</strong> nights</p>\n</section>\n",
        ["css"] = ".trip {\n  display: grid;\n  gap: 1rem;\n  padding: 1.5rem;\n}\n",
        ["sql"] = "SELECT d.name, COUNT(f.id) AS flights\nFROM destinations d\nJOIN flights f ON f.destination_id = d.id\nGROUP BY d.name\nORDER BY flights DESC;\n",
        ["markdown"] = "# Release\n\n- **Trips**\n- *Magic Survival*\n- `Path of Exile`\n",
        [PlainText] = "release\nomnieurope\nmonaco\n"
    };

    /// <summary>The languages a sample exists for, in the order of the list.</summary>
    public static IReadOnlyList<string> Languages { get; } = [.. Samples.Keys];

    /// <summary>The sample of a language; a language without one reads as plain text.</summary>
    /// <param name="language">The language identifier, such as <c>yaml</c> or <c>csharp</c>.</param>
    /// <returns>The sample text.</returns>
    public static string Of(string language) => Samples.TryGetValue(language, out var sample) ? sample : Samples[PlainText];
}
