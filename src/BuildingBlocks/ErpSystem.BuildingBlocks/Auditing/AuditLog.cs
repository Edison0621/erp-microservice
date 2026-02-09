using ErpSystem.BuildingBlocks.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ErpSystem.BuildingBlocks.Auditing;

/// <summary>
/// Audit Log Entry - Records all significant changes for compliance and debugging.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public Guid? UserId { get; private set; }
    public string? TenantId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        string entityId,
        string action,
        object? oldValues,
        object? newValues,
        Guid? userId,
        string? tenantId,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is not null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues is not null ? JsonSerializer.Serialize(newValues) : null,
            UserId = userId,
            TenantId = tenantId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };
    }
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit Behavior - Automatically records command execution for auditing.
/// </summary>
public class AuditBehavior<TRequest, TResponse>(
    IAuditLogRepository auditRepository,
    IUserContext userContext,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IAuditableRequest auditableRequest)
        {
            return await next(cancellationToken);
        }

        TResponse response = await next(cancellationToken);

        try
        {
            AuditLog auditLog = AuditLog.Create(
                entityType: auditableRequest.EntityType,
                entityId: auditableRequest.EntityId,
                action: typeof(TRequest).Name,
                oldValues: null,
                newValues: request,
                userId: userContext.UserId,
                tenantId: userContext.TenantId);

            await auditRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create audit log for {RequestType}", typeof(TRequest).Name);
        }

        return response;
    }
}

/// <summary>
/// Marker interface for auditable requests
/// </summary>
public interface IAuditableRequest
{
    string EntityType { get; }
    string EntityId { get; }
}

/// <summary>
/// EF Core configuration for AuditLog
/// </summary>
public static class AuditLogConfiguration
{
    public static void ConfigureAuditLog(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
            builder.Property(x => x.TenantId).HasMaxLength(100);
            builder.Property(x => x.IpAddress).HasMaxLength(50);
            builder.Property(x => x.UserAgent).HasMaxLength(500);
            builder.HasIndex(x => new { x.EntityType, x.EntityId });
            builder.HasIndex(x => x.Timestamp);
            builder.HasIndex(x => x.UserId);
        });
    }
}
