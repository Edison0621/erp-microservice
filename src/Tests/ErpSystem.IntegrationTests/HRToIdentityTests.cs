using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.HR.Application;
using ErpSystem.HR.Domain;
using ErpSystem.Identity.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.IntegrationTests;

public class HrToIdentityTests : IntegrationTestBase
{
    [Fact]
    public async Task EmployeeLifecycle_ShouldManageIdentityAccount()
    {
        WebApplicationFactory<Identity.Program>? identityApp = null;
        WebApplicationFactory<HR.Program>? hrApp = null;

        try 
        {
            // 1. Setup Identity App
            identityApp = this.CreateIdentityApp();
            HttpClient identityClient = identityApp.CreateClient();
            
            // 2. Setup HR App (with two endpoints for hired and terminated)
            // Note: Our simple TestEventBus only supports one endpoint per bus.
            // But we can create two buses or use a more flexible one.
            // For now, I'll hire, then terminated. I'll switch the endpoint if needed or use a multi-endpoint bus.
            
            TestEventBus testEventBus = new(identityClient, "/api/v1/identity/integration/employee-hired");
            hrApp = this.CreateHrApp(testEventBus);

            IMediator mediatorHr = hrApp.Services.GetRequiredService<IMediator>();
            IMediator mediatorIdentity = identityApp.Services.GetRequiredService<IMediator>();
            
            Guid employeeId = Guid.NewGuid(); // We'll bypass Guid.NewGuid() in Hire to use a controlled one if needed, 
                                             // but HireEmployeeCommand generates its own. 
                                             // Wait! Handle(HireEmployeeCommand) uses Guid.NewGuid().
                                             
            // I'll use the returned Id.
            
            // 3. Hire Employee
            Guid hiredId = await mediatorHr.Send(new HireEmployeeCommand(
                "John Doe", "Male", new DateTime(1990, 1, 1), "ID", "123456789",
                DateTime.UtcNow, EmploymentType.FullTime, "COMP-1", "DEPT-1", "POS-1",
                "", "CC-1", "john.doe@example.com"
            ));

            // 4. Verify User Created in Identity
            await Task.Delay(500); 

            using (IServiceScope scope = identityApp.Services.CreateScope())
            {
                IdentityReadDbContext identDb = scope.ServiceProvider.GetRequiredService<IdentityReadDbContext>();
                UserReadModel? user = await identDb.Users.FirstOrDefaultAsync(u => u.UserId == hiredId);
                Assert.NotNull(user);
                Assert.Equal("john.doe@example.com", user.Email);
                Assert.False(user.IsLocked);
            }

            // 5. Setup Terminated Bus (Re-create HR App with new bus or use a smarter bus)
            // Re-creating is easiest for this test.
            hrApp.Dispose();
            TestEventBus termEventBus = new(identityClient, "/api/v1/identity/integration/employee-terminated");
            hrApp = this.CreateHrApp(termEventBus);
            mediatorHr = hrApp.Services.GetRequiredService<IMediator>();

            // 6. Terminate Employee
            await mediatorHr.Send(new TerminateEmployeeCommand(
                hiredId, DateTime.UtcNow, "RESIGNATION", "Leaving for better opportunity"
            ));

            // 7. Verify User Locked in Identity
            await Task.Delay(500);

            using (IServiceScope scope = identityApp.Services.CreateScope())
            {
                IdentityReadDbContext identDb = scope.ServiceProvider.GetRequiredService<IdentityReadDbContext>();
                UserReadModel? user = await identDb.Users.FirstOrDefaultAsync(u => u.UserId == hiredId);
                Assert.NotNull(user);
                Assert.True(user.IsLocked);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ACTUAL_ERROR: {ex.Message}");
            Console.WriteLine($"STACK_TRACE: {ex.StackTrace}");
            throw;
        }
        finally
        {
            identityApp?.Dispose();
            hrApp?.Dispose();
        }
    }
}
