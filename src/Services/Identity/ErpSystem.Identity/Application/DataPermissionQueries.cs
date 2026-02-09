using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Identity.Domain;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ErpSystem.Identity.Application;

// Query
public record GetUserDataPermissionsQuery(Guid UserId, string DataDomain) : IRequest<ResolvedDataPermission>;

public class ResolvedDataPermission
{
    public Guid UserId { get; set; }
    public string DataDomain { get; set; }
    public ScopeType FinalScope { get; set; }
    public List<Guid> AllowedDepartmentIds { get; set; } = [];
    
    // Logic: Merging multiple roles
    // If any role says ALL, then ALL.
    // If any role says DeptAndSub, and another says Dept, then DeptAndSub (larger coverage).
    // Actually, "larger coverage" is tricky. Usually Union of sets.
}

public class DataPermissionQueryHandler(EventStoreRepository<User> userRepo, IdentityReadDbContext readContext) : IRequestHandler<GetUserDataPermissionsQuery, ResolvedDataPermission>
{
    public async Task<ResolvedDataPermission> Handle(GetUserDataPermissionsQuery request, CancellationToken cancellationToken)
    {
        // 1. Get User to find assigned Roles
        // We can use UserReadModel logic if we put RoleCodes there, but Aggregate is source of truth for Roles list usually
        // Actually UserAggregate has List<string> Roles (RoleCodes).
        
        User? user = await userRepo.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        List<string> userRoleCodes = user.Roles.ToList(); 
        
        // 2. Load RoleDefinitions from ReadModel (more efficient than Aggregate loading loop)
        List<RoleReadModel> userRoles = await readContext.Roles
            .Where(r => userRoleCodes.Contains(r.RoleCode))
            .ToListAsync(cancellationToken);

        // 3. Merge Logic
        ScopeType finalScope = ScopeType.Self; // Default lowest
        // var allowedDepts ...

        HashSet<string> allPermissions = []; //TODO
        List<RoleDataPermissionSafe> roleDataPermissions = [];

        foreach (RoleReadModel role in userRoles)
        {
            List<string> p = JsonSerializer.Deserialize<List<string>>(role.Permissions ?? "[]") ?? [];
            foreach (string item in p) allPermissions.Add(item);

            List<RoleDataPermissionSafe> dp = JsonSerializer.Deserialize<List<RoleDataPermissionSafe>>(role.DataPermissions ?? "[]") ?? [];
            roleDataPermissions.AddRange(dp);
        }

        RoleDataPermissionSafe? target = roleDataPermissions.FirstOrDefault(p => p.DataDomain == request.DataDomain);
            
        if (target != null)
        {
            ScopeType scope = (ScopeType)target.ScopeType;
            if (scope > finalScope) finalScope = scope; // Simple enum comparison works if All > Dept > Self
        }

        return new ResolvedDataPermission
        {
            UserId = request.UserId,
            DataDomain = request.DataDomain,
            FinalScope = finalScope
        };
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private record RoleDataPermissionSafe(string DataDomain, int ScopeType);
}
