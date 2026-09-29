using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Localization;

/// <summary>
/// The package's texts, each overridable by the host: a host resource named with the prefix and the
/// package key (<c>Omni_ConnectionReconnectNow</c>, say) replaces the package text, in every culture
/// the host translates. What is generic stays in the package; what belongs to one application lives
/// in that application's resources. Registered by
/// <see cref="Microsoft.Extensions.DependencyInjection.OmniEuropeBlazorServiceCollectionExtensions.AddOmniEuropeTextOverrides{THostResource}"/>.
/// </summary>
public sealed class OmniTextOverrideLocalizer : IStringLocalizer<AppStrings>
{
    private readonly IStringLocalizer _package;
    private readonly IStringLocalizer _host;
    private readonly string _prefix;

    /// <summary>Creates a localizer that reads each text from <paramref name="host"/> first, then from <paramref name="package"/>.</summary>
    /// <param name="package">The package's own resources (<see cref="AppStrings"/>).</param>
    /// <param name="host">The host's resources, searched for the overriding keys.</param>
    /// <param name="prefix">The prefix of an overriding key, <c>Omni_</c> for instance; must not be null or empty.</param>
    public OmniTextOverrideLocalizer(IStringLocalizer package, IStringLocalizer host, string prefix)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        _package = package;
        _host = host;
        _prefix = prefix;
    }

    /// <summary>
    /// The text of <paramref name="name"/>: the host resource named with the prefix and the key when it
    /// exists in the current UI culture, else the package text.
    /// </summary>
    /// <param name="name">The package key.</param>
    public LocalizedString this[string name]
    {
        get
        {
            var overridden = _host[_prefix + name];
            return overridden.ResourceNotFound ? _package[name] : new LocalizedString(name, overridden.Value, false, overridden.SearchedLocation);
        }
    }

    /// <summary>
    /// The text of <paramref name="name"/>, formatted with <paramref name="arguments"/>: the host override
    /// when it exists, else the package text.
    /// </summary>
    /// <param name="name">The package key.</param>
    /// <param name="arguments">The values of the placeholders of the text.</param>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var overridden = _host[_prefix + name, arguments];
            return overridden.ResourceNotFound ? _package[name, arguments] : new LocalizedString(name, overridden.Value, false, overridden.SearchedLocation);
        }
    }

    /// <summary>The package's own texts; an override does not add keys the package does not have.</summary>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _package.GetAllStrings(includeParentCultures).Select(text => this[text.Name]);
}
