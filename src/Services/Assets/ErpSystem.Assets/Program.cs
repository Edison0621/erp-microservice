using Microsoft.EntityFrameworkCore;
using MediatR;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Assets.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Persistence
builder.Services.AddDbContext<AssetsEventStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("assetsdb")));
builder.Services.AddDbContext<AssetsReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("assetsdb")));

// BuildingBlocks
builder.Services.AddDaprEventBus();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register EventStore
builder.Services.AddScoped<IEventStore>(sp =>
    new EventStore(
        sp.GetRequiredService<AssetsEventStoreDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Assets.Domain.{name}, ErpSystem.Assets")!));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ErpSystem.Assets API", Version = "v1" });
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
