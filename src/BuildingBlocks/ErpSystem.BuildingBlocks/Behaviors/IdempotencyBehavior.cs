using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ErpSystem.BuildingBlocks.Behaviors;

/// <summary>
/// Idempotency Behavior - Prevents duplicate command execution in distributed systems.
/// Uses distributed cache to track processed request IDs.
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    public IdempotencyBehavior(IDistributedCache cache, ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Check if request implements IIdempotentRequest
        if (request is not IIdempotentRequest idempotentRequest)
        {
            return await next();
        }

        var idempotencyKey = GenerateIdempotencyKey(idempotentRequest);
        var cachedResponse = await _cache.GetStringAsync(idempotencyKey, cancellationToken);

        if (cachedResponse is not null)
        {
            _logger.LogWarning("Duplicate request detected. IdempotencyKey: {Key}", idempotencyKey);
            return JsonSerializer.Deserialize<TResponse>(cachedResponse)!;
        }

        var response = await next();

        // Cache the response
        var serializedResponse = JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(
            idempotencyKey,
            serializedResponse,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DefaultExpiration },
            cancellationToken);

        _logger.LogDebug("Request processed and cached. IdempotencyKey: {Key}", idempotencyKey);

        return response;
    }

    private static string GenerateIdempotencyKey(IIdempotentRequest request)
    {
        var requestType = request.GetType().FullName;
        var requestId = request.IdempotencyKey;
        var combined = $"{requestType}:{requestId}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return $"idempotency:{Convert.ToBase64String(hash)}";
    }
}

/// <summary>
/// Marker interface for idempotent requests
/// </summary>
public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}
