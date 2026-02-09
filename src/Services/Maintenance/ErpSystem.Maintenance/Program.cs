using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Maintenance.Infrastructure;
using MediatR;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Persistence
builder.Services.AddDbContext<MaintenanceEventStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("maintenancedb")));
builder.Services.AddDbContext<MaintenanceReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("maintenancedb")));

// BuildingBlocks
builder.Services.AddDaprEventBus();

// MediatR - MUST be before IPublisher!
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));

// IPublisher (depends on MediatR)
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp => 
    new EventStore(
        sp.GetRequiredService<MaintenanceEventStoreDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Maintenance.Domain.{name}, ErpSystem.Maintenance")!));

// Register typed repositories
builder.Services.AddScoped(typeof(EventStoreRepository<>));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Ensure databases created
if (!app.Environment.IsEnvironment("Testing"))
{
    using IServiceScope scope = app.Services.CreateScope();
    MaintenanceEventStoreDbContext es = scope.ServiceProvider.GetRequiredService<MaintenanceEventStoreDbContext>();
    await es.Database.EnsureCreatedAsync();
    MaintenanceReadDbContext rs = scope.ServiceProvider.GetRequiredService<MaintenanceReadDbContext>();
    await rs.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }
