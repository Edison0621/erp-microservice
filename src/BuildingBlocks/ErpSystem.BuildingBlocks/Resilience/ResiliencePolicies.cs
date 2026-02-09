using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;

namespace ErpSystem.BuildingBlocks.Resilience;

/// <summary>
/// Resilience Policies - Provides pre-configured Polly V8 resilience pipelines for fault tolerance.
/// Implements Circuit Breaker, Retry, Timeout patterns using the new ResiliencePipelineBuilder API.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Standard retry pipeline with exponential backoff
    /// </summary>
    public static ResiliencePipeline CreateRetryPipeline(int maxRetryAttempts = 3)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1)
            })
            .Build();
    }

    /// <summary>
    /// Circuit breaker pipeline - Stops calling failing services
    /// </summary>
    public static ResiliencePipeline CreateCircuitBreakerPipeline(
        double failureRatio = 0.5,
        TimeSpan? samplingDuration = null,
        int minimumThroughput = 10,
        TimeSpan? breakDuration = null)
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = failureRatio,
                SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(30),
                MinimumThroughput = minimumThroughput,
                BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30)
            })
            .Build();
    }

    /// <summary>
    /// Timeout pipeline
    /// </summary>
    public static ResiliencePipeline CreateTimeoutPipeline(TimeSpan timeout)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeout
            })
            .Build();
    }

    /// <summary>
    /// Combined pipeline with retry, circuit breaker, and timeout
    /// </summary>
    public static ResiliencePipeline CreateCombinedPipeline(
        int maxRetryAttempts = 3,
        double failureRatio = 0.5,
        TimeSpan? timeout = null)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = failureRatio,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build();
    }

    /// <summary>
    /// Typed HTTP retry pipeline for transient errors
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateHttpRetryPipeline(int maxRetryAttempts = 3)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError)
                    .HandleResult(r => r.StatusCode == HttpStatusCode.RequestTimeout)
            })
            .Build();
    }
}
