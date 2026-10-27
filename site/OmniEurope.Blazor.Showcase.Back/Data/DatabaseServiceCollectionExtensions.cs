using Microsoft.EntityFrameworkCore;

namespace OmniEurope.Blazor.Showcase.Back.Data;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>The connection string name, so the environment variable is <c>ConnectionStrings__DefaultConnection</c>.</summary>
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>Registers the PostgreSQL context. A missing connection string stops the start-up.</summary>
    public static IServiceCollection AddShowcaseDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{ConnectionStringName} must be configured (environment variable "
                + $"ConnectionStrings__{ConnectionStringName}). PostgreSQL is the only supported provider.");
        }

        services.AddDbContext<ShowcaseDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
