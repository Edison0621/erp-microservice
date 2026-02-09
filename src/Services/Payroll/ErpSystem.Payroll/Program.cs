using Microsoft.EntityFrameworkCore;
using MediatR;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Payroll.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Persistence
builder.Services.AddDbContext<PayrollEventStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("payrolldb")));
builder.Services.AddDbContext<PayrollReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("payrolldb")));

// BuildingBlocks
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
