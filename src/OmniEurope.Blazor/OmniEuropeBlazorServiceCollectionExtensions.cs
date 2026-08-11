namespace Microsoft.Extensions.DependencyInjection;

public static class OmniEuropeBlazorServiceCollectionExtensions
{
    public static IServiceCollection AddOmniEuropeBlazor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddLocalization();
        return services;
    }
}
