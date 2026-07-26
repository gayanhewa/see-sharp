using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SeeSharp.Infrastructure.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddSeeSharpTelemetry(
        this IServiceCollection services, IConfiguration config, string serviceName)
    {
        var endpoint = new Uri(config["Otel:Endpoint"] ?? "http://localhost:4317");

        services.AddSingleton<AppMetrics>();

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", config["ASPNETCORE_ENVIRONMENT"] ?? "Development")
            });

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddSource(AppTelemetry.ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = endpoint))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(AppMetrics.MeterName)
                .AddOtlpExporter(o => o.Endpoint = endpoint))
            .WithLogging(logging => logging
                .SetResourceBuilder(resource)
                .AddOtlpExporter(o => o.Endpoint = endpoint),
                options => options.IncludeScopes = true);

        return services;
    }
}
