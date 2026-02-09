using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ErpSystem.Procurement.Infrastructure;
using ErpSystem.Inventory.Infrastructure;
using ErpSystem.Sales.Infrastructure;
using ErpSystem.Production.Infrastructure;
using ErpSystem.HR.Infrastructure;
using ErpSystem.Identity.Infrastructure;
using ErpSystem.Finance.Infrastructure;
using ErpSystem.BuildingBlocks.EventBus;
using Moq;
using Dapr.Client;
using System.Net.Http.Json;

namespace ErpSystem.IntegrationTests;

public class IntegrationTestBase
{
    protected WebApplicationFactory<HR.Program> CreateHrApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<HR.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    List<ServiceDescriptor> dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach (ServiceDescriptor d in dbDescriptors) services.Remove(d);

                    services.AddDbContext<HrEventStoreDbContext>(o => o.UseInMemoryDatabase("TestHRES"));
                    services.AddDbContext<HrReadDbContext>(o => o.UseInMemoryDatabase("TestHRRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    ServiceDescriptor? busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<Identity.Program> CreateIdentityApp()
    {
        return new WebApplicationFactory<Identity.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    List<ServiceDescriptor> dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach (ServiceDescriptor d in dbDescriptors) services.Remove(d);

                    services.AddDbContext<EventStoreDbContext>(o => o.UseInMemoryDatabase("TestIdentES"));
                    services.AddDbContext<IdentityReadDbContext>(o => o.UseInMemoryDatabase("TestIdentRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);
                });
            });
    }

    protected WebApplicationFactory<Production.Program> CreateProductionApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<Production.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    List<ServiceDescriptor> dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(ServiceDescriptor d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<ProductionEventStoreDbContext>(o => o.UseInMemoryDatabase("TestPrdES"));
                    services.AddDbContext<ProductionReadDbContext>(o => o.UseInMemoryDatabase("TestPrdRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    ServiceDescriptor? busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<Sales.Program> CreateSalesApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<Sales.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    List<ServiceDescriptor> dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(ServiceDescriptor d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<SalesEventStoreDbContext>(o => o.UseInMemoryDatabase("TestSalesES"));
                    services.AddDbContext<SalesReadDbContext>(o => o.UseInMemoryDatabase("TestSalesRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    ServiceDescriptor? busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<Procurement.Program> CreateProcurementApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<Procurement.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Kill DbContexts to prevent connection attempts
                    List<ServiceDescriptor> dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(ServiceDescriptor d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<ProcurementEventStoreDbContext>(o => o.UseInMemoryDatabase("TestPOES"));
                    services.AddDbContext<ProcurementReadDbContext>(o => o.UseInMemoryDatabase("TestPORead"));

                    // Mock DaprClient
                    services.AddSingleton(new Mock<DaprClient>().Object);

                    // Replace real EventBus
                    ServiceDescriptor? busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<Inventory.Program> CreateInventoryApp()
    {
        return new WebApplicationFactory<Inventory.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    List<ServiceDescriptor> dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(ServiceDescriptor d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<InventoryEventStoreDbContext>(o => o.UseInMemoryDatabase("TestInvES"));
                    services.AddDbContext<InventoryReadDbContext>(o => o.UseInMemoryDatabase("TestInvRead"));

                    // Mock DaprClient
                    services.AddSingleton(new Mock<DaprClient>().Object);
                });
            });
    }

    protected WebApplicationFactory<ErpSystem.Finance.Program> CreateFinanceApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<ErpSystem.Finance.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    List<ServiceDescriptor> dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(ServiceDescriptor d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<FinanceEventStoreDbContext>(o => o.UseInMemoryDatabase("TestFinES"));
                    services.AddDbContext<FinanceReadDbContext>(o => o.UseInMemoryDatabase("TestFinRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    ServiceDescriptor? busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }
}

public class TestEventBus(HttpClient targetClient, string endpoint) : IEventBus
{
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        if (targetClient == null) return;
        await targetClient.PostAsJsonAsync(endpoint, @event, cancellationToken);
    }
}
