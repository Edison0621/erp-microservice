using Microsoft.EntityFrameworkCore;
using MediatR;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Payroll.Infrastructure;
using Dapr.Client;
using ErpSystem.BuildingBlocks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Dapr Client
var daprClient = new DaprClientBuilder().Build();

// Fetch connection string from Dapr Secrets with retry
string? connectionString = null;
for (int i = 0; i < 5; i++)
{
    try
    {
        var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:payrolldb");
        connectionString = secrets.Values.FirstOrDefault();
        if (!string.IsNullOrEmpty(connectionString)) break;
    }
    catch { await Task.Delay(1000); }
}

if (string.IsNullOrEmpty(connectionString))
    connectionString = builder.Configuration.GetConnectionString("payrolldb");

// Persistence
builder.Services.AddDbContext<PayrollEventStoreDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<PayrollReadDbContext>(options =>
    options.UseNpgsql(connectionString));

// Dapr
builder.Services.AddDaprClient();

// BuildingBlocks
builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
builder.Services.AddDaprEventBus();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register EventStore
builder.Services.AddScoped<IEventStore>(sp =>
    new EventStore(
        sp.GetRequiredService<PayrollEventStoreDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Payroll.Domain.{name}, ErpSystem.Payroll")!));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ErpSystem.Payroll API", Version = "v1" });
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
