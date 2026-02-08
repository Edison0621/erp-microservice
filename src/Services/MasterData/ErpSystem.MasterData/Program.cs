using Microsoft.EntityFrameworkCore;
using ErpSystem.MasterData.Infrastructure;
using ErpSystem.MasterData.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using ErpSystem.MasterData.Application;
using MediatR;
using ErpSystem.BuildingBlocks.Domain;

var builder = WebApplication.CreateBuilder(args);

// Service Defaults


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
builder.Services.AddScoped<BOMQueries>();

var app = builder.Build();

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
using (var scope = app.Services.CreateScope())
{
    var eventStoreDb = scope.ServiceProvider.GetRequiredService<MasterDataEventStoreDbContext>();
    var readDb = scope.ServiceProvider.GetRequiredService<MasterDataReadDbContext>();
    eventStoreDb.Database.EnsureCreated();
    readDb.Database.EnsureCreated();
}

app.Run();
