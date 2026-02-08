using ErpSystem.BuildingBlocks.EventBus;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker; // Required for CircuitBreakerStrategyOptions

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddDaprEventBus();

var proxyConfig = builder.Configuration.GetSection("ReverseProxy");
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
    // .AddServiceDiscoveryDestinationResolver(); // Service Discovery package not installed yet

// =====================================================================================
// ENTERPRISE RESILIENCE (Pro Level)
// =====================================================================================

// Define a standardized resilience pipeline for all outgoing HTTP calls
builder.Services.AddResiliencePipeline("default", pipeline =>
{
    // 1. Retry with Exponential Backoff + Jitter
    // Handles transient failures gracefully without thundering herd problems
    pipeline.AddRetry(new Polly.Retry.RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        Delay = TimeSpan.FromSeconds(2),
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });

    // 2. Circuit Breaker
    // Prevents cascading failures by stopping requests when downstream is unhealthy
    // Breaks if > 50% failures in 30s window
    pipeline.AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 20,
        BreakDuration = TimeSpan.FromSeconds(30)
    });

    // 3. Timeout
    // Fail fast to release resources
    pipeline.AddTimeout(TimeSpan.FromSeconds(10));
});

// Configure Rate Limiting to protect backend services from overload
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapReverseProxy();
app.MapHealthChecks("/health");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
