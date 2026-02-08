using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ErpSystem.BuildingBlocks.Auth;

public interface IApiClientRepository
{
    Task<string?> GetSecretAsync(string appId);
}

public class SignatureVerificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiClientRepository _clientRepo;
    private readonly ILogger<SignatureVerificationMiddleware> _logger;

    public SignatureVerificationMiddleware(RequestDelegate next, IApiClientRepository clientRepo, ILogger<SignatureVerificationMiddleware> logger)
    {
        _next = next;
        _clientRepo = clientRepo;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if not an API request or specific path if needed, but for now apply to all
        
        if (!context.Request.Headers.TryGetValue("X-AppId", out var appId) ||
            !context.Request.Headers.TryGetValue("X-Timestamp", out var timestamp) ||
            !context.Request.Headers.TryGetValue("X-Nonce", out var nonce) ||
            !context.Request.Headers.TryGetValue("X-Signature", out var signature))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing signature headers (X-AppId, X-Timestamp, X-Nonce, X-Signature)");
            return;
        }

        // Validate Timestamp (e.g., within 5 minutes)
        if (!long.TryParse(timestamp, out var ts))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid timestamp format");
            return;
        }

        var serverTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(serverTime - ts) > 300) // 5 minutes window
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Request expired");
            return;
        }

        // Retrieve Secret
        var secret = await _clientRepo.GetSecretAsync(appId!);
        if (string.IsNullOrEmpty(secret))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid AppId");
            return;
        }

        // Read Body safely
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Verify Signature
        // Format: AppId + Timestamp + Nonce + Body
        var payload = $"{appId}{timestamp}{nonce}{body}";
        var computedSignature = ComputeHmacSha256(payload, secret);

        // Case-insensitive comparison
        if (!string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Signature verification failed for AppId: {AppId}. Server computed: {Computed}, Client sent: {Signature}", appId, computedSignature, signature);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid signature");
            return;
        }

        await _next(context);
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }
}
