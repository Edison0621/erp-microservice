using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Mrp.Infrastructure;
using ErpSystem.Mrp.Application;
using MediatR;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<MrpDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("mrpdb")));

// Event Sourcing & MediatR & EventBus
builder.Services.AddDaprEventBus();

// MediatR - MUST be before IPublisher!
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MrpCalculationEngine).Assembly));

// IPublisher (depends on MediatR)
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Dapr Client (Required for Query Services)
builder.Services.AddDaprClient();

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp => 
    new EventStore(
        sp.GetRequiredService<MrpDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Mrp.Domain.{name}, ErpSystem.Mrp")!));

// Register engine
builder.Services.AddScoped<MrpCalculationEngine>();

// Register Query Services
builder.Services.AddScoped<IInventoryQueryService, DaprInventoryQueryService>();
builder.Services.AddScoped<IProcurementQueryService, DaprProcurementQueryService>();
builder.Services.AddScoped<IProductionQueryService, DaprProductionQueryService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Ensure databases created
using (IServiceScope scope = app.Services.CreateScope())
{
    MrpDbContext db = scope.ServiceProvider.GetRequiredService<MrpDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace ErpSystem.Mrp.Infrastructure
{
    public class MrpDbContext(DbContextOptions<MrpDbContext> options) : DbContext(options)
    {
        public DbSet<EventStream> Events { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventStream>(b =>
            {
                b.HasKey(e => new { e.AggregateId, e.Version });
                b.Property(e => e.Payload).HasColumnType("jsonb");
            });
        }
    }
}
