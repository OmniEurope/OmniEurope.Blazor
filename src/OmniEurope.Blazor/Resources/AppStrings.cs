namespace OmniEurope.Blazor.Resources;

/// <summary>
/// The marker type of the package's resources (<c>AppStrings.resx</c> and its translations): inject
/// <c>IStringLocalizer&lt;AppStrings&gt;</c> to read the package texts, and target it to override them
/// through <see cref="Microsoft.Extensions.DependencyInjection.OmniEuropeBlazorServiceCollectionExtensions.AddOmniEuropeTextOverrides{THostResource}"/>.
/// </summary>
public sealed class AppStrings;
