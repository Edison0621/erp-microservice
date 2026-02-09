using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Identity.Domain;
using MediatR;
using System.Text.Json;

namespace ErpSystem.Identity.Infrastructure;

// Read Models
public class UserReadModel
{
    [Key]
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string PrimaryDepartmentId { get; set; } = string.Empty;
    public string PrimaryPositionId { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class RoleReadModel
{
    [Key]
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public string Permissions { get; set; } = "[]"; 
    public string DataPermissions { get; set; } = "[]"; 
}

public class DepartmentReadModel
{
    [Key]
    public Guid DepartmentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ParentId { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class PositionReadModel
{
    [Key]
    public Guid PositionId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AuditLogEntry
{
    [Key]
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class IdentityReadDbContext(DbContextOptions<IdentityReadDbContext> options) : DbContext(options)
{
    public DbSet<UserReadModel> Users { get; set; }
    public DbSet<RoleReadModel> Roles { get; set; }
    public DbSet<DepartmentReadModel> Departments { get; set; }
    public DbSet<PositionReadModel> Positions { get; set; }
    public DbSet<AuditLogEntry> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleReadModel>(b => {
             b.Property(r => r.Permissions).HasColumnType("jsonb");
             b.Property(r => r.DataPermissions).HasColumnType("jsonb");
        });
    }
}

// User Projection
public class UserProjection(IdentityReadDbContext dbContext) :
    INotificationHandler<UserCreatedEvent>,
    INotificationHandler<UserLoggedInEvent>,
    INotificationHandler<UserLoginFailedEvent>,
    INotificationHandler<UserLockedEvent>,
    INotificationHandler<UserUnlockedEvent>,
    INotificationHandler<UserProfileUpdatedEvent>
{
    public async Task Handle(UserCreatedEvent n, CancellationToken ct)
    {
        dbContext.Users.Add(new UserReadModel { UserId = n.UserId, Username = n.Username, Email = n.Email, DisplayName = n.DisplayName, PasswordHash = n.PasswordHash, CreatedAt = n.OccurredOn });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(UserLoggedInEvent n, CancellationToken ct)
    {
        UserReadModel? user = await dbContext.Users.FindAsync([n.UserId], ct);
        if (user != null) { user.LastLoginAt = n.OccurredOn; user.AccessFailedCount = 0; user.IsLocked = false; await dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserLoginFailedEvent n, CancellationToken ct)
    {
        UserReadModel? user = await dbContext.Users.FindAsync([n.UserId], ct);
        if (user != null) { user.AccessFailedCount++; await dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserLockedEvent n, CancellationToken ct)
    {
        UserReadModel? user = await dbContext.Users.FindAsync([n.UserId], ct);
        if (user != null) { user.IsLocked = true; user.LockoutEnd = n.LockoutEnd; await dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserUnlockedEvent n, CancellationToken ct)
    {
        UserReadModel? user = await dbContext.Users.FindAsync([n.UserId], ct);
        if (user != null) { user.IsLocked = false; user.AccessFailedCount = 0; await dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserProfileUpdatedEvent n, CancellationToken ct)
    {
        UserReadModel? user = await dbContext.Users.FindAsync([n.UserId], ct);
        if (user != null) { user.PrimaryDepartmentId = n.PrimaryDepartmentId; user.PrimaryPositionId = n.PrimaryPositionId; user.PhoneNumber = n.PhoneNumber; await dbContext.SaveChangesAsync(ct); }
    }
}

// Role & Position Projections
public class RoleProjection(IdentityReadDbContext dbContext) :
    INotificationHandler<RoleCreatedEvent>,
    INotificationHandler<RolePermissionAssignedEvent>,
    INotificationHandler<RoleDataPermissionConfiguredEvent>
{
    public async Task Handle(RoleCreatedEvent n, CancellationToken ct)
    {
        dbContext.Roles.Add(new RoleReadModel { RoleId = n.RoleId, RoleName = n.RoleName, RoleCode = n.RoleCode, IsSystemRole = n.IsSystemRole });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(RolePermissionAssignedEvent n, CancellationToken ct)
    {
        RoleReadModel? role = await dbContext.Roles.FindAsync([n.RoleId], ct);
        if (role != null)
        {
            List<string> p = JsonSerializer.Deserialize<List<string>>(role.Permissions) ?? [];
            if (!p.Contains(n.PermissionCode)) { p.Add(n.PermissionCode); role.Permissions = JsonSerializer.Serialize(p); await dbContext.SaveChangesAsync(ct); }
        }
    }

    public async Task Handle(RoleDataPermissionConfiguredEvent n, CancellationToken ct)
    {
        RoleReadModel? role = await dbContext.Roles.FindAsync([n.RoleId], ct);
        if (role != null)
        {
            List<RoleDataPermission> dps = JsonSerializer.Deserialize<List<RoleDataPermission>>(role.DataPermissions) ?? [];
            dps.RemoveAll(x => x.DataDomain == n.DataDomain);
            dps.Add(new RoleDataPermission(n.DataDomain, n.ScopeType, n.AllowedIds));
            role.DataPermissions = JsonSerializer.Serialize(dps);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}

public class PositionProjection(IdentityReadDbContext dbContext) : INotificationHandler<PositionCreatedEvent>
{
    public async Task Handle(PositionCreatedEvent n, CancellationToken ct)
    {
        dbContext.Positions.Add(new PositionReadModel { PositionId = n.PositionId, Name = n.Name, Description = n.Description });
        await dbContext.SaveChangesAsync(ct);
    }
}

// Department Projection
public class DepartmentProjection(IdentityReadDbContext dbContext) :
    INotificationHandler<DepartmentCreatedEvent>,
    INotificationHandler<DepartmentMovedEvent>
{
    public async Task Handle(DepartmentCreatedEvent n, CancellationToken ct)
    {
        dbContext.Departments.Add(new DepartmentReadModel { DepartmentId = n.DepartmentId, Name = n.Name, ParentId = n.ParentId, Order = n.Order });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(DepartmentMovedEvent n, CancellationToken ct)
    {
        DepartmentReadModel? dept = await dbContext.Departments.FindAsync([n.DepartmentId], ct);
        if (dept != null) { dept.ParentId = n.NewParentId; await dbContext.SaveChangesAsync(ct); }
    }
}

// Audit Projection
public class AuditLogProjection(IdentityReadDbContext dbContext) : INotificationHandler<IDomainEvent>
{
    public async Task Handle(IDomainEvent n, CancellationToken ct)
    {
        // Simple generic audit for now
        dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), Details = JsonSerializer.Serialize(n), EventType = n.GetType().Name, Timestamp = n.OccurredOn });
        await dbContext.SaveChangesAsync(ct);
    }
}
