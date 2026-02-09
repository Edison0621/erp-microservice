using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ErpSystem.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, string serviceName)
    {
        // 1. Logging
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // 2. Metrics & Tracing
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddSource(serviceName);
                       
                // Export to OTLP (e.g. Jaeger/Aspire Dashboard/Seq/Elastic)
                // This requires OTEL_EXPORTER_OTLP_ENDPOINT env var to be set in deployment
                tracing.AddOtlpExporter();
            });

        return builder;
    }
}
