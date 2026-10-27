using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Back.Data;
using OmniEurope.Blazor.Showcase.Back.Extensions;
using OmniEurope.Blazor.Showcase.Back.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Every value comes from configuration, so the environment of a container sets it with the same names
// as the kit's Compose file: ConnectionStrings__DefaultConnection, Cors__Origins__0, and the optional
// ForwardedHeaders__Enabled, Preferences__Limits__*, Preferences__Retention__*, RateLimiting__*.
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;
    // No endpoint reads more than one preference: a larger body is refused before it is buffered.
    kestrel.Limits.MaxRequestBodySize = VisitorPreferencesController.MaxRequestBodyBytes;
});

builder.Services.AddShowcaseDatabase(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<ShowcaseDbContext>("database", tags: ["ready"]);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddFrontCors(builder.Configuration);
builder.Services.AddShowcaseRateLimiting(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Behind the reverse proxy the request reaches the container from the proxy, and every client would
// share its address, hence one rate limit. Off by default; switched on where a proxy really fronts the
// API (ForwardedHeaders__Enabled=true). The trusted proxy lists are emptied because the deployment
// publishes the port on 127.0.0.1 only: the proxy is then the only possible client. Publishing the port
// on a public interface would let any client forge X-Forwarded-For and pick its own rate limit partition.
if (app.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePages();
app.UseCors(ServiceCollectionExtensions.FrontCorsPolicy);
app.UseRateLimiter();

// Liveness answers as long as the process serves requests; readiness also needs PostgreSQL. The
// deployment's health check targets /health/ready.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health");
app.MapControllers();

await DbInitializer.MigrateAsync(app.Services, app.Lifetime.ApplicationStopping);
await app.RunAsync();
