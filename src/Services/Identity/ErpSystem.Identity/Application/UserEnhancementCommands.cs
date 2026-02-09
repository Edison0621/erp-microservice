using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Identity.Domain;

namespace ErpSystem.Identity.Application;

// Commands
public record LockUserCommand(Guid UserId, string Reason, TimeSpan Duration) : IRequest<bool>;

public record UnlockUserCommand(Guid UserId) : IRequest<bool>;

public record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>;

public record AssignRoleToUserCommand(Guid UserId, string RoleCode) : IRequest<bool>;

// Handler
public class UserEnhancementCommandHandler(EventStoreRepository<User> repository) :
    IRequestHandler<LockUserCommand, bool>,
    IRequestHandler<UnlockUserCommand, bool>,
    IRequestHandler<ResetPasswordCommand, bool>,
    IRequestHandler<AssignRoleToUserCommand, bool>
{
    public async Task<bool> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        user.LockUser(request.Reason, request.Duration);
        await repository.SaveAsync(user);
        return true;
    }

    public async Task<bool> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        user.UnlockUser();
        await repository.SaveAsync(user);
        return true;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        string? hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetPassword(hash);
        await repository.SaveAsync(user);
        return true;
    }

    public async Task<bool> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        user.AssignRole(request.RoleCode);
        await repository.SaveAsync(user);
        return true;
    }
}
