using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace ErpSystem.BuildingBlocks.Auth;

public interface IApiClientRepository
{
    Task<string?> GetSecretAsync(string appId);
}

public class SignatureVerificationMiddleware(RequestDelegate next, IApiClientRepository clientRepo, ILogger<SignatureVerificationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if not an API request or specific path if needed, but for now apply to all
        
        if (!context.Request.Headers.TryGetValue("X-AppId", out StringValues appId) ||
            !context.Request.Headers.TryGetValue("X-Timestamp", out StringValues timestamp) ||
            !context.Request.Headers.TryGetValue("X-Nonce", out StringValues nonce) ||
            !context.Request.Headers.TryGetValue("X-Signature", out StringValues signature))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing signature headers (X-AppId, X-Timestamp, X-Nonce, X-Signature)");
            return;
        }

        // Validate Timestamp (e.g., within 5 minutes)
        if (!long.TryParse(timestamp, out long ts))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid timestamp format");
            return;
        }

        long serverTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(serverTime - ts) > 300) // 5 minutes window
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Request expired");
            return;
        }

        // Retrieve Secret
        string? secret = await clientRepo.GetSecretAsync(appId!);
        if (string.IsNullOrEmpty(secret))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid AppId");
            return;
        }

        // Read Body safely
        context.Request.EnableBuffering();
        using StreamReader reader = new StreamReader(context.Request.Body, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Verify Signature
        // Format: AppId + Timestamp + Nonce + Body
        string payload = $"{appId}{timestamp}{nonce}{body}";
        string computedSignature = ComputeHmacSha256(payload, secret);

        // Case-insensitive comparison
        if (!string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Signature verification failed for AppId: {AppId}. Server computed: {Computed}, Client sent: {Signature}", appId, computedSignature, signature);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid signature");
            return;
        }

        await next(context);
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }
}
