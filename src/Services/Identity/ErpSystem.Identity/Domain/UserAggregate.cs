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
    
    public List<string> Roles { get; private set; } = [];

    public static User Create(Guid id, string username, string email, string displayName, string passwordHash)
    {
        User user = new();
        user.ApplyChange(new UserCreatedEvent(id, username, email, displayName, passwordHash));
        return user;
    }

    public void LoginSucceeded(string ipAddress)
    {
        this.ApplyChange(new UserLoggedInEvent(this.Id, DateTime.UtcNow, ipAddress));
    }

    public void LoginFailed(string reason)
    {
        this.ApplyChange(new UserLoginFailedEvent(this.Id, reason));
        if (this.AccessFailedCount >= 5)
        {
            this.LockUser("Too many failed attempts", TimeSpan.FromMinutes(15));
        }
    }

    public void UpdateProfile(string deptId, string posId, string phone)
    {
        this.ApplyChange(new UserProfileUpdatedEvent(this.Id, deptId, posId, phone));
    }

    public void LockUser(string reason, TimeSpan duration)
    {
        this.ApplyChange(new UserLockedEvent(this.Id, reason, DateTime.UtcNow.Add(duration)));
    }

    public void UnlockUser()
    {
        this.ApplyChange(new UserUnlockedEvent(this.Id));
    }

    public void AssignRole(string roleCode)
    {
        if (!this.Roles.Contains(roleCode))
        {
            this.ApplyChange(new UserRoleAssignedEvent(this.Id, roleCode));
        }
    }

    public void ResetPassword(string newPasswordHash)
    {
        this.ApplyChange(new UserPasswordChangedEvent(this.Id, newPasswordHash));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case UserCreatedEvent e:
                this.Id = e.UserId;
                this.Username = e.Username;
                this.Email = e.Email;
                this.DisplayName = e.DisplayName;
                this.PasswordHash = e.PasswordHash;
                this.AccessFailedCount = 0;
                break;
            case UserLoggedInEvent:
                this.AccessFailedCount = 0;
                this.IsLocked = false;
                this.LockoutEnd = null;
                break;
            case UserLoginFailedEvent:
                this.AccessFailedCount++;
                break;
            case UserLockedEvent e:
                this.IsLocked = true;
                this.LockoutEnd = e.LockoutEnd;
                break;
            case UserUnlockedEvent:
                this.IsLocked = false;
                this.LockoutEnd = null;
                this.AccessFailedCount = 0;
                break;
            case UserProfileUpdatedEvent e:
                this.PrimaryDepartmentId = e.PrimaryDepartmentId;
                this.PrimaryPositionId = e.PrimaryPositionId;
                this.PhoneNumber = e.PhoneNumber;
                break;
            case UserPasswordChangedEvent e:
                this.PasswordHash = e.NewPasswordHash;
                break;
            case UserRoleAssignedEvent e:
                this.Roles.Add(e.RoleCode);
                break;
        }
    }
}
