using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Automation.Domain;
using ErpSystem.Automation.Infrastructure;
using ErpSystem.Automation.Application;
using MediatR;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databases
builder.Services.AddDbContext<AutomationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("automationdb")));

// Event Sourcing & MediatR & EventBus
builder.Services.AddDaprEventBus();

// MediatR - MUST be before IPublisher!
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AutomationEngine).Assembly));

// IPublisher (depends on MediatR)
builder.Services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

// Register the main EventStore
builder.Services.AddScoped<IEventStore>(sp => 
    new EventStore(
        sp.GetRequiredService<AutomationDbContext>(),
        sp.GetRequiredService<IPublisher>(),
        sp.GetRequiredService<IEventBus>(),
        name => Type.GetType($"ErpSystem.Automation.Domain.{name}, ErpSystem.Automation")!));

// Register repositories and engine
builder.Services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
builder.Services.AddScoped<IActionExecutor, ActionExecutor>();
builder.Services.AddScoped<AutomationEngine>();

builder.Services.AddHttpClient();

// Placeholder services for ActionExecutor
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

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
    var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace ErpSystem.Automation.Infrastructure
{
    public class AutomationDbContext : DbContext
    {
        public DbSet<EventStream> Events { get; set; } = null!;
        public DbSet<AutomationRuleReadModel> Rules { get; set; } = null!;

        public AutomationDbContext(DbContextOptions<AutomationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventStream>(b =>
            {
                b.HasKey(e => new { e.AggregateId, e.Version });
                b.Property(e => e.Payload).HasColumnType("jsonb");
            });
            
            modelBuilder.Entity<AutomationRuleReadModel>().HasKey(x => x.Id);
        }
    }

    public class AutomationRuleReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TriggerEventType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AutomationRuleRepository : IAutomationRuleRepository
    {
        private readonly AutomationDbContext _context;
        private readonly IEventStore _eventStore;
        public AutomationRuleRepository(AutomationDbContext context, IEventStore eventStore) 
        {
             _context = context;
             _eventStore = eventStore;
        }

        public async Task<List<AutomationRule>> GetActiveRulesByEventType(string eventType)
        {
            // In a real system, we'd query the read model then load aggregates
            return new List<AutomationRule>();
        }
    }

    public class EmailService : IEmailService 
    {
        public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
    }

    public class NotificationService : INotificationService
    {
        public Task SendNotificationAsync(string channel, string message) => Task.CompletedTask;
    }
}
