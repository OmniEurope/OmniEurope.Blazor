namespace OmniEurope.Blazor.Internal;

internal static class OmniUriPolicy
{
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        Uri.UriSchemeMailto,
        "tel"
    };

    internal static string? EnsureSafe(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var candidate = value.Trim();
        if (candidate.Any(char.IsControl))
        {
            throw CreateException(parameterName);
        }

        if (Uri.TryCreate(candidate, UriKind.Absolute, out var absolute))
        {
            if (!AllowedSchemes.Contains(absolute.Scheme))
            {
                throw CreateException(parameterName);
            }

            return candidate;
        }

        if (Uri.TryCreate(candidate, UriKind.Relative, out _))
        {
            return candidate;
        }

        throw CreateException(parameterName);
    }

    private static InvalidOperationException CreateException(string parameterName) =>
        new($"'{parameterName}' uses a URI scheme that is not allowed. Use a relative URI or an http, https, mailto, or tel URI.");
}
