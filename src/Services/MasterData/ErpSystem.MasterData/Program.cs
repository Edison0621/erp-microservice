using Microsoft.EntityFrameworkCore;
using ErpSystem.MasterData.Infrastructure;
using ErpSystem.MasterData.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.MasterData.Application;
using MediatR;
using ErpSystem.BuildingBlocks.Domain;
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
        var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:masterdatadb");
        connectionString = secrets.Values.FirstOrDefault();
        if (!string.IsNullOrEmpty(connectionString)) break;
    }
    catch { await Task.Delay(1000); }
}

if (string.IsNullOrEmpty(connectionString))
    connectionString = builder.Configuration.GetConnectionString("masterdatadb");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<MasterDataEventStoreDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDbContext<MasterDataReadDbContext>(options =>
    options.UseNpgsql(connectionString));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// BuildingBlocks
builder.Services.AddDaprClient();
builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());
builder.Services.AddDaprEventBus();

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp =>
    new EventStore(
        sp.GetRequiredService<MasterDataEventStoreDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.MasterData.Domain.{name}, ErpSystem.MasterData")!));

// Register typed repositories
builder.Services.AddScoped(typeof(EventStoreRepository<>));

// Domain Services
builder.Services.AddSingleton<ICodeGenerator, DefaultCodeGenerator>();
builder.Services.AddScoped<BomQueries>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

// Migrate and Ensure DB
using (IServiceScope scope = app.Services.CreateScope())
{
    MasterDataEventStoreDbContext eventStoreDb = scope.ServiceProvider.GetRequiredService<MasterDataEventStoreDbContext>();
    MasterDataReadDbContext readDb = scope.ServiceProvider.GetRequiredService<MasterDataReadDbContext>();
    eventStoreDb.Database.EnsureCreated();
    readDb.Database.EnsureCreated();
}

app.Run();
