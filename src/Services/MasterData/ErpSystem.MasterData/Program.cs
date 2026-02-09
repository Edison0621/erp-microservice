using Microsoft.EntityFrameworkCore;
using ErpSystem.MasterData.Infrastructure;
using ErpSystem.MasterData.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.MasterData.Application;
using MediatR;
using ErpSystem.BuildingBlocks.Domain;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<MasterDataEventStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("masterdatadb")));

builder.Services.AddDbContext<MasterDataReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("masterdatadb")));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// BuildingBlocks
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
app.MapControllers();
// app.MapSubscribeHandler(); 

// Migrate and Ensure DB
using (IServiceScope scope = app.Services.CreateScope())
{
    MasterDataEventStoreDbContext eventStoreDb = scope.ServiceProvider.GetRequiredService<MasterDataEventStoreDbContext>();
    MasterDataReadDbContext readDb = scope.ServiceProvider.GetRequiredService<MasterDataReadDbContext>();
    eventStoreDb.Database.EnsureCreated();
    readDb.Database.EnsureCreated();
}

app.Run();
