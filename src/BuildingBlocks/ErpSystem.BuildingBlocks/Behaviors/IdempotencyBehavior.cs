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
public class IdempotencyBehavior<TRequest, TResponse>(IDistributedCache cache, ILogger<IdempotencyBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Check if request implements IIdempotentRequest
        if (request is not IIdempotentRequest idempotentRequest)
        {
            return await next(cancellationToken);
        }

        string idempotencyKey = GenerateIdempotencyKey(idempotentRequest);
        string? cachedResponse = await cache.GetStringAsync(idempotencyKey, cancellationToken);

        if (cachedResponse is not null)
        {
            logger.LogWarning("Duplicate request detected. IdempotencyKey: {Key}", idempotencyKey);
            return JsonSerializer.Deserialize<TResponse>(cachedResponse)!;
        }

        TResponse response = await next(cancellationToken);

        // Cache the response
        string serializedResponse = JsonSerializer.Serialize(response);
        await cache.SetStringAsync(
            idempotencyKey,
            serializedResponse,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _defaultExpiration },
            cancellationToken);

        logger.LogDebug("Request processed and cached. IdempotencyKey: {Key}", idempotencyKey);

        return response;
    }

    private static string GenerateIdempotencyKey(IIdempotentRequest request)
    {
        string? requestType = request.GetType().FullName;
        string requestId = request.IdempotencyKey;
        string combined = $"{requestType}:{requestId}";
        
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
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
