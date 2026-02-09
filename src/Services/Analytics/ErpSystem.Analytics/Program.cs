using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Analytics.Infrastructure;
using ErpSystem.Analytics.Application;
using ErpSystem.Analytics.Infrastructure.BackgroundJobs;
using MediatR;
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
        var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:analyticsdb");
        connectionString = secrets.Values.FirstOrDefault();
        if (!string.IsNullOrEmpty(connectionString)) break;
    }
    catch { await Task.Delay(1000); }
}

if (string.IsNullOrEmpty(connectionString))
    connectionString = builder.Configuration.GetConnectionString("analyticsdb");

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Dapr
builder.Services.AddDaprClient();

// BuildingBlocks
builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
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
builder.Services.AddHostedService<AnalyticsNotifier>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHub<ErpSystem.Analytics.API.Hubs.AnalyticsHub>("/hubs/analytics");

// Ensure databases created
using (IServiceScope scope = app.Services.CreateScope())
{
    AnalyticsDbContext db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace ErpSystem.Analytics.Infrastructure
{
    public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
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
