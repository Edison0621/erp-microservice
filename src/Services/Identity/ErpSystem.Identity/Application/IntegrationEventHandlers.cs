using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Identity.Domain;
using ErpSystem.Identity.Application.IntegrationEvents;

namespace ErpSystem.Identity.Application;

public class HRIntegrationEventHandler : 
    INotificationHandler<HRIntegrationEvents.EmployeeHiredIntegrationEvent>,
    INotificationHandler<HRIntegrationEvents.EmployeeTerminatedIntegrationEvent>
{
    private readonly EventStoreRepository<User> _userRepo;

    public HRIntegrationEventHandler(EventStoreRepository<User> userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task Handle(HRIntegrationEvents.EmployeeHiredIntegrationEvent n, CancellationToken ct)
    {
        // Auto-create user on hire? 
        // For now, let's just log it or maybe create a draft user.
        // The PRD says "Identity creates corresponding user".
        
        var user = User.Create(n.EmployeeId, n.EmployeeNumber, n.Email, n.FullName, "Password123!"); // Default password
        user.UpdateProfile(n.DepartmentId, n.PositionId, "");
        await _userRepo.SaveAsync(user);
    }

    public async Task Handle(HRIntegrationEvents.EmployeeTerminatedIntegrationEvent n, CancellationToken ct)
    {
        var user = await _userRepo.LoadAsync(n.EmployeeId);
        if (user != null)
        {
            user.LockUser("Employee Terminated", TimeSpan.FromDays(36500)); // Lock practically forever
            await _userRepo.SaveAsync(user);
        }
    }
}
