using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ErpSystem.BuildingBlocks.Behaviors;

/// <summary>
/// Performance Behavior - Tracks slow requests and logs performance metrics.
/// Warns when requests exceed threshold.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly Stopwatch _timer = new();
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        this._timer.Start();

        TResponse response = await next(cancellationToken);

        this._timer.Stop();

        long elapsedMs = this._timer.ElapsedMilliseconds;

        if (elapsedMs > SlowRequestThresholdMs)
        {
            string requestName = typeof(TRequest).Name;

            logger.LogWarning(
                "Long Running Request: {Name} ({ElapsedMilliseconds} ms) {@Request}",
                requestName,
                elapsedMs,
                request);
        }

        return response;
    }
}

/// <summary>
/// Unhandled Exception Behavior - Catches and logs all unhandled exceptions.
/// </summary>
public class UnhandledExceptionBehavior<TRequest, TResponse>(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            string requestName = typeof(TRequest).Name;

            logger.LogError(
                ex,
                "Unhandled Exception for Request {Name} {@Request}",
                requestName,
                request);

            throw;
        }
    }
}
