using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ErpSystem.Procurement.Infrastructure;
using ErpSystem.Procurement.Domain;
using ErpSystem.Inventory.Infrastructure;
using ErpSystem.Inventory.Domain;
using ErpSystem.Sales.Infrastructure;
using ErpSystem.Sales.Domain;
using ErpSystem.Production.Infrastructure;
using ErpSystem.Production.Domain;
using ErpSystem.HR.Infrastructure;
using ErpSystem.HR.Domain;
using ErpSystem.Identity.Infrastructure;
using ErpSystem.Identity.Domain;
using ErpSystem.Finance.Infrastructure;
using ErpSystem.Finance.Domain;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using Moq;
using Dapr.Client;
using MediatR;
using System.Net.Http.Json;

namespace ErpSystem.IntegrationTests;

public class IntegrationTestBase
{
    protected WebApplicationFactory<ErpSystem.HR.Program> CreateHRApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<ErpSystem.HR.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach (var d in dbDescriptors) services.Remove(d);

                    services.AddDbContext<HREventStoreDbContext>(o => o.UseInMemoryDatabase("TestHRES"));
                    services.AddDbContext<HRReadDbContext>(o => o.UseInMemoryDatabase("TestHRRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<ErpSystem.Identity.Program> CreateIdentityApp()
    {
        return new WebApplicationFactory<ErpSystem.Identity.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach (var d in dbDescriptors) services.Remove(d);

                    services.AddDbContext<EventStoreDbContext>(o => o.UseInMemoryDatabase("TestIdentES"));
                    services.AddDbContext<IdentityReadDbContext>(o => o.UseInMemoryDatabase("TestIdentRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);
                });
            });
    }

    protected WebApplicationFactory<ErpSystem.Production.Program> CreateProductionApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<ErpSystem.Production.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(var d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<ProductionEventStoreDbContext>(o => o.UseInMemoryDatabase("TestPrdES"));
                    services.AddDbContext<ProductionReadDbContext>(o => o.UseInMemoryDatabase("TestPrdRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<ErpSystem.Sales.Program> CreateSalesApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<ErpSystem.Sales.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(var d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<SalesEventStoreDbContext>(o => o.UseInMemoryDatabase("TestSalesES"));
                    services.AddDbContext<SalesReadDbContext>(o => o.UseInMemoryDatabase("TestSalesRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<ErpSystem.Procurement.Program> CreateProcurementApp(IEventBus mockEventBus)
    {
        return new WebApplicationFactory<ErpSystem.Procurement.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Kill DbContexts to prevent connection attempts
                    var dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(var d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<ProcurementEventStoreDbContext>(o => o.UseInMemoryDatabase("TestPOES"));
                    services.AddDbContext<ProcurementReadDbContext>(o => o.UseInMemoryDatabase("TestPORead"));

                    // Mock DaprClient
                    services.AddSingleton(new Mock<DaprClient>().Object);

                    // Replace real EventBus
                    var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }

    protected WebApplicationFactory<ErpSystem.Inventory.Program> CreateInventoryApp()
    {
        return new WebApplicationFactory<ErpSystem.Inventory.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(var d in dbDescriptors) services.Remove(d);
                    
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
                    var dbDescriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext")).ToList();
                    foreach(var d in dbDescriptors) services.Remove(d);
                    
                    services.AddDbContext<FinanceEventStoreDbContext>(o => o.UseInMemoryDatabase("TestFinES"));
                    services.AddDbContext<FinanceReadDbContext>(o => o.UseInMemoryDatabase("TestFinRead"));

                    services.AddSingleton(new Mock<DaprClient>().Object);

                    var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
                    if (busDescriptor != null) services.Remove(busDescriptor);
                    services.AddSingleton(mockEventBus);
                });
            });
    }
}

public class TestEventBus : IEventBus
{
    private readonly HttpClient _targetClient;
    private readonly string _endpoint;

    public TestEventBus(HttpClient targetClient, string endpoint)
    {
        _targetClient = targetClient;
        _endpoint = endpoint;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        if (_targetClient == null) return;
        await _targetClient.PostAsJsonAsync(_endpoint, @event, cancellationToken);
    }
}
