using ErpSystem.Reporting.Application;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.EventBus;
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
        var secrets = await daprClient.GetSecretAsync("localsecretstore", "connectionstrings:reportingdb");
        connectionString = secrets.Values.FirstOrDefault();
        if (!string.IsNullOrEmpty(connectionString)) break;
    }
    catch { await Task.Delay(1000); }
}

if (string.IsNullOrEmpty(connectionString))
    connectionString = builder.Configuration.GetConnectionString("reportingdb");

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ERP Reporting API", Version = "v1" });
});

// Databases
builder.Services.AddDbContext<ReportingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Dapr
builder.Services.AddDaprClient();

// BuildingBlocks
builder.Services.AddBuildingBlocks(new[] { typeof(Program).Assembly });
builder.Services.AddDaprEventBus();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DashboardService).Assembly));

// Health checks
builder.Services.AddHealthChecks();

// Application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

WebApplication app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.UseAuthorization();
app.MapControllers();

// Ensure databases created
using (IServiceScope scope = app.Services.CreateScope())
{
    ReportingDbContext db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace ErpSystem.Reporting.Application
{
    public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
    {
        public DbSet<DashboardSummaryReadModel> DashboardSummaries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DashboardSummaryReadModel>().HasKey(x => x.TenantId);
        }
    }

    public class DashboardSummaryReadModel
    {
        public string TenantId { get; set; } = "DEFAULT";
        public decimal TotalRevenue { get; set; }
        public decimal RevenueChange { get; set; }
        public int TotalOrders { get; set; }
        public int OrdersChange { get; set; }
        public decimal InventoryValue { get; set; }
        public int LowStockItems { get; set; }
        public int PendingPurchaseOrders { get; set; }
        public int ActiveProductionOrders { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
