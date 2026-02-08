using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Identity.Domain;
using ErpSystem.Identity.Infrastructure;

namespace ErpSystem.Identity.Application;

// Commands
public record LockUserCommand(Guid UserId, string Reason, TimeSpan Duration) : IRequest<bool>;
public record UnlockUserCommand(Guid UserId) : IRequest<bool>;
public record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>;
public record AssignRoleToUserCommand(Guid UserId, string RoleCode) : IRequest<bool>;

// Handler
public class UserEnhancementCommandHandler : 
    IRequestHandler<LockUserCommand, bool>,
    IRequestHandler<UnlockUserCommand, bool>,
    IRequestHandler<ResetPasswordCommand, bool>,
    IRequestHandler<AssignRoleToUserCommand, bool>
{
    private readonly EventStoreRepository<User> _repository;

    public UserEnhancementCommandHandler(EventStoreRepository<User> repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        user.LockUser(request.Reason, request.Duration);
        await _repository.SaveAsync(user);
        return true;
    }

    public async Task<bool> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        user.UnlockUser();
        await _repository.SaveAsync(user);
        return true;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetPassword(hash);
        await _repository.SaveAsync(user);
        return true;
    }

    public async Task<bool> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.LoadAsync(request.UserId);
        if (user == null) throw new KeyNotFoundException("User not found");

        user.AssignRole(request.RoleCode);
        await _repository.SaveAsync(user);
        return true;
    }
}
