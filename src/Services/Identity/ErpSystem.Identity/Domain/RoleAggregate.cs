using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Identity.Domain;

// Events
public record RoleCreatedEvent(Guid RoleId, string RoleName, string RoleCode, bool IsSystemRole) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record RolePermissionAssignedEvent(Guid RoleId, string PermissionCode) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record RoleDataPermissionConfiguredEvent(
    Guid RoleId, 
    string DataDomain, 
    ScopeType ScopeType,
    List<string> AllowedIds
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Enums / Value Objects
public enum ScopeType
{
    Self = 1,
    Department = 2,
    DepartmentAndSub = 3,
    All = 4,
    Custom = 5
}

public record RoleDataPermission(string DataDomain, ScopeType ScopeType, List<string> AllowedIds);

// Aggregate
public class Role : AggregateRoot<Guid>
{
    public string RoleName { get; private set; } = string.Empty;
    public string RoleCode { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }
    
    public List<string> Permissions { get; private set; } = new();
    public List<RoleDataPermission> DataPermissions { get; private set; } = new();

    public static Role Create(Guid id, string roleName, string roleCode, bool isSystemRole)
    {
        var role = new Role();
        role.ApplyChange(new RoleCreatedEvent(id, roleName, roleCode, isSystemRole));
        return role;
    }

    public void AssignPermission(string permissionCode)
    {
        if (!Permissions.Contains(permissionCode))
        {
            ApplyChange(new RolePermissionAssignedEvent(Id, permissionCode));
        }
    }

    public void ConfigureDataPermission(string dataDomain, ScopeType scopeType, List<string> allowedIds)
    {
        ApplyChange(new RoleDataPermissionConfiguredEvent(Id, dataDomain, scopeType, allowedIds));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case RoleCreatedEvent e:
                Id = e.RoleId;
                RoleName = e.RoleName;
                RoleCode = e.RoleCode;
                IsSystemRole = e.IsSystemRole;
                break;
                
            case RolePermissionAssignedEvent e:
                if (!Permissions.Contains(e.PermissionCode))
                    Permissions.Add(e.PermissionCode);
                break;

            case RoleDataPermissionConfiguredEvent e:
                var existing = DataPermissions.FirstOrDefault(x => x.DataDomain == e.DataDomain);
                if (existing != null) DataPermissions.Remove(existing);
                DataPermissions.Add(new RoleDataPermission(e.DataDomain, e.ScopeType, e.AllowedIds));
                break;
        }
    }
}
