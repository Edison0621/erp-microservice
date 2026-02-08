using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Identity.Domain;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Identity.Application;

public record CreatePositionCommand(string Name, string Description) : IRequest<Guid>;
public record UpdateUserProfileCommand(Guid UserId, string DeptId, string PosId, string Phone) : IRequest;
public record ConfigureRoleDataPermissionCommand(Guid RoleId, string DataDomain, ScopeType ScopeType, List<string> AllowedIds) : IRequest;
public record RegisterUserCommand(string Username, string Email, string Password, string DisplayName) : IRequest<Guid>;
public record LoginUserCommand(string Username, string Password) : IRequest<string>;
public record CreateRoleCommand(string RoleName, string RoleCode, bool IsSystemRole) : IRequest<Guid>;
public record AssignRolePermissionCommand(Guid RoleId, string PermissionCode) : IRequest;

public record CreateDepartmentCommand(string Name, string ParentId, int Order) : IRequest<Guid>;
public record MoveDepartmentCommand(Guid DepartmentId, string NewParentId) : IRequest<bool>;

public class IdentityFullCommandHandler : 
    IRequestHandler<CreatePositionCommand, Guid>,
    IRequestHandler<UpdateUserProfileCommand>,
    IRequestHandler<ConfigureRoleDataPermissionCommand>,
    IRequestHandler<RegisterUserCommand, Guid>,
    IRequestHandler<LoginUserCommand, string>,
    IRequestHandler<CreateRoleCommand, Guid>,
    IRequestHandler<AssignRolePermissionCommand>,
    IRequestHandler<CreateDepartmentCommand, Guid>,
    IRequestHandler<MoveDepartmentCommand, bool>
{
    private readonly EventStoreRepository<User> _userRepo;
    private readonly EventStoreRepository<Role> _roleRepo;
    private readonly EventStoreRepository<Position> _posRepo;
    private readonly EventStoreRepository<Department> _deptRepo;
    private readonly IdentityReadDbContext _readDb;

    public IdentityFullCommandHandler(
        EventStoreRepository<User> userRepo,
        EventStoreRepository<Role> roleRepo,
        EventStoreRepository<Position> posRepo,
        EventStoreRepository<Department> deptRepo,
        IdentityReadDbContext readDb)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _posRepo = posRepo;
        _deptRepo = deptRepo;
        _readDb = readDb;
    }

    public async Task<Guid> Handle(CreatePositionCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var pos = Position.Create(id, r.Name, r.Description);
        await _posRepo.SaveAsync(pos);
        return id;
    }

    public async Task Handle(UpdateUserProfileCommand r, CancellationToken ct)
    {
        var user = await _userRepo.LoadAsync(r.UserId);
        if (user == null) return;
        user.UpdateProfile(r.DeptId, r.PosId, r.Phone);
        await _userRepo.SaveAsync(user);
    }

    public async Task Handle(ConfigureRoleDataPermissionCommand r, CancellationToken ct)
    {
        var role = await _roleRepo.LoadAsync(r.RoleId);
        if (role == null) return;
        role.ConfigureDataPermission(r.DataDomain, r.ScopeType, r.AllowedIds);
        await _roleRepo.SaveAsync(role);
    }

    public async Task<Guid> Handle(RegisterUserCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var hash = BCrypt.Net.BCrypt.HashPassword(r.Password);
        var user = User.Create(id, r.Username, r.Email, r.DisplayName, hash);
        await _userRepo.SaveAsync(user);
        return id;
    }

    public async Task<string> Handle(LoginUserCommand r, CancellationToken ct)
    {
        var readModel = await _readDb.Users.FirstOrDefaultAsync(u => u.Username == r.Username, ct);
        if (readModel == null || !BCrypt.Net.BCrypt.Verify(r.Password, readModel.PasswordHash))
            throw new Exception("Invalid credentials");

        var user = await _userRepo.LoadAsync(readModel.UserId);
        if (user == null) throw new Exception("User aggregate not found");
        
        user.LoginSucceeded("127.0.0.1");
        await _userRepo.SaveAsync(user);
        return JwtTokenGenerator.Generate(user.Id, user.Username);
    }

    public async Task<Guid> Handle(CreateRoleCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var role = Role.Create(id, r.RoleName, r.RoleCode, r.IsSystemRole);
        await _roleRepo.SaveAsync(role);
        return id;
    }

    public async Task Handle(AssignRolePermissionCommand r, CancellationToken ct)
    {
        var role = await _roleRepo.LoadAsync(r.RoleId);
        role!.AssignPermission(r.PermissionCode);
        await _roleRepo.SaveAsync(role);
    }

    public async Task<Guid> Handle(CreateDepartmentCommand r, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var dept = Department.Create(id, r.Name, r.ParentId, r.Order);
        await _deptRepo.SaveAsync(dept);
        return id;
    }

    public async Task<bool> Handle(MoveDepartmentCommand r, CancellationToken ct)
    {
        var dept = await _deptRepo.LoadAsync(r.DepartmentId);
        if (dept == null) return false;
        dept.Move(r.NewParentId);
        await _deptRepo.SaveAsync(dept);
        return true;
    }
}

