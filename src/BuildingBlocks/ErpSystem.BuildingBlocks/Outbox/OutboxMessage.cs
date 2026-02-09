using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ErpSystem.BuildingBlocks.Outbox;

/// <summary>
/// Transactional Outbox Pattern - Guarantees reliable message delivery in distributed systems.
/// Messages are persisted in the same transaction as domain changes, then dispatched asynchronously.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create<T>(T message) where T : class
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(T).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(message),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    public void MarkAsProcessed()
    {
        this.ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        this.RetryCount++;
        this.Error = error;
    }

    public T? DeserializePayload<T>() where T : class
    {
        return JsonSerializer.Deserialize<T>(this.Payload);
    }

    public object? DeserializePayload()
    {
        Type? type = Type.GetType(this.MessageType);
        return type is not null ? JsonSerializer.Deserialize(this.Payload, type) : null;
    }
}

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// EF Core configuration for OutboxMessage
/// </summary>
public static class OutboxMessageConfiguration
{
    public static void ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MessageType).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.Error).HasMaxLength(2000);
            builder.HasIndex(x => x.ProcessedAt).HasFilter("\"ProcessedAt\" IS NULL");
        });
    }
}
