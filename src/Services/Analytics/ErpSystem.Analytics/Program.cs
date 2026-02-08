using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Analytics.Domain;
using ErpSystem.Analytics.Infrastructure;
using ErpSystem.Analytics.Application;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("analyticsdb")));

// Event Sourcing & MediatR & EventBus
builder.Services.AddDaprEventBus();

// MediatR - MUST be before IPublisher!
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ForecastingAppService).Assembly));

// IPublisher (depends on MediatR)
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp => 
    new EventStore(
        sp.GetRequiredService<AnalyticsDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Analytics.Domain.{name}, ErpSystem.Analytics")!));

// Register forecasting service
builder.Services.AddScoped<DemandForecastEngine>();
builder.Services.AddScoped<TimescaleDataExtractor>();
builder.Services.AddScoped<ForecastingAppService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Ensure databases created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace ErpSystem.Analytics.Infrastructure
{
    public class AnalyticsDbContext : DbContext
    {
        public DbSet<EventStream> Events { get; set; } = null!;

        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

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
