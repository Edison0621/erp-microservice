using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Identity.Domain;
using ErpSystem.Identity.Application.IntegrationEvents;

namespace ErpSystem.Identity.Application;

public class HrIntegrationEventHandler(EventStoreRepository<User> userRepo) :
    INotificationHandler<HrIntegrationEvents.EmployeeHiredIntegrationEvent>,
    INotificationHandler<HrIntegrationEvents.EmployeeTerminatedIntegrationEvent>
{
    public async Task Handle(HrIntegrationEvents.EmployeeHiredIntegrationEvent n, CancellationToken ct)
    {
        // Auto-create user on hire? 
        // For now, let's just log it or maybe create a draft user.
        // The PRD says "Identity creates corresponding user".
        
        User user = User.Create(n.EmployeeId, n.EmployeeNumber, n.Email, n.FullName, "Password123!"); // Default password
        user.UpdateProfile(n.DepartmentId, n.PositionId, "");
        await userRepo.SaveAsync(user);
    }

    public async Task Handle(HrIntegrationEvents.EmployeeTerminatedIntegrationEvent n, CancellationToken ct)
    {
        User? user = await userRepo.LoadAsync(n.EmployeeId);
        if (user != null)
        {
            user.LockUser("Employee Terminated", TimeSpan.FromDays(36500)); // Lock practically forever
            await userRepo.SaveAsync(user);
        }
    }
}
