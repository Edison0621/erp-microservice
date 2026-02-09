using ErpSystem.Reporting.Application;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.EventBus;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ERP Reporting API", Version = "v1" });
});

// Databases
builder.Services.AddDbContext<ReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("reportingdb")));

// EventBus & MediatR (For Projections)
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
