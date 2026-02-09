using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Quality.Domain;
using ErpSystem.Quality.Infrastructure;
using ErpSystem.Quality.Application;
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
        var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:qualitydb");
        connectionString = secrets.Values.FirstOrDefault();
        if (!string.IsNullOrEmpty(connectionString)) break;
    }
    catch { await Task.Delay(1000); }
}

if (string.IsNullOrEmpty(connectionString))
    connectionString = builder.Configuration.GetConnectionString("qualitydb");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<QualityDbContext>(options =>
    options.UseNpgsql(connectionString));

// Dapr
builder.Services.AddDaprClient();

// BuildingBlocks
builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
builder.Services.AddDaprEventBus();

// MediatR - MUST be before IPublisher!
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(QualityIntegrationEventHandlers).Assembly));

// IPublisher (depends on MediatR)
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp =>
    new EventStore(
        sp.GetRequiredService<QualityDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Quality.Domain.{name}, ErpSystem.Quality")!));

// Register repositories
builder.Services.AddScoped<IQualityPointRepository, QualityPointRepository>();
builder.Services.AddScoped<QualityIntegrationEventHandlers>();

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
    QualityDbContext db = scope.ServiceProvider.GetRequiredService<QualityDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace ErpSystem.Quality.Infrastructure
{
    public class QualityDbContext(DbContextOptions<QualityDbContext> options) : DbContext(options)
    {
        public DbSet<EventStream> Events { get; set; } = null!;
        public DbSet<QualityPoint> QualityPoints { get; set; } = null!;
        public DbSet<QualityCheckReadModel> QualityChecks { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventStream>(b =>
            {
                b.HasKey(e => new { e.AggregateId, e.Version });
                b.Property(e => e.Payload).HasColumnType("jsonb");
            });

            modelBuilder.Entity<QualityPoint>().HasKey(x => x.Id);
            modelBuilder.Entity<QualityCheckReadModel>().HasKey(x => x.Id);
        }
    }

    public class QualityCheckReadModel
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class QualityPointRepository(QualityDbContext context) : IQualityPointRepository
    {
        public async Task<List<QualityPoint>> GetPointsForMaterial(string materialId, string operationType)
        {
            return await context.QualityPoints
                .Where(x => (x.MaterialId == materialId || x.MaterialId == "*") && x.OperationType == operationType && x.IsActive)
                .ToListAsync();
        }
    }
}
