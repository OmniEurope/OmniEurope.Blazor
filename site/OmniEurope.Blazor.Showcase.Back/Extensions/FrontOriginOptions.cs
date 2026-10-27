using Microsoft.Extensions.Options;

namespace OmniEurope.Blazor.Showcase.Back.Extensions;

/// <summary>
/// The origins of the showcase front, the only ones CORS lets call the API (section <c>Cors</c>,
/// environment variables <c>Cors__Origins__0</c>, <c>Cors__Origins__1</c>...). Required: without one
/// the API refuses to start rather than answer every origin or none.
/// </summary>
public sealed class FrontOriginOptions
{
    public const string Section = "Cors";

    public string[] Origins { get; set; } = [];
}

/// <summary>Each origin is an absolute http or https origin: a scheme, a host, a port, no path.</summary>
public sealed class FrontOriginOptionsValidator : IValidateOptions<FrontOriginOptions>
{
    public ValidateOptionsResult Validate(string? name, FrontOriginOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Origins is not { Length: > 0 })
        {
            return ValidateOptionsResult.Fail("Cors:Origins must list the origin of the showcase front (Cors__Origins__0).");
        }

        var invalid = options.Origins.Where(origin => !IsOrigin(origin)).ToArray();
        return invalid.Length == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Cors:Origins holds values that are not http(s) origins: {string.Join(", ", invalid)}.");
    }

    private static bool IsOrigin(string? origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
        && !origin!.EndsWith('/');
}
