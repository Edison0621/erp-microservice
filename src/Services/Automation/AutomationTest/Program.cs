using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ErpSystem.Automation.Infrastructure;

namespace AutomationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            Console.WriteLine("Starting Automation DB Test...");

            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());
            services.AddDbContext<AutomationDbContext>(options =>
                options.UseNpgsql("Host=localhost;Database=automationdb;Username=postgres;Password=postgres"));

            var serviceProvider = services.BuildServiceProvider();

            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
                    Console.WriteLine("Database Context Created.");
                    
                    Console.WriteLine("EnsureCreatedAsync starting...");
                    context.Database.EnsureCreated();
                    Console.WriteLine("EnsureCreatedAsync completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"INNER ERROR: {ex.InnerException.Message}");
                    Console.WriteLine($"INNER STACK: {ex.InnerException.StackTrace}");
                }
            }
            
            Console.WriteLine("Test Finished.");
        }
    }
}
