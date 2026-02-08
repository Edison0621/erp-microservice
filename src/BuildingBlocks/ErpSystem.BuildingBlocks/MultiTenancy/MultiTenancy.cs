using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq.Expressions;

namespace ErpSystem.BuildingBlocks.MultiTenancy;

/// <summary>
/// Multi-Tenancy Infrastructure - Provides tenant isolation for SaaS applications.
/// </summary>
public interface ITenantContext
{
    string? TenantId { get; }
    bool HasTenant => !string.IsNullOrEmpty(TenantId);
}

/// <summary>
/// Marker interface for multi-tenant entities
/// </summary>
public interface IMultiTenantEntity
{
    string TenantId { get; set; }
}

/// <summary>
/// Query filter extension for multi-tenancy
/// </summary>
public static class MultiTenancyExtensions
{
    /// <summary>
    /// Configures global query filter for multi-tenant entities
    /// </summary>
    public static void ConfigureMultiTenancy<T>(
        this ModelBuilder modelBuilder,
        ITenantContext tenantContext) where T : class, IMultiTenantEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => 
            string.IsNullOrEmpty(tenantContext.TenantId) || e.TenantId == tenantContext.TenantId);
        
        modelBuilder.Entity<T>().HasIndex(e => e.TenantId);
    }

    /// <summary>
    /// Applies tenant context to all multi-tenant entities
    /// </summary>
    public static void ApplyMultiTenancyConfiguration(
        this ModelBuilder modelBuilder,
        ITenantContext tenantContext)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMultiTenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(MultiTenancyExtensions)
                    .GetMethod(nameof(ConfigureMultiTenancy))!
                    .MakeGenericMethod(entityType.ClrType);
                
                method.Invoke(null, new object[] { modelBuilder, tenantContext });
            }
        }
    }
}

/// <summary>
/// Interceptor to automatically set TenantId on new entities
/// </summary>
public class MultiTenantSaveChangesInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;

    public MultiTenantSaveChangesInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetTenantId(DbContext? context)
    {
        if (context is null || !_tenantContext.HasTenant) return;

        var entries = context.ChangeTracker.Entries<IMultiTenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.TenantId = _tenantContext.TenantId!;
        }
    }
}
