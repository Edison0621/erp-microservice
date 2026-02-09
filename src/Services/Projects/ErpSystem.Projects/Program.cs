using Microsoft.EntityFrameworkCore;
using MediatR;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Projects.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Persistence
builder.Services.AddDbContext<ProjectsEventStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("projectsdb")));
builder.Services.AddDbContext<ProjectsReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("projectsdb")));

// BuildingBlocks - EventBus first
builder.Services.AddDaprEventBus();

// MediatR - MUST be before IPublisher!
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));

// IPublisher (depends on MediatR)
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp =>
    new EventStore(
        sp.GetRequiredService<ProjectsEventStoreDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Projects.Domain.{name}, ErpSystem.Projects")!));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ErpSystem.Projects API", Version = "v1" });
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
