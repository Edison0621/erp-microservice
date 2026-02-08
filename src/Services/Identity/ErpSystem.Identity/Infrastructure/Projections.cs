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

public class IdentityReadDbContext : DbContext
{
    public DbSet<UserReadModel> Users { get; set; }
    public DbSet<RoleReadModel> Roles { get; set; }
    public DbSet<DepartmentReadModel> Departments { get; set; }
    public DbSet<PositionReadModel> Positions { get; set; }
    public DbSet<AuditLogEntry> AuditLogs { get; set; }

    public IdentityReadDbContext(DbContextOptions<IdentityReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleReadModel>(b => {
             b.Property(r => r.Permissions).HasColumnType("jsonb");
             b.Property(r => r.DataPermissions).HasColumnType("jsonb");
        });
    }
}

// User Projection
public class UserProjection : 
    INotificationHandler<UserCreatedEvent>,
    INotificationHandler<UserLoggedInEvent>,
    INotificationHandler<UserLoginFailedEvent>,
    INotificationHandler<UserLockedEvent>,
    INotificationHandler<UserUnlockedEvent>,
    INotificationHandler<UserProfileUpdatedEvent>
{
    private readonly IdentityReadDbContext _dbContext;
    public UserProjection(IdentityReadDbContext dbContext) => _dbContext = dbContext;

    public async Task Handle(UserCreatedEvent n, CancellationToken ct)
    {
        _dbContext.Users.Add(new UserReadModel { UserId = n.UserId, Username = n.Username, Email = n.Email, DisplayName = n.DisplayName, PasswordHash = n.PasswordHash, CreatedAt = n.OccurredOn });
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(UserLoggedInEvent n, CancellationToken ct)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { n.UserId }, ct);
        if (user != null) { user.LastLoginAt = n.OccurredOn; user.AccessFailedCount = 0; user.IsLocked = false; await _dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserLoginFailedEvent n, CancellationToken ct)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { n.UserId }, ct);
        if (user != null) { user.AccessFailedCount++; await _dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserLockedEvent n, CancellationToken ct)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { n.UserId }, ct);
        if (user != null) { user.IsLocked = true; user.LockoutEnd = n.LockoutEnd; await _dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserUnlockedEvent n, CancellationToken ct)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { n.UserId }, ct);
        if (user != null) { user.IsLocked = false; user.AccessFailedCount = 0; await _dbContext.SaveChangesAsync(ct); }
    }

    public async Task Handle(UserProfileUpdatedEvent n, CancellationToken ct)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { n.UserId }, ct);
        if (user != null) { user.PrimaryDepartmentId = n.PrimaryDepartmentId; user.PrimaryPositionId = n.PrimaryPositionId; user.PhoneNumber = n.PhoneNumber; await _dbContext.SaveChangesAsync(ct); }
    }
}

// Role & Position Projections
public class RoleProjection : 
    INotificationHandler<RoleCreatedEvent>,
    INotificationHandler<RolePermissionAssignedEvent>,
    INotificationHandler<RoleDataPermissionConfiguredEvent>
{
    private readonly IdentityReadDbContext _dbContext;
    public RoleProjection(IdentityReadDbContext dbContext) => _dbContext = dbContext;

    public async Task Handle(RoleCreatedEvent n, CancellationToken ct)
    {
        _dbContext.Roles.Add(new RoleReadModel { RoleId = n.RoleId, RoleName = n.RoleName, RoleCode = n.RoleCode, IsSystemRole = n.IsSystemRole });
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(RolePermissionAssignedEvent n, CancellationToken ct)
    {
        var role = await _dbContext.Roles.FindAsync(new object[] { n.RoleId }, ct);
        if (role != null)
        {
            var p = JsonSerializer.Deserialize<List<string>>(role.Permissions) ?? new();
            if (!p.Contains(n.PermissionCode)) { p.Add(n.PermissionCode); role.Permissions = JsonSerializer.Serialize(p); await _dbContext.SaveChangesAsync(ct); }
        }
    }

    public async Task Handle(RoleDataPermissionConfiguredEvent n, CancellationToken ct)
    {
        var role = await _dbContext.Roles.FindAsync(new object[] { n.RoleId }, ct);
        if (role != null)
        {
            var dps = JsonSerializer.Deserialize<List<RoleDataPermission>>(role.DataPermissions) ?? new();
            dps.RemoveAll(x => x.DataDomain == n.DataDomain);
            dps.Add(new RoleDataPermission(n.DataDomain, n.ScopeType, n.AllowedIds));
            role.DataPermissions = JsonSerializer.Serialize(dps);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}

public class PositionProjection : INotificationHandler<PositionCreatedEvent>
{
    private readonly IdentityReadDbContext _dbContext;
    public PositionProjection(IdentityReadDbContext dbContext) => _dbContext = dbContext;
    public async Task Handle(PositionCreatedEvent n, CancellationToken ct)
    {
        _dbContext.Positions.Add(new PositionReadModel { PositionId = n.PositionId, Name = n.Name, Description = n.Description });
        await _dbContext.SaveChangesAsync(ct);
    }
}

// Department Projection
public class DepartmentProjection : 
    INotificationHandler<DepartmentCreatedEvent>,
    INotificationHandler<DepartmentMovedEvent>
{
    private readonly IdentityReadDbContext _dbContext;
    public DepartmentProjection(IdentityReadDbContext dbContext) => _dbContext = dbContext;

    public async Task Handle(DepartmentCreatedEvent n, CancellationToken ct)
    {
        _dbContext.Departments.Add(new DepartmentReadModel { DepartmentId = n.DepartmentId, Name = n.Name, ParentId = n.ParentId, Order = n.Order });
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task Handle(DepartmentMovedEvent n, CancellationToken ct)
    {
        var dept = await _dbContext.Departments.FindAsync(new object[] { n.DepartmentId }, ct);
        if (dept != null) { dept.ParentId = n.NewParentId; await _dbContext.SaveChangesAsync(ct); }
    }
}

// Audit Projection
public class AuditLogProjection : INotificationHandler<IDomainEvent>
{
    private readonly IdentityReadDbContext _dbContext;
    public AuditLogProjection(IdentityReadDbContext dbContext) => _dbContext = dbContext;

    public async Task Handle(IDomainEvent n, CancellationToken ct)
    {
        // Simple generic audit for now
        _dbContext.AuditLogs.Add(new AuditLogEntry { Id = Guid.NewGuid(), Details = JsonSerializer.Serialize(n), EventType = n.GetType().Name, Timestamp = n.OccurredOn });
        await _dbContext.SaveChangesAsync(ct);
    }
}
