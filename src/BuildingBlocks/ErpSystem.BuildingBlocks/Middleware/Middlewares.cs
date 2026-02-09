using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ErpSystem.BuildingBlocks.Middleware;

/// <summary>
/// Request/Response Logging Middleware - Logs all HTTP requests with timing.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string requestId = Guid.NewGuid().ToString("N")[..8];

        logger.LogInformation(
            "[{RequestId}] {Method} {Path} started",
            requestId,
            context.Request.Method,
            context.Request.Path);

        try
        {
            await next(context);

            stopwatch.Stop();
            logger.LogInformation(
                "[{RequestId}] {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "[{RequestId}] {Method} {Path} failed with exception in {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Correlation ID Middleware - Adds correlation ID to all requests for distributed tracing.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                               ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        await next(context);
    }
}

/// <summary>
/// Global Exception Handler Middleware - Provides consistent error responses.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (FluentValidation.ValidationException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "ValidationError",
                Title = "Validation Failed",
                Status = 400,
                Errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "Forbidden",
                Title = "Access Denied",
                Status = 403
            });
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "NotFound",
                Title = ex.Message,
                Status = 404
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                Type = "InternalServerError",
                Title = "An unexpected error occurred",
                Status = 500
            });
        }
    }
}
