using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Identity.Domain;

// Events
public record UserCreatedEvent(Guid UserId, string Username, string Email, string DisplayName, string PasswordHash) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserLoggedInEvent(Guid UserId, DateTime LoginTime, string IpAddress) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserLoginFailedEvent(Guid UserId, string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserLockedEvent(Guid UserId, string Reason, DateTime? LockoutEnd) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserUnlockedEvent(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserProfileUpdatedEvent(Guid UserId, string PrimaryDepartmentId, string PrimaryPositionId, string PhoneNumber) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserPasswordChangedEvent(Guid UserId, string NewPasswordHash) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserRoleAssignedEvent(Guid UserId, string RoleCode) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// User Aggregate
public class User : AggregateRoot<Guid>
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    
    public bool IsLocked { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    
    public string PrimaryDepartmentId { get; private set; } = string.Empty;
    public string PrimaryPositionId { get; private set; } = string.Empty;
    
    public List<string> Roles { get; private set; } = new();

    public static User Create(Guid id, string username, string email, string displayName, string passwordHash)
    {
        var user = new User();
        user.ApplyChange(new UserCreatedEvent(id, username, email, displayName, passwordHash));
        return user;
    }

    public void LoginSucceeded(string ipAddress)
    {
        ApplyChange(new UserLoggedInEvent(Id, DateTime.UtcNow, ipAddress));
    }

    public void LoginFailed(string reason)
    {
        ApplyChange(new UserLoginFailedEvent(Id, reason));
        if (AccessFailedCount >= 5)
        {
            LockUser("Too many failed attempts", TimeSpan.FromMinutes(15));
        }
    }

    public void UpdateProfile(string deptId, string posId, string phone)
    {
        ApplyChange(new UserProfileUpdatedEvent(Id, deptId, posId, phone));
    }

    public void LockUser(string reason, TimeSpan duration)
    {
        ApplyChange(new UserLockedEvent(Id, reason, DateTime.UtcNow.Add(duration)));
    }

    public void UnlockUser()
    {
        ApplyChange(new UserUnlockedEvent(Id));
    }

    public void AssignRole(string roleCode)
    {
        if (!Roles.Contains(roleCode))
        {
            ApplyChange(new UserRoleAssignedEvent(Id, roleCode));
        }
    }

    public void ResetPassword(string newPasswordHash)
    {
        ApplyChange(new UserPasswordChangedEvent(Id, newPasswordHash));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case UserCreatedEvent e:
                Id = e.UserId;
                Username = e.Username;
                Email = e.Email;
                DisplayName = e.DisplayName;
                PasswordHash = e.PasswordHash;
                AccessFailedCount = 0;
                break;
            case UserLoggedInEvent:
                AccessFailedCount = 0;
                IsLocked = false;
                LockoutEnd = null;
                break;
            case UserLoginFailedEvent:
                AccessFailedCount++;
                break;
            case UserLockedEvent e:
                IsLocked = true;
                LockoutEnd = e.LockoutEnd;
                break;
            case UserUnlockedEvent:
                IsLocked = false;
                LockoutEnd = null;
                AccessFailedCount = 0;
                break;
            case UserProfileUpdatedEvent e:
                PrimaryDepartmentId = e.PrimaryDepartmentId;
                PrimaryPositionId = e.PrimaryPositionId;
                PhoneNumber = e.PhoneNumber;
                break;
            case UserPasswordChangedEvent e:
                PasswordHash = e.NewPasswordHash;
                break;
            case UserRoleAssignedEvent e:
                Roles.Add(e.RoleCode);
                break;
        }
    }
}
