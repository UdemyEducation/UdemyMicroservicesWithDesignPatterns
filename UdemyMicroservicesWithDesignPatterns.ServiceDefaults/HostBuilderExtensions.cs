using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// IHostBuilder / Startup-based equivalents of the Aspire ServiceDefaults for projects
// that do not use WebApplication.CreateBuilder.
public static class HostBuilderExtensions
{
    public static IHostBuilder AddServiceDefaults(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureLogging(logging => logging.AddOpenTelemetry(o =>
        {
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
        }));

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation())
                .WithTracing(tracing => tracing
                    .AddSource(context.HostingEnvironment.ApplicationName)
                    .AddAspNetCoreInstrumentation(t => t.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health")
                        && !ctx.Request.Path.StartsWithSegments("/alive"))
                    .AddHttpClientInstrumentation());

            if (!string.IsNullOrWhiteSpace(context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            {
                services.AddOpenTelemetry().UseOtlpExporter();
            }

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            services.AddServiceDiscovery();
            services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });
        });

        return hostBuilder;
    }

    // Call from Startup.Configure inside UseEndpoints.
    public static IEndpointRouteBuilder MapDefaultEndpoints(this IEndpointRouteBuilder endpoints, bool isDevelopment)
    {
        if (isDevelopment)
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
        }

        return endpoints;
    }
}
