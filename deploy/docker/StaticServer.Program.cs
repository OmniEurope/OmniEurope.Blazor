// Static host of the published OmniEurope.Blazor showcase (Blazor WebAssembly), .NET 11.
//
// It is compiled by deploy/docker/Dockerfile.front into a throw-away `dotnet new web` project, never as part of
// the repository's solution. What it does that a plain static file server does not:
// - sends every rule of the published wwwroot/_headers (Content-Security-Policy, nosniff, Referrer-Policy,
//   Permissions-Policy, Cache-Control), the same file eng/Serve-StaticWithHeaders.mjs serves in the smoke tests;
//   the server refuses to start without it, so a deployed front never runs without its policy;
// - adds the showcase API origin (API_BASE_URL) to the connect-src of that policy, and a sha256 source for each
//   inline script the published index.html holds, so the policy stays strict without 'unsafe-inline';
// - writes wwwroot/appsettings.json at start: ApiBaseUrl, and DeployedAt (DEPLOYED_AT, the moment the pipeline
//   deployed this release, an ISO 8601 value; empty when absent or not a moment, and the version then has no tooltip);
// - gives every response a cache policy (the global rule: a file served without Cache-Control is kept by a
//   browser for a tenth of its age): `immutable` only for a file whose name carries the hash of its bytes,
//   `no-store` for the HTML shell, the runtime settings and every error, `no-cache` (revalidated, 304 while
//   unchanged) for everything else.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);
builder.WebHost.UseUrls("http://+:8080");
var app = builder.Build();

var webRoot = app.Environment.WebRootPath;
var rules = HeaderRule.Load(Path.Combine(webRoot, "_headers"));
var apiBaseUrl = ApiBaseUrl(Environment.GetEnvironmentVariable("API_BASE_URL"));
var deployedAt = DeployedAt(Environment.GetEnvironmentVariable("DEPLOYED_AT"));
WriteRuntimeSettings(webRoot, apiBaseUrl, deployedAt);
var contentSecurityPolicy = ComposePolicy(
    rules.SelectMany(rule => rule.Headers).LastOrDefault(header => header.Key.Equals("Content-Security-Policy", StringComparison.OrdinalIgnoreCase)).Value,
    apiBaseUrl is null ? null : new Uri(apiBaseUrl).GetLeftPart(UriPartial.Authority),
    InlineScripts(Path.Combine(webRoot, "index.html")));
app.Logger.LogInformation("Showcase front: API={Api}, deployed at {DeployedAt}", apiBaseUrl ?? "(none: preferences stay in the browser)", deployedAt ?? "(unknown: the version has no tooltip)");
if (deployedAt is null && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DEPLOYED_AT")))
{
    app.Logger.LogWarning("DEPLOYED_AT '{Value}' is not an ISO 8601 moment: the version has no tooltip.", Environment.GetEnvironmentVariable("DEPLOYED_AT"));
}

// The container is published on the loopback and the reverse proxy sets X-Forwarded-For.
var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
forwarded.KnownIPNetworks.Clear();
forwarded.KnownProxies.Clear();
app.UseForwardedHeaders(forwarded);

// Headers and cache policy, written when the response starts: the static asset endpoints have then set
// their own Cache-Control, which the policy keeps only when it is right for that file.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    context.Response.OnStarting(() =>
    {
        ApplyHeaders(context.Response, path == "/" ? "/index.html" : path);
        return Task.CompletedTask;
    });
    await next();
});

// The headers file configures this host, it is not content: it is never served.
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/_headers", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

// appsettings.json is rewritten at start: it bypasses MapStaticAssets, whose manifest holds the build-time
// length and ETag of the file as published.
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/appsettings.json", StringComparison.OrdinalIgnoreCase)
        && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
    {
        context.Response.ContentType = "application/json";
        await context.Response.SendFileAsync(Path.Combine(webRoot, "appsettings.json"));
        return;
    }

    await next();
});

// Serves the published assets through the endpoints manifest copied beside this server (fingerprinted names).
app.MapStaticAssets();

// Blazor routes: every path that is not a file answers the shell.
app.MapFallbackToFile("index.html");

app.Run();

void ApplyHeaders(HttpResponse response, string path)
{
    var matching = rules.Where(rule => rule.Matches(path)).SelectMany(rule => rule.Headers).ToList();
    foreach (var (name, value) in matching.Where(header => !header.Key.Equals("Cache-Control", StringComparison.OrdinalIgnoreCase)))
    {
        response.Headers[name] = name.Equals("Content-Security-Policy", StringComparison.OrdinalIgnoreCase) ? contentSecurityPolicy : value;
    }

    var fileName = path[(path.LastIndexOf('/') + 1)..];
    var fingerprinted = Patterns.Fingerprinted.IsMatch(fileName);
    var declared = matching.LastOrDefault(header => header.Key.Equals("Cache-Control", StringComparison.OrdinalIgnoreCase)).Value;
    var isHtml = response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;
    if (isHtml || response.StatusCode >= 400
        || fileName.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        response.Headers.CacheControl = "no-store";
    }
    else if (declared is not null && (fingerprinted || !declared.Contains("immutable", StringComparison.OrdinalIgnoreCase)))
    {
        response.Headers.CacheControl = declared;
    }
    else if (!(fingerprinted && response.Headers.CacheControl.ToString().Contains("immutable", StringComparison.OrdinalIgnoreCase)))
    {
        // Whatever the endpoint wrote (a fixed lifetime included) is replaced: everything else is revalidated.
        response.Headers.CacheControl = "no-cache";
    }
}

static string? ApiBaseUrl(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    var url = value.Trim();
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
    {
        throw new InvalidOperationException($"API_BASE_URL '{value}' is not an absolute http(s) address.");
    }

    return url.EndsWith('/') ? url : url + "/";
}

// The moment as an ISO 8601 UTC value, or null when it is absent or not a moment.
static string? DeployedAt(string? value) =>
    DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var at)
        ? at.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture)
        : null;

static void WriteRuntimeSettings(string webRoot, string? apiBaseUrl, string? deployedAt)
{
    var file = Path.Combine(webRoot, "appsettings.json");
    var settings = File.Exists(file) ? JsonNode.Parse(File.ReadAllText(file)) as JsonObject ?? new JsonObject() : new JsonObject();
    settings["ApiBaseUrl"] = apiBaseUrl ?? string.Empty;
    settings["DeployedAt"] = deployedAt ?? string.Empty;
    File.WriteAllText(file, settings.ToJsonString(new() { WriteIndented = true }));
    // The pre-compressed variants hold the build-time values.
    File.Delete(file + ".br");
    File.Delete(file + ".gz");
}

static string ComposePolicy(string? published, string? apiOrigin, IReadOnlyList<string> inlineScripts)
{
    if (string.IsNullOrWhiteSpace(published))
    {
        throw new InvalidOperationException("wwwroot/_headers holds no Content-Security-Policy: the front refuses to start without one.");
    }

    var policy = published;
    if (apiOrigin is not null)
    {
        policy = Patterns.ConnectSource.Replace(policy, match => $"{match.Value} {apiOrigin}", 1);
    }

    if (inlineScripts.Count > 0)
    {
        var hashes = string.Join(' ', inlineScripts.Select(script => $"'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(script)))}'"));
        policy = Patterns.ScriptSource.Replace(policy, match => $"{match.Value} {hashes}", 1);
    }

    return policy;
}

// The inline scripts of the shell (an import map, a boot snippet), line endings normalised as the browser does.
static IReadOnlyList<string> InlineScripts(string indexFile) =>
    [.. Patterns.InlineScript.Matches(File.ReadAllText(indexFile))
        .Where(match => !Patterns.HasSource.IsMatch(match.Groups["attributes"].Value) && !string.IsNullOrWhiteSpace(match.Groups["body"].Value))
        .Select(match => match.Groups["body"].Value.Replace("\r\n", "\n", StringComparison.Ordinal))];

internal sealed record HeaderRule(string Pattern, List<KeyValuePair<string, string>> Headers)
{
    private readonly Regex expression = new(
        "^" + Regex.Escape(Pattern).Replace("\\*", ".*", StringComparison.Ordinal) + "$", RegexOptions.CultureInvariant);

    public bool Matches(string path) => expression.IsMatch(path);

    /// <summary>The _headers format of the static hosts: a path pattern on an unindented line, then its headers, indented.</summary>
    public static List<HeaderRule> Load(string file)
    {
        if (!File.Exists(file))
        {
            throw new InvalidOperationException($"{file} is missing: the front refuses to start without its headers.");
        }

        var rules = new List<HeaderRule>();
        foreach (var raw in File.ReadAllLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (!char.IsWhiteSpace(raw[0]))
            {
                rules.Add(new HeaderRule(line, []));
                continue;
            }

            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (rules.Count == 0 || separator < 1)
            {
                throw new InvalidOperationException($"Invalid line in _headers: {line}");
            }

            rules[^1].Headers.Add(new(line[..separator].Trim(), line[(separator + 1)..].Trim()));
        }

        return rules;
    }
}

internal static class Patterns
{
    // The hash of the bytes in the name (.NET static web assets: ten lower-case letters and digits before the extension).
    public static readonly Regex Fingerprinted = new(@"\.[a-z0-9]{10}\.[A-Za-z0-9]+$",RegexOptions.CultureInvariant);

    public static readonly Regex ConnectSource = new(@"connect-src[^;]*", RegexOptions.CultureInvariant);

    public static readonly Regex ScriptSource = new(@"script-src[^;]*", RegexOptions.CultureInvariant);

    public static readonly Regex InlineScript = new(
        @"<script(?<attributes>[^>]*)>(?<body>.*?)</script>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex HasSource = new(@"\ssrc\s*=",RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
}
