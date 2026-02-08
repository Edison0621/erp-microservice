using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ErpSystem.HR.Application;
using ErpSystem.HR.Domain;
using ErpSystem.Identity.Infrastructure;
using ErpSystem.Identity.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using MediatR;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.IntegrationTests;

public class HRToIdentityTests : IntegrationTestBase
{
    [Fact]
    public async Task EmployeeLifecycle_ShouldManageIdentityAccount()
    {
        WebApplicationFactory<ErpSystem.Identity.Program>? identityApp = null;
        WebApplicationFactory<ErpSystem.HR.Program>? hrApp = null;

        try 
        {
            // 1. Setup Identity App
            identityApp = CreateIdentityApp();
            var identityClient = identityApp.CreateClient();
            
            // 2. Setup HR App (with two endpoints for hired and terminated)
            // Note: Our simple TestEventBus only supports one endpoint per bus.
            // But we can create two buses or use a more flexible one.
            // For now, I'll hire, then terminated. I'll switch the endpoint if needed or use a multi-endpoint bus.
            
            var testEventBus = new TestEventBus(identityClient, "/api/v1/identity/integration/employee-hired");
            hrApp = CreateHRApp(testEventBus);

            var mediatorHR = hrApp.Services.GetRequiredService<IMediator>();
            var mediatorIdentity = identityApp.Services.GetRequiredService<IMediator>();
            
            var employeeId = Guid.NewGuid(); // We'll bypass Guid.NewGuid() in Hire to use a controlled one if needed, 
                                             // but HireEmployeeCommand generates its own. 
                                             // Wait! Handle(HireEmployeeCommand) uses Guid.NewGuid().
                                             
            // I'll use the returned Id.
            
            // 3. Hire Employee
            var hiredId = await mediatorHR.Send(new HireEmployeeCommand(
                "John Doe", "Male", new DateTime(1990, 1, 1), "ID", "123456789",
                DateTime.UtcNow, EmploymentType.FullTime, "COMP-1", "DEPT-1", "POS-1",
                "", "CC-1", "john.doe@example.com"
            ));

            // 4. Verify User Created in Identity
            await Task.Delay(500); 

            using (var scope = identityApp.Services.CreateScope())
            {
                var identDb = scope.ServiceProvider.GetRequiredService<IdentityReadDbContext>();
                var user = await identDb.Users.FirstOrDefaultAsync(u => u.UserId == hiredId);
                Assert.NotNull(user);
                Assert.Equal("john.doe@example.com", user.Email);
                Assert.False(user.IsLocked);
            }

            // 5. Setup Terminated Bus (Re-create HR App with new bus or use a smarter bus)
            // Re-creating is easiest for this test.
            hrApp.Dispose();
            var termEventBus = new TestEventBus(identityClient, "/api/v1/identity/integration/employee-terminated");
            hrApp = CreateHRApp(termEventBus);
            mediatorHR = hrApp.Services.GetRequiredService<IMediator>();

            // 6. Terminate Employee
            await mediatorHR.Send(new TerminateEmployeeCommand(
                hiredId, DateTime.UtcNow, "RESIGNATION", "Leaving for better opportunity"
            ));

            // 7. Verify User Locked in Identity
            await Task.Delay(500);

            using (var scope = identityApp.Services.CreateScope())
            {
                var identDb = scope.ServiceProvider.GetRequiredService<IdentityReadDbContext>();
                var user = await identDb.Users.FirstOrDefaultAsync(u => u.UserId == hiredId);
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
