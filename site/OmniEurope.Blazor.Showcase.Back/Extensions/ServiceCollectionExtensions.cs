using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Back.Components.Shared;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    /// <summary>The CORS policy of the API: the front origins only.</summary>
    public const string FrontCorsPolicy = "ShowcaseFront";

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPreferencesModule(configuration);
        return services;
    }

    /// <summary>
    /// CORS for the configured front origins: GET and PUT, the JSON content type and the visitor header,
    /// no credentials (the API has no cookie to protect).
    /// </summary>
    public static IServiceCollection AddFrontCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FrontOriginOptions>().Bind(configuration.GetSection(FrontOriginOptions.Section)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<FrontOriginOptions>, FrontOriginOptionsValidator>();
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IOptions<FrontOriginOptions>>((cors, front) => cors.AddPolicy(FrontCorsPolicy, policy => policy
            .WithOrigins(front.Value.Origins)
            .WithMethods(HttpMethods.Get, HttpMethods.Put)
            .WithHeaders("Content-Type", VisitorPreferenceApi.VisitorHeader)
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));
        return services;
    }

    /// <summary>
    /// Per-address fixed windows for the reads and the writes (<see cref="RateLimitingOptions"/>). A refusal
    /// is a 429 ProblemDetails with the time to wait when the limiter knows it.
    /// </summary>
    public static IServiceCollection AddShowcaseRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>().Bind(configuration.GetSection(RateLimitingOptions.Section))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ShowcaseRateLimiterPolicies.Reads, context =>
                PerAddressWindow(context, context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value.ReadsPerMinute));
            options.AddPolicy(ShowcaseRateLimiterPolicies.Writes, context =>
                PerAddressWindow(context, context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value.WritesPerMinute));
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    rejected.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var problems = rejected.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = rejected.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = "Too many requests from this address; try again later.",
                    },
                });
            };
        });
        return services;
    }

    private static RateLimitPartition<string> PerAddressWindow(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(ClientAddress.Partition(context.Connection.RemoteIpAddress), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
}
